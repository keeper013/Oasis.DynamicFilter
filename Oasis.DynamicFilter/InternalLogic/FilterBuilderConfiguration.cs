namespace Oasis.DynamicFilter.InternalLogic;

using System.Collections.Generic;
using System;

internal interface IFilterGlobalConfiguration
{
    ISet<string> ExcludedProperties { get; }

    IList<Func<string, bool>> ExcludedPropertyConditions { get; }
}

internal sealed class FilterBuilderConfiguration : IFilterBuilderConfiguration, IFilterGlobalConfiguration
{
    private readonly IFilterBuilderFactory _factory;

    public FilterBuilderConfiguration(IFilterBuilderFactory factory)
    {
        _factory = factory;
    }

    public ISet<string> ExcludedProperties { get; } = new HashSet<string>();

    public IList<Func<string, bool>> ExcludedPropertyConditions { get; } = new List<Func<string, bool>>();

    public IFilterBuilderConfiguration ExcludeTargetProperty(string entityPropertyName)
    {
        ExcludedProperties.Add(entityPropertyName);
        return this;
    }

    public IFilterBuilderConfiguration ExcludeTargetProperty(Func<string, bool> condition)
    {
        ExcludedPropertyConditions.Add(condition);
        return this;
    }

    public IFilterBuilderFactory Finish()
    {
        return _factory;
    }
}