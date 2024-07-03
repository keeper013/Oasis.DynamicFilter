namespace Oasis.DynamicFilter.Exceptions;

using System;
using System.Linq.Expressions;

public sealed class RedundantRegisterException : DynamicFilterException
{
    public RedundantRegisterException(Type entityType, Type filterType)
        : base($"Filtering {entityType.Name} with {filterType.Name} has been registered.")
    {
    }
}

public sealed class InvalidPropertyExpressionException : DynamicFilterException
{
    public InvalidPropertyExpressionException(Expression exp)
        : base($"Expression {exp} doesn't have a property name.")
    {
    }
}

public sealed class TrivialRegisterException : DynamicFilterException
{
    public TrivialRegisterException(Type entityType, Type filterType)
        : base($"Entity class {entityType.Name} doesn't have any common field with filter class {filterType.Name}.")
    {
    }
}