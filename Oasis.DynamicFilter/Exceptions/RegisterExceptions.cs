namespace Oasis.DynamicFilter.Exceptions;

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

public sealed class RedundantRegisterException : DynamicFilterException
{
    public RedundantRegisterException(Type entityType, Type filterType)
        : base($"Filtering {entityType.Name} with {filterType.Name} has been registered.")
    {
    }
}

public sealed class InvalidComparisonException : DynamicFilterException
{
    public InvalidComparisonException(Type entityPropertyType, Operator type, Type filterPropertyType)
        : base($"{type} can't be applied to {entityPropertyType.Name} and {filterPropertyType.Name}.")
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