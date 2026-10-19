/*
https://git.annaclemens.io/ascclemens/XivCommon/src/branch/main/XivCommon/Functions/Chat.cs 
MIT License
Copyright (c) 2021 Anna Clemens
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using Lumina.Excel.Sheets;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using static ECommons.Automation.Chat.Memory;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
namespace ECommons.Automation;
#nullable disable

/// <summary>
/// A class containing chat functionality
/// </summary>
public static unsafe class Chat
{
    public static class Memory
    {
        public static readonly string SendChatSignature = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9";
        public static readonly string SanitiseStringSignature = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70 4D 8B F8 4C 89 44 24 ?? 4C 8B 05 ?? ?? ?? ?? 44 8B E2";

        // 兩個取得器：特徵碼對不上時回 null（結果快取、只記一次 Warning），不再每次讀都重掃再擲。
        private static bool ProcessChatBoxFailed = false;
        private static bool SanitizeStringFailed = false;

        public delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);
        public static ProcessChatBoxDelegate ProcessChatBox
        {
            get
            {
                if(field == null && !ProcessChatBoxFailed)
                {
                    if(!Svc.SigScanner.TryScanText(SendChatSignature, out var addr))
                    {
                        ProcessChatBoxFailed = true;
                        PluginLog.Warning($"Chat: signature for chat sending not found; Chat.SendMessage/ExecuteCommand will do nothing until the plugin is reloaded ({SendChatSignature})");
                        return null;
                    }
                    PluginLog.Debug($"ProcessChatBox addr: {(nint)addr:X16}");
                    field = EzDelegate.Get<ProcessChatBoxDelegate>(addr) ?? throw new InvalidOperationException("Could not find signature for chat sending");
                }
                return field;
            }
        }
        public delegate void SanitizeStringDeletage(Utf8String* stringPtr, int a2, nint a3);
        public static SanitizeStringDeletage SanitizeString
        {
            get
            {
                if(field == null && !SanitizeStringFailed)
                {
                    if(!Svc.SigScanner.TryScanText(SanitiseStringSignature, out var addr))
                    {
                        SanitizeStringFailed = true;
                        PluginLog.Warning($"Chat: SanitizeString signature not found; Chat.SendMessage/ExecuteCommand will do nothing and Chat.SanitiseText will throw until the plugin is reloaded ({SanitiseStringSignature})");
                        return null;
                    }
                    PluginLog.Debug($"SanitizeString addr: {(nint)addr:X16}");
                    field = EzDelegate.Get<SanitizeStringDeletage>(addr) ?? throw new InvalidOperationException("Could not find SanitizeString signature");
                }
                return field;
            }
        }
    }

    /// <summary>
    /// <para>
    /// Send a given message to the chat box. <b>This can send chat to the server.</b>
    /// </para>
    /// <para>
    /// <b>This method is unsafe.</b> This method does no checking on your input and
    /// may send content to the server that the normal client could not. You must
    /// verify what you're sending and handle content and length to properly use
    /// this.
    /// </para>
    /// </summary>
    /// <param name="message">Message to send</param>
    /// <remarks>If the signature for this function could not be found, logs a warning once and does nothing.</remarks>
    /// <exception cref="InvalidOperationException">If the framework, UI module, or the game's string functions are not available</exception>
    [Obsolete("Use safe message sending")]
    public static void SendMessageUnsafe(byte[] message)
    {
        var processChatBox = ProcessChatBox;
        if(processChatBox == null) return;
        // 🔴 Framework.Instance() 是 [StaticAddress(..., isPointer: true)]:回的是靜態位址裡存放的指標值,
        // 產生器不判空,登入前/關閉中真的會是 null。GetUIModule() 是原生成員函式,對 null 的 this 呼叫是
        // AccessViolation(corrupted-state exception,try/catch 攔不到)。
        // 沿用本檔缺必要前提時擲 InvalidOperationException 的慣例(見上面的特徵碼取得器):
        // 訊息不送出,呼叫端拿到的是可攔截的例外而不是遊戲崩潰。
        var framework = CSAddress.FrameworkOrNull();
        if(framework == null) throw new InvalidOperationException("Framework is not available; chat message was not sent.");
        if(!CSAddress.Resolved(Framework.MemberFunctionPointers.GetUIModule)) throw new InvalidOperationException("UIModule is not available; chat message was not sent.");
        var uiModulePtr = framework->GetUIModule();
        if(uiModulePtr == null) throw new InvalidOperationException("UIModule is not available; chat message was not sent.");
        var uiModule = (IntPtr)uiModulePtr;
        // 遊戲要的是完整初始化的 Utf8String（0x68）；字串內容到第一個 NUL 為止，與原生 C 字串讀法相同。
        var terminated = new byte[message.Length + 1];
        message.CopyTo(terminated, 0);
        Utf8String* text;
        fixed(byte* bytes = terminated)
        {
            text = Utf8String.FromSequence(bytes);
        }
        try
        {
            processChatBox(uiModule, (IntPtr)text, IntPtr.Zero, 0);
        }
        finally
        {
            text->Dtor();
            IMemorySpace.Free(text);
        }
    }
    /// <summary>
    /// <para>
    /// Send a given message to the chat box. <b>This can send chat to the server.</b>
    /// </para>
    /// <para>
    /// This method is slightly less unsafe than <see cref="SendMessageUnsafe"/>. It
    /// will throw exceptions for certain inputs that the client can't normally send,
    /// but it is still possible to make mistakes. Use with caution.
    /// </para>
    /// </summary>
    /// <param name="message">message to send</param>
    /// <remarks>If a signature this needs could not be found, logs a warning once and does nothing (empty or over-long messages still throw).</remarks>
    /// <exception cref="ArgumentException">If <paramref name="message"/> is empty, longer than 500 bytes in UTF-8, or contains invalid characters.</exception>
    /// <exception cref="InvalidOperationException">If the framework or UI module is not available</exception>
    public static void SendMessage(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        if(bytes.Length == 0)
        {
            throw new ArgumentException("message is empty", nameof(message));
        }
        if(bytes.Length > 500)
        {
            throw new ArgumentException("message is longer than 500 bytes", nameof(message));
        }
        if(SanitizeString == null || ProcessChatBox == null) return;
        if(message.Length != SanitiseText(message).Length)
        {
            throw new ArgumentException("message contained invalid characters", nameof(message));
        }
        if(message.Contains('\n'))
        {
            throw new ArgumentException("message can't contain multiple lines", nameof(message));
        }
        if(message.Contains('\r'))
        {
            throw new ArgumentException("message can't contain carriage return", nameof(message));
        }
#pragma warning disable CS0618 // Type or member is obsolete
        SendMessageUnsafe(bytes);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Executes command as if it was typed in chat box. 
    /// </summary>
    /// <param name="message">Full text of the command.</param>
    /// <exception cref="InvalidOperationException">If you didn't prefixed it with a slash.</exception>
    public static void ExecuteCommand(string message)
    {
        if(!message.StartsWith("/")) throw new InvalidOperationException($"Attempted to execute command but was not prefixed with a slash: {message}");
        SendMessage(message);
    }

    /// <summary>
    /// Executes General Action by ID via chat.
    /// </summary>
    /// <param name="generalActionId"></param>
    public static void ExecuteGeneralAction(uint generalActionId)
    {
        ExecuteCommand($"/generalaction \"{Svc.Data.GetExcelSheet<GeneralAction>().GetRowOrDefault(generalActionId)?.Name}\"");
    }

    /// <summary>
    /// Executes Action by ID via chat.
    /// </summary>
    /// <param name="actionId"></param>
    public static void ExecuteAction(uint actionId)
    {
        ExecuteCommand($"/action \"{Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(actionId)?.Name}\"");
    }

    /// <summary>
    /// <para>
    /// Sanitises a string by removing any invalid input.
    /// </para>
    /// <para>
    /// The result of this method is safe to use with
    /// <see cref="SendMessage"/>, provided that it is not empty or too
    /// long.
    /// </para>
    /// </summary>
    /// <param name="text">text to sanitise</param>
    /// <returns>sanitised text</returns>
    /// <exception cref="InvalidOperationException">If the signature for this function could not be found</exception>
    public static string SanitiseText(string text)
    {
        if(SanitizeString == null)
        {
            throw new InvalidOperationException("Could not find signature for chat sanitisation");
        }
        var uText = Utf8String.FromString(text);
        SanitizeString(uText, 0x27F, IntPtr.Zero);
        var sanitised = uText->ToString();
        uText->Dtor();
        IMemorySpace.Free(uText);
        return sanitised;
    }

    [Obsolete("Use Chat.<MethodName> directly instead of Chat.Instance.<MethodName>")]
    public static class Instance
    {
        public static void ExecuteCommand(string message) => Chat.ExecuteCommand(message);
        public static void SendMessageUnsafe(byte[] message) => Chat.SendMessageUnsafe(message);
        public static void SendMessage(string message) => Chat.SendMessage(message);
        public static void ExecuteGeneralAction(uint generalActionId) => Chat.ExecuteGeneralAction(generalActionId);
        public static void ExecuteAction(uint actionId) => Chat.ExecuteAction(actionId);
        public static string SanitiseText(string text) => Chat.SanitiseText(text);
    }
}