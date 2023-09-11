namespace Oasis.DynamicFilter;

using Oasis.DynamicFilter.InternalLogic;

public sealed class FilterBuilderFactory : IFilterBuilderFactory
{
    private FilterBuilderConfiguration? _configuration;

    public IFilterBuilderConfiguration Configure()
    {
        return _configuration ??= new FilterBuilderConfiguration(this);
    }

    public IFilterBuilder Make()
    {
        return new FilterBuilder(_configuration);
    }
}
