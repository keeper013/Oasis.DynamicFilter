namespace Oasis.DynamicFilter.Exceptions;

using System;

public sealed class FilterNotRegisteredException : DynamicFilterException
{
    public FilterNotRegisteredException(Type entityType, Type filterType)
        : base($"No filtering has been mapped for {filterType.Name} to {entityType.Name}.")
    {
    }
}
