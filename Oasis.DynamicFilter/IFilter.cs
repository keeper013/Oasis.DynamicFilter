namespace Oasis.DynamicFilter;

using System;
using System.Linq.Expressions;

public interface IFilter
{
    Expression<Func<TEntity, bool>> GetExpression<TFilter, TEntity>(TFilter filter)
        where TFilter : class
        where TEntity : class;

    Func<TEntity, bool> GetFunc<TFilter, TEntity>(TFilter filter)
        where TFilter : class
        where TEntity : class;
}
