namespace Oasis.DynamicFilter.Exceptions;

using System;

public sealed class BadFilterException : DynamicFilterException
{
    public BadFilterException(Type filterType, Type entityType)
        : base($"{filterType.Name} doesn't have any property that can be used to filter {entityType.Name}.")
    {
    }
}
