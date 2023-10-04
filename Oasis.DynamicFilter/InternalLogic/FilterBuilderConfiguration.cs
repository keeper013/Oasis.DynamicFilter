namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;
using System.Linq.Expressions;
using Oasis.DynamicFilter.Exceptions;
using System.Linq;

internal interface IGlobalFilterPropertyExcludeByValueManager
{
    bool IsFilterPropertyExcluded<TProperty>(Type filterType, string propertyName, TProperty value);
}

internal sealed class GlobalFilterPropertyExcludeByValueManager : IGlobalFilterPropertyExcludeByValueManager
{
    private readonly IReadOnlyDictionary<Type, Delegate> _equalityManager;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Delegate>>? _byCondition;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, ExcludingOption>>? _byOption;

    public GlobalFilterPropertyExcludeByValueManager(
        IReadOnlyDictionary<Type, Delegate> equalityManager,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Delegate>>? byCondition,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<string, ExcludingOption>>? byOption)
    {
        _equalityManager = equalityManager;
        _byCondition = byCondition;
        _byOption = byOption;
    }

    public bool IsFilterPropertyExcluded<TProperty>(Type filterType, string propertyName, TProperty value)
    {
        return (_byOption != null && _byOption.TryGetValue(filterType, out var po) && po.TryGetValue(propertyName, out var o) && (o == ExcludingOption.Always || (o == ExcludingOption.DefaultValue && (_equalityManager[typeof(TProperty)] as Func<TProperty?, TProperty?, bool>)!(value, default))))
            || (_byCondition != null && (_byCondition.Find(filterType, propertyName) as Func<TProperty, bool>)!(value));
    }
}

internal interface IGlobalExcludedPropertyManager
{
    IGlobalFilterPropertyExcludeByValueManager? FilterPropertyExcludeByValueManager { get; }

    bool IsPropertyExcluded(string propertyName);

    bool IsEntityPropertyExcluded(Type entityType, string propertyName);

    bool IsFilterPropertyExcluded(Type filterType, string propertyName);
}

internal interface IFilterBuilderConfiguration : IGlobalExcludedPropertyManager
{
    ISet<string> ExcludedProperties { get; }

    IList<Func<string, bool>> ExcludedPropertyConditions { get; }

    Dictionary<Type, HashSet<string>> ExcludedEntityProperties { get; }

    Dictionary<Type, Dictionary<string, Delegate>> ExcludedFilterPropertiesByCondition { get; }

    Dictionary<Type, Dictionary<string, ExcludingOption>> ExcludedFilterPropertiesByOption { get; }
}