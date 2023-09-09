namespace Oasis.DynamicFilter;

using Oasis.DynamicFilter.InternalLogic;

public sealed class FilterBuilderFactory : IFilterBuilderFactory
{
    private readonly FilterBuilderConfiguration _configuration;

    public FilterBuilderFactory()
    {
        _configuration = new FilterBuilderConfiguration(this);
    }

    public IFilterBuilderConfiguration Configure()
    {
        return _configuration;
    }

    public IFilterBuilder Make()
    {
        return new FilterBuilder(_configuration);
    }
}
