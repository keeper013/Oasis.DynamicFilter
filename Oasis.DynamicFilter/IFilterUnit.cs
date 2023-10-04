namespace Oasis.DynamicFilter;

using System;
using System.Linq.Expressions;

public interface IFilterUnit<TEntity>
    where TEntity : class
{
    Expression<Func<TEntity, bool>>? ToExpression();
}
