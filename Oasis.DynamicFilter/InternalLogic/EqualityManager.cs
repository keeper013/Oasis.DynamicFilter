namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Reflection;

internal interface IEqualityManager
{
    bool Equals<T>(T? value1, T? value2);
}

internal interface IEqualityMethodBuilder
{
    MethodMetaData BuildEqualityMethod(Type type);
}

internal sealed class EqualityManager : IEqualityManager
{
    private IReadOnlyDictionary<Type, Delegate> _dict = null!;

    public void Initialize(IReadOnlyDictionary<Type, MethodMetaData> dict, Type type)
    {
        var temp = new Dictionary<Type, Delegate>();
        foreach (var kvp in dict)
        {
            temp.Add(kvp.Key, Delegate.CreateDelegate(kvp.Value.type, type.GetMethod(kvp.Value.name, BindingFlags.Public | BindingFlags.Static)));
        }

        _dict = temp;
    }

    public bool Equals<T>(T? value1, T? value2)
    {
        return _dict.TryGetValue(typeof(T), out var del)
            ? (del as Func<T?, T?, bool>)!(value1, value2)
            : throw new InvalidOperationException($"Equal method for {typeof(T).Name} isn't registered.");
    }
}
