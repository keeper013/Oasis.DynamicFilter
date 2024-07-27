namespace Oasis.DynamicFilter.InternalLogic;

using Oasis.DynamicFilter.Exceptions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System;

internal sealed class FilterConfiguration<TEntity, TFilter> : IFilterConfigurationBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    private readonly FilterBuilder _builder;
    private readonly FilterTypeBuilder<TEntity, TFilter> _filterTypeBuilder;
    private readonly HashSet<string> _configuredEntityProperties = new ();

    private readonly List<(Func<TFilter, Expression<Func<TEntity, bool>>>, Func<TFilter, bool>?)> _filterMethods = new ();

    public FilterConfiguration(FilterBuilder builder, FilterTypeBuilder<TEntity, TFilter> filterTypeBuilder)
    {
        _builder = builder;
        _filterTypeBuilder = filterTypeBuilder;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> ExcludeProperties<TEntityProperty>(params Expression<Func<TEntity, TEntityProperty>>[] entityPropertyExpressions)
    {
        foreach (var entityPropertyExpression in entityPropertyExpressions)
        {
            var entityPropertyName = GetPropertyName(entityPropertyExpression);
            if (entityPropertyName == null)
            {
                throw new InvalidPropertyExpressionException(entityPropertyExpression);
            }

            _configuredEntityProperties.Add(entityPropertyName);
        }

        return this;
    }

    public IFilterConfigurationBuilder<TEntity, TFilter> Filter(Func<TFilter, Expression<Func<TEntity, bool>>> filterMethod, Func<TFilter, bool>? applyFilter)
    {
        _filterMethods.Add((filterMethod, applyFilter));
        return this;
    }

    public IFilterBuilder Finish()
    {
        var dele0 = BuildTypeAndFunction();
        if (dele0 == null)
        {
            if (_filterMethods.Count == 0)
            {
                throw new TrivialRegisterException(typeof(TEntity), typeof(TFilter));
            }
            else
            {
                _builder.Add(typeof(TEntity), typeof(TFilter), CombineFunctions(_filterMethods));
            }
        }
        else if (_filterMethods.Count > 0)
        {
            _filterMethods.Insert(0, ((dele0 as Func<TFilter, Expression<Func<TEntity, bool>>>)!, null));
            _builder.Add(typeof(TEntity), typeof(TFilter), CombineFunctions(_filterMethods));
        }
        else
        {
            _builder.Add(typeof(TEntity), typeof(TFilter), dele0);
        }

        return _builder;
    }

    private static string? GetPropertyName<TClass, TProperty>(Expression<Func<TClass, TProperty>> expression)
        where TClass : class
    {
        MemberExpression? memberExpression = null;
        if (expression.Body.NodeType == ExpressionType.Convert)
        {
            var body = (UnaryExpression)expression.Body;
            memberExpression = body.Operand as MemberExpression;
        }
        else if (expression.Body.NodeType == ExpressionType.MemberAccess)
        {
            memberExpression = expression.Body as MemberExpression;
        }

        if (memberExpression == null)
        {
            return null;
        }

        if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
        {
            return null;
        }

        return (memberExpression.Member as PropertyInfo)?.Name;
    }

    private static Func<TFilter, Expression<Func<TEntity, bool>>> CombineFunctions(IList<(Func<TFilter, Expression<Func<TEntity, bool>>>, Func<TFilter, bool>?)> list)
    {
        return (filter) =>
        {
            Expression<Func<TEntity, bool>>? exp = null;
            foreach (var item in list)
            {
                if (item.Item2 == null || item.Item2(filter))
                {
                    exp = exp == null ? item.Item1(filter) : AndAlso(exp, item.Item1(filter));
                }
            }

            return exp ?? ((TEntity e) => true);
        };
    }

    private static Expression<Func<T, bool>> AndAlso<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var param = Expression.Parameter(typeof(T), "entity");
        var body = Expression.AndAlso(Expression.Invoke(left, param), Expression.Invoke(right, param));
        var lambda = Expression.Lambda<Func<T, bool>>(body, param);
        return lambda;
    }

    private Delegate? BuildTypeAndFunction()
    {
        var type = _filterTypeBuilder.Build(_configuredEntityProperties);
        if (type != null)
        {
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(TFilter), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(TEntity), typeof(bool))));
            return Delegate.CreateDelegate(delegateType, type.GetMethod(FilterTypeBuilder<TEntity, TFilter>.FilterMethodName, Utilities.PublicStatic));
        }

        return null;
    }
}