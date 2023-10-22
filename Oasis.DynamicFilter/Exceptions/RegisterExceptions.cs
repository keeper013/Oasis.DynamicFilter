﻿namespace Oasis.DynamicFilter.Exceptions;

using System;
using System.Reflection;

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

public sealed class InvalidComparisonException : DynamicFilterException
{
    public InvalidComparisonException(Type entityType, string entityPropertyName, FilterBy type, Type filterType, string filterPropertyName)
        : base($"{type} can't be applied to {entityPropertyName} of {entityType.Name} and {filterPropertyName} of {filterType.Name}.")
    {
    }
}

public sealed class InvalidContainException : DynamicFilterException
{
    public InvalidContainException(Type containerType, Type itemType)
        : base($"Element type of {containerType.Name} can't be used to compare with type {itemType.Name}.")
    {
    }
}

public sealed class UnnecessaryIncludeNullException : DynamicFilterException
{
    public UnnecessaryIncludeNullException(Type entityType)
        : base($"Type {entityType.Name} isn't suitable to have an includeNull configuration.")
    {
    }
}

public sealed class InvalidStringTypeException : DynamicFilterException
{
    public InvalidStringTypeException(Type entityType, string entityPropertyName, Type filterType, string filterPropertyName)
        : base($"To filter by string, type of both {entityPropertyName} of {entityType} and {filterPropertyName} of {filterType} must both be string.")
    {
    }
}