using System;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ECommons.Automation.NeoTaskManager;
public class TaskManagerTask
{
    public string Name { get; init; }
    public string Location { get; init; }
    public Func<bool?> Function { get; init; }
    public TaskManagerConfiguration? Configuration { get; init; }

    public TaskManagerTask(Func<bool?> function, TaskManagerConfiguration? configuration = null)
    {
        var method = function.GetMethodInfo();
        Function = function;
        Configuration = configuration;
        Name = DescribeMethod(method);
        Location = method.DeclaringType?.Name ?? "";
    }

    public TaskManagerTask(Func<bool> function, TaskManagerConfiguration? configuration = null)
    {
        // 名字要在包裝之前取: Function 被包成 () => function() 之後反查只會拿到 ECommons 自己的型別。
        var method = function.GetMethodInfo();
        Function = () => function();
        Configuration = configuration;
        Name = DescribeMethod(method);
        Location = method.DeclaringType?.Name ?? "";
    }

    public TaskManagerTask(Action action, TaskManagerConfiguration? configuration = null)
    {
        var method = action.GetMethodInfo();
        Function = () =>
        {
            action();
            return true;
        };
        Configuration = configuration;
        Name = DescribeMethod(method);
        Location = method.DeclaringType?.Name ?? "";
    }

    public TaskManagerTask(Func<bool?> function, string taskName, TaskManagerConfiguration? configuration = null)
    {
        Function = function;
        Configuration = configuration;
        Name = taskName;
        Location = function.GetMethodInfo().DeclaringType?.Name ?? "";
    }

    public TaskManagerTask(Func<bool> function, string taskName, TaskManagerConfiguration? configuration = null)
    {
        Function = () => function();
        Configuration = configuration;
        Name = taskName;
        Location = function.GetMethodInfo().DeclaringType?.Name ?? "";
    }

    public TaskManagerTask(Action action, string taskName, TaskManagerConfiguration? configuration = null)
    {
        Function = () =>
        {
            action();
            return true;
        };
        Configuration = configuration;
        Name = taskName;
        Location = action.GetMethodInfo().DeclaringType?.Name ?? "";
    }

    // 格式與 TaskManager 其餘 log 行的 [Name@Location] 一致，同一顆任務在 log 裡只有一種寫法。
    public override string ToString() => $"{Name}@{Location}";

    /// <summary>lambda 序號的分隔符。'#' 不是 C# 識別字字元，撞不到任何方法名或具名任務名。</summary>
    private const char LambdaOrdinalSeparator = '#';

    /// <summary>未具名任務的顯示名。</summary>
    /// <remarks>
    /// 編譯器產生的名字(lambda／區域函式)改寫成「真類別.外層方法」，lambda 再接上序號;
    /// 一般方法與方法群組原樣保留 —— 它們的名字本來就可讀，而 <see cref="Location"/> 已經帶了型別。
    /// </remarks>
    private static string DescribeMethod(MethodInfo method)
    {
        var raw = method.Name ?? "";
        if(raw.Length == 0 || raw[0] != '<') return raw;
        var member = ParseGeneratedMemberName(raw);
        if(member.Length == 0) return raw;
        var owner = UnwrapCompilerGeneratedType(method.DeclaringType);
        return owner == null ? member : $"{owner.Name}.{member}";
    }

    /// <summary>從編譯器產生的方法名取出可讀的成員名; 取不出來時回空字串。</summary>
    /// <remarks>
    /// 形狀是 <c>&lt;外層方法&gt;b__N</c> 與 <c>&lt;外層方法&gt;g__區域函式|N_M</c>，
    /// 巢狀 lambda 會再包一層角括號，所以遞迴剝。
    /// 區域函式的名字比外層方法精確，兩個都留成「外層方法.區域函式」。
    /// </remarks>
    private static string ParseGeneratedMemberName(string raw)
    {
        if(raw.Length == 0 || raw[0] != '<') return raw;
        var depth = 0;
        var end = -1;
        for(var i = 0; i < raw.Length; i++)
        {
            if(raw[i] == '<')
            {
                depth++;
            }
            else if(raw[i] == '>')
            {
                depth--;
                if(depth == 0)
                {
                    end = i;
                    break;
                }
            }
        }
        if(end <= 1) return "";
        var outer = ParseGeneratedMemberName(raw.Substring(1, end - 1));
        var suffix = raw[(end + 1)..];
        if(suffix.StartsWith("g__", StringComparison.Ordinal))
        {
            var bar = suffix.IndexOf('|');
            var local = bar < 0 ? suffix[3..] : suffix[3..bar];
            if(local.Length > 0) return outer.Length == 0 ? local : $"{outer}.{local}";
        }
        // 序號是同一個方法內多個匿名步驟唯一分得開的東西，不能丟。
        if(outer.Length > 0 && suffix.StartsWith("b__", StringComparison.Ordinal))
        {
            return $"{outer}{LambdaOrdinalSeparator}{suffix}";
        }
        return outer;
    }

    /// <summary>剝掉 <c>&lt;&gt;c</c>／<c>&lt;&gt;c__DisplayClassN_M</c>(可能巢狀)，回真正宣告 lambda 的使用者型別。</summary>
    private static Type? UnwrapCompilerGeneratedType(Type? type)
    {
        while(type != null && type.Name.StartsWith("<>", StringComparison.Ordinal))
        {
            type = type.DeclaringType;
        }
        return type;
    }
}
