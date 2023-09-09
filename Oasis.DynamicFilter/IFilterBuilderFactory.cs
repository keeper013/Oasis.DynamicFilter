namespace Oasis.DynamicFilter;

using System;

public interface IFilterBuilderConfiguration
{
    IFilterBuilderConfiguration ExcludeTargetProperty(string entityPropertyName);

    IFilterBuilderConfiguration ExcludeTargetProperty(Func<string, bool> condition);

    IFilterBuilderFactory Finish();
}

public interface IFilterBuilderFactory
{
    IFilterBuilderConfiguration Configure();

    IFilterBuilder Make();
}
