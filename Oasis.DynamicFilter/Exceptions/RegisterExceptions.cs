namespace Oasis.DynamicFilter.Exceptions;

using System;

public sealed class BadFilterException : DynamicFilterException
{
    public BadFilterException(Type filterType, Type entityType)
        : base($"{filterType.Name} doesn't have any property that can be used to filter {entityType.Name}.")
    {
    }
}

public sealed class RedundantExcludingException : DynamicFilterException
{
    public RedundantExcludingException(Type entityType, string propertyName)
        : base($"Property \"{propertyName}\" of type {entityType.Name} has been excluded already")
    {
    }
}

public sealed class PropertyMatchingException : DynamicFilterException
{
    public PropertyMatchingException(Type filterType, string filterProperty, Type entityType, string entityProperty)
        : base($"Property \"{filterProperty}\" of type {filterType.Name} doesn't match property \"{entityProperty}\" type of type {entityType.Name}.")
    {
    }
}

public sealed class RedundantMatchingException : DynamicFilterException
{
    public RedundantMatchingException(Type filterType, string filterProperty, Type entityType, string entityProperty)
        : base($"Match from property \"{filterProperty}\" of filter {filterType.Name} to property \"{entityProperty}\" of entity {entityType.Name} has been configured already")
    {
    }
}

public sealed class RedundantRegisterException : DynamicFilterException
{
    public RedundantRegisterException(Type filterType, Type entityType)
        : base($"Filtering {entityType.Name} with {filterType.Name} has been registered.")
    {
    }
}

public sealed class FilteringPropertyExcludedException : DynamicFilterException
{
    public FilteringPropertyExcludedException(Type entityType, string propertyName)
        : base($"Specified property \"{propertyName}\" of type {entityType.Name} is configured to be excluded.")
    {
    }
}