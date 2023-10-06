namespace Oasis.DynamicFilter;

using System;
using System.Linq.Expressions;

public interface IFilter
{
    Expression<Func<TEntity, bool>> GetExpression<TEntity, TFilter>(TFilter filter)
        where TEntity : class
        where TFilter : class;

    Func<TEntity, bool> GetFunc<TEntity, TFilter>(TFilter filter)
        where TEntity : class
        where TFilter : class;
}
