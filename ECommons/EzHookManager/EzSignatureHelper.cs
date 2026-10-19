using ECommons.Logging;
using ECommons.Reflection;
using System;
using System.Reflection;
using TerraFX.Interop.Windows;

namespace ECommons.EzHookManager;
public static class EzSignatureHelper
{
    public static void Initialize(Object obj)
    {
        foreach(var x in obj.GetType().GetFields(ReflectionHelper.AllFlags))
        {
            var attr = x.GetCustomAttribute<EzHookAttribute>();
            try
            {
                if(attr != null)
                {
                    var detourName = attr.Detour ?? (x.Name.EndsWith("Hook") ? x.Name[..^4] : x.Name) + "Detour";
                    //PluginLog.Debug($"Detour: {detourName}");
                    var method = obj.GetType().GetMethod(detourName, ReflectionHelper.AllFlags);
                    CreateAssignEnable(x, obj, attr, Delegate.CreateDelegate(x.FieldType.GenericTypeArguments[0], method!.IsStatic ? null : obj, method));
                }
            }
            catch(TargetInvocationException te)
            {
                te.Log();
                te.InnerException.Log();
            }
            catch(Exception e)
            {
                PluginLog.Error($"An error during initialization of attribute {attr} on {x.Name}");
                e.Log();
            }
        }
    }

    public static void Initialize(Type type)
    {
        foreach(var x in type.GetFields(ReflectionHelper.AllFlags))
        {
            var attr = x.GetCustomAttribute<EzHookAttribute>();
            try
            {
                if(attr != null)
                {
                    var detourName = attr.Detour ?? (x.Name.EndsWith("Hook") ? x.Name[..^4] : x.Name) + "Detour";
                    //PluginLog.Debug($"Detour: {detourName}");
                    var method = type.GetMethod(detourName, ReflectionHelper.AllFlags);
                    CreateAssignEnable(x, null, attr, Delegate.CreateDelegate(x.FieldType.GenericTypeArguments[0], null, method));
                }
            }
            catch(TargetInvocationException te)
            {
                te.Log();
                te.InnerException.Log();
            }
            catch(Exception e)
            {
                PluginLog.Error($"An error during initialization of attribute {attr} on {x.Name}");
                e.Log();
            }
        }
    }

    /// <summary>
    /// 先以未啟用狀態建立並寫進欄位，再依 <see cref="EzHookAttribute.AutoEnable"/> 啟用：
    /// 啟用那一刻遊戲執行緒就可能呼叫 detour，而 detour 會讀這個欄位的 Original。
    /// 啟用擲例外時釋放這個 hook、欄位還原成原值，例外原樣（TargetInvocationException）往外丟。
    /// </summary>
    private static void CreateAssignEnable(FieldInfo field, object? target, EzHookAttribute attr, Delegate detour)
    {
        var hook = Activator.CreateInstance(field.FieldType, attr.Signature, detour, false, attr.Offset)!;
        var previous = field.GetValue(target);
        field.SetValue(target, hook);
        if(!attr.AutoEnable) return;
        try
        {
            field.FieldType.GetMethod(nameof(EzHook<Action>.Enable), Type.EmptyTypes)!.Invoke(hook, null);
        }
        catch
        {
            try
            {
                field.FieldType.GetMethod(nameof(EzHook<Action>.Disable), Type.EmptyTypes)!.Invoke(hook, null);
                field.SetValue(target, previous);
            }
            catch(Exception e)
            {
                // 釋放失敗＝hook 可能仍在生效：欄位保留這個 hook，detour 才不會讀到 null。
                PluginLog.Error($"Failed to dispose the hook on {field.Name} after enabling it failed; the field keeps it");
                e.Log();
            }
            throw;
        }
    }
}
