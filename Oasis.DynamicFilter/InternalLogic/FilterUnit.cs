namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Data.Common;
using System.Linq.Expressions;

public abstract class FilterUnitBase
{
    protected FilterUnitBase(ParameterExpression parameter)
    {
        Parameter = parameter;
    }

    protected ParameterExpression Parameter { get; }
}

public sealed class CompareFilterUnit<TFilter, TEntity> : FilterUnitBase, IFilterUnit<TEntity>
    where TFilter : class
    where TEntity : class
{
    public CompareFilterUnit(ParameterExpression parameter)
        : base(parameter)
    {
    }

    public Expression<Func<TEntity, bool>>? ToExpression()
    {
        var exp = Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(Expression.Constant(value), Expression.Property(parameter, propertyName)), parameter);
        result = result == null ? exp : Expression.Lambda<Func<TEntity, bool>>(Expression.And(result.Body, exp.Body), parameter);
    }
}
