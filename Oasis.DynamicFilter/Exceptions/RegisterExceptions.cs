﻿namespace Oasis.DynamicFilter.Exceptions;

using System;

public sealed class BadFilterException : DynamicFilterException
{
    public BadFilterException(Type entityType, Type filterType)
        : base($"{entityType.Name} doesn't have any property that can be filtered by filter type {filterType.Name}.")
    {
    }
}

public sealed class PropertyMatchingException : DynamicFilterException
{
    public PropertyMatchingException(Type entityType, string entityProperty, Type filterType, string filterProperty)
        : base($"Property \"{entityProperty}\" of type {entityType.Name} doesn't match property \"{filterProperty}\" type of type {filterType.Name}.")
    {
    }
}

public sealed class RedundantMatchingException : DynamicFilterException
{
    public RedundantMatchingException(Type entityType, string entityProperty, Type filterType, string filterProperty)
        : base($"Filter of property \"{entityProperty}\" of entity {entityType.Name} with property \"{filterProperty}\" of filter {filterType.Name} has been configured already")
    {
    }

    public RedundantMatchingException(Type entityType, string entityProperty, Type filterType, string filterProperty1, string filterProperty2)
        : base($"Filter of property \"{entityProperty}\" of entity {entityType.Name} with range \"{filterProperty1}\" and \"{filterProperty2}\" of filter {filterType.Name} has been configured already")
    {
    }

    public RedundantMatchingException(Type entityType, string entityProperty1, string entityProperty2, Type filterType, string filterProperty)
        : base($"Filter with range property \"{entityProperty1}\" and \"{entityProperty2}\" of entity {entityType.Name} with property \"{filterProperty}\" of filter {filterType.Name} has been configured already")
    {
    }
}

public sealed class RedundantRegisterException : DynamicFilterException
{
    public RedundantRegisterException(Type entityType, Type filterType)
        : base($"Filtering {entityType.Name} with {filterType.Name} has been registered.")
    {
    }
}