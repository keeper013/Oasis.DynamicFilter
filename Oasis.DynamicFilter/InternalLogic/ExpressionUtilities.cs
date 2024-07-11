namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public sealed class ExpressionParameterReplacer
{
    private readonly ReplaceParameterVisitor _visitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionParameterReplacer"/> class.
    /// This constructor will be called by ilGenerator emitted code provided via reflection, so it's not reference by any source code.
    /// Deleting it will cause the program to malfunction.
    /// </summary>
    /// <param name="expression">Expression for parameter.</param>
    public ExpressionParameterReplacer(ParameterExpression expression)
    {
        _visitor = new (expression);
    }

    public Expression GetBodyWithReplacedParameter(LambdaExpression expression)
    {
        _visitor.Source = expression.Parameters[0];
        return _visitor.Visit(expression.Body);
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _target;

        public ReplaceParameterVisitor(ParameterExpression target)
        {
            _target = target;
        }

        public ParameterExpression Source { private get; set; } = null!;

        /// <summary>
        /// This method is necessary for the class, though not reference anywhere in the program.
        /// </summary>
        /// <param name="node">Expression Node.</param>
        /// <returns>Expression of parameter.</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == Source ? _target : base.VisitParameter(node);
        }
    }
}

public static class ExpressionUtilities
{
    private static readonly MethodInfo EnumerableContains = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => string.Equals(m.Name, nameof(Enumerable.Contains)) && m.GetParameters().Length == 2);

    public static void BuildCompareExpression<TFilter, TFilterProperty>(ExpressionParameterReplacer parameterReplacer, TFilter filter, CompareData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        if (data.ignore == null || !data.ignore(filter))
        {
            var entityPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityPropertyExpression);
            var value = (data.filterFunc as Func<TFilter, TFilterProperty>)!.Invoke(filter);
            Expression exp = Expression.Equal(
                data.entityPropertyConvertTo != null ? Expression.Convert(entityPropertyExpression, data.entityPropertyConvertTo!) : entityPropertyExpression,
                data.filterPropertyConvertTo != null ? Expression.Convert(Expression.Constant(value, data.filterPropertyType), data.filterPropertyConvertTo!) : Expression.Constant(value, data.filterPropertyType));

            result = result == null ? exp : Expression.AndAlso(result, exp);
        }
    }

    public static void BuildStringCompareExpression<TFilter>(ExpressionParameterReplacer parameterReplacer, TFilter filter, CompareStringData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        if (data.ignore == null || !data.ignore(filter))
        {
            var methodInfo = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) }, null);
            var value = (data.filterFunc as Func<TFilter, string>)!.Invoke(filter);
            var entityPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityPropertyExpression);
            Expression exp;
            if (value == null)
            {
                exp = Expression.Constant(false);
            }
            else
            {
                Expression compareExpression = Expression.Call(entityPropertyExpression, methodInfo, Expression.Constant(value, typeof(string)));
                exp = Expression.AndAlso(Expression.NotEqual(entityPropertyExpression, Expression.Constant(null, typeof(string))), compareExpression);
            }

            result = result == null ? exp : Expression.AndAlso(result, exp);
        }
    }

    public static void BuildCollectionContainsExpression<TFilter, TFilterProperty>(ExpressionParameterReplacer parameterReplacer, TFilter filter, ContainData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        if (data.ignore == null || !data.ignore(filter))
        {
            var collectionType = typeof(ICollection<>).MakeGenericType(data.entityPropertyItemType);
            var value = (data.filterFunc as Func<TFilter, TFilterProperty>)!.Invoke(filter);
            var entityPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityPropertyExpression);
            Expression MakeContainsExpression()
            {
                var filterPropertyType = typeof(TFilterProperty);
                var containsMethod = collectionType!.GetMethod("Contains")!;
                return Expression.Call(
                    entityPropertyExpression,
                    containsMethod,
                    data.filterPropertyConvertTo == null ? Expression.Constant(value, filterPropertyType) : Expression.Convert(Expression.Constant(value, filterPropertyType), data.filterPropertyConvertTo));
            }

            BuildContainsExpression(entityPropertyExpression, filter, value, data, collectionType, MakeContainsExpression, ref result);
        }
    }

    public static void BuildArrayContainsExpression<TFilter, TFilterProperty>(ExpressionParameterReplacer parameterReplacer, TFilter filter, ContainData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        if (data.ignore == null || !data.ignore(filter))
        {
            var arrayType = data.entityPropertyItemType.MakeArrayType();
            var value = (data.filterFunc as Func<TFilter, TFilterProperty>)!.Invoke(filter);
            var entityPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityPropertyExpression);
            Expression MakeContainsExpression()
            {
                var filterPropertyType = typeof(TFilterProperty);
                var containsMethod = EnumerableContains.MakeGenericMethod(data.entityPropertyItemType);
                return Expression.Call(
                    containsMethod,
                    entityPropertyExpression,
                    data.filterPropertyConvertTo == null ? Expression.Constant(value, filterPropertyType) : Expression.Convert(Expression.Constant(value, filterPropertyType), data.filterPropertyConvertTo));
            }

            BuildContainsExpression(entityPropertyExpression, filter, value, data, arrayType, MakeContainsExpression, ref result);
        }
    }

    public static void BuildInCollectionExpression<TFilter, TFilterProperty>(ExpressionParameterReplacer parameterReplacer, TFilter filter, InData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        var value = (data.filterFunc as Func<TFilter, TFilterProperty>)!.Invoke(filter);
        var entityPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityPropertyExpression);
        Expression MakeContainsExpression()
        {
            var collectionType = typeof(ICollection<>).MakeGenericType(data.filterPropertyItemType);
            var containsMethod = collectionType.GetMethod("Contains")!;
            return Expression.Call(
                Expression.Constant(value),
                containsMethod,
                data.entityPropertyConvertTo == null ? entityPropertyExpression : Expression.Convert(entityPropertyExpression, data.entityPropertyConvertTo));
        }

        BuildInExpression(entityPropertyExpression, filter, value, data, MakeContainsExpression, ref result);
    }

    public static void BuildInArrayExpression<TFilter, TFilterProperty>(ExpressionParameterReplacer parameterReplacer, TFilter filter, InData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        var value = (data.filterFunc as Func<TFilter, TFilterProperty>)!.Invoke(filter);
        var entityPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityPropertyExpression);
        Expression MakeContainsExpression()
        {
            var containsMethod = EnumerableContains.MakeGenericMethod(data.filterPropertyItemType);
            return Expression.AndAlso(
                Expression.NotEqual(Expression.Constant(value), Expression.Constant(null, data.filterPropertyItemType.MakeArrayType())),
                Expression.Call(
                    containsMethod,
                    Expression.Constant(value),
                    data.entityPropertyConvertTo == null ? entityPropertyExpression : Expression.Convert(entityPropertyExpression, data.entityPropertyConvertTo)));
        }

        BuildInExpression(entityPropertyExpression, filter, value, data, MakeContainsExpression, ref result);
    }

    private static void BuildContainsExpression<TFilter, TFilterProperty>(
        Expression entityPropertyExpression,
        TFilter filter,
        TFilterProperty value,
        ContainData<TFilter> data,
        Type containerType,
        Func<Expression> makeContainsExpression,
        ref Expression? result)
        where TFilter : class
    {
        Expression exp;

        // can't call contains, if entity property isn't null then not contains
        if (data.nullValueNotCovered && value == null)
        {
            exp = Expression.Constant(false);
        }
        else
        {
            var containsExpression = makeContainsExpression();

            exp = Expression.Condition(
                    Expression.Equal(entityPropertyExpression, Expression.Constant(null, containerType)),
                    Expression.Constant(false),
                    containsExpression);
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    private static void BuildInExpression<TFilter, TFilterProperty>(
        Expression entityPropertyExpression,
        TFilter filter,
        TFilterProperty value,
        InData<TFilter> data,
        Func<Expression> makeContainsExpression,
        ref Expression? result)
        where TFilter : class
    {
        Expression exp;
        if (value == null)
        {
            exp = Expression.Constant(false);
        }
        else
        {
            Expression containsExpression = makeContainsExpression();

            if (data.nullValueNotCovered)
            {
                exp = Expression.Condition(
                    Expression.Equal(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)),
                    Expression.Constant(false),
                    containsExpression);
            }
            else
            {
                exp = containsExpression;
            }
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }
}
