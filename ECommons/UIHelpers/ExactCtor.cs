using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ECommons.UIHelpers;
#nullable disable

// 契約：Create 非 null 時，它呼叫的建構子與 Activator.CreateInstance(typeof(T), 參數...) 綁定到的是同一個，
// 建構子擲的例外由 New 包成 TargetInvocationException（與 Activator 相同）。綁定結果不確定時（abstract、
// 沒有參數型別逐字相同的公開建構子、任何公開建構子帶 params）Create 為 null，呼叫端照舊走 Activator。
internal static class ExactCtor
{
    internal static Delegate Build(Type type, Type delegateType, Type[] signature)
    {
        try
        {
            if(type.IsAbstract || type.IsInterface || type.ContainsGenericParameters) return null;
            ConstructorInfo hit = null;
            foreach(var c in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var ps = c.GetParameters();
                if(ps.Length > 0 && ps[^1].IsDefined(typeof(ParamArrayAttribute), false)) return null;
                if(ps.Select(p => p.ParameterType).SequenceEqual(signature)) hit = c;
            }
            if(hit == null) return null;
            var args = signature.Select(Expression.Parameter).ToArray();
            return Expression.Lambda(delegateType, Expression.New(hit, args), args).Compile();
        }
        catch(Exception)
        {
            return null;
        }
    }
}

internal static class ExactCtor<T, A1>
{
    internal static readonly Func<A1, T> Create = (Func<A1, T>)ExactCtor.Build(typeof(T), typeof(Func<A1, T>), [typeof(A1)]);

    internal static T New(A1 a1)
    {
        try
        {
            return Create(a1);
        }
        catch(Exception e)
        {
            throw new TargetInvocationException(e);
        }
    }
}

internal static class ExactCtor<T, A1, A2>
{
    internal static readonly Func<A1, A2, T> Create = (Func<A1, A2, T>)ExactCtor.Build(typeof(T), typeof(Func<A1, A2, T>), [typeof(A1), typeof(A2)]);

    internal static T New(A1 a1, A2 a2)
    {
        try
        {
            return Create(a1, a2);
        }
        catch(Exception e)
        {
            throw new TargetInvocationException(e);
        }
    }
}
