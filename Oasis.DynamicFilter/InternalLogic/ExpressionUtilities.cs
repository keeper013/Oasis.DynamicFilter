namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public sealed class ExpressionParameterReplacer
{
    private readonly ReplaceParameterVisitor _visitor;

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

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == Source ? _target : base.VisitParameter(node);
        }
    }
}

public static class ExpressionUtilities
{
    private static readonly MethodInfo EnumerableContains = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => string.Equals(m.Name, nameof(Enumerable.Contains)) && m.GetParameters().Length == 2);
    private static readonly IReadOnlyDictionary<Operator, Func<Expression, Expression, BinaryExpression>> _compareFunctions = new Dictionary<Operator, Func<Expression, Expression, BinaryExpression>>
    {
        { Operator.Equality, Expression.Equal },
        { Operator.GreaterThan, Expression.GreaterThan },
        { Operator.GreaterThanOrEqual, Expression.GreaterThanOrEqual },
        { Operator.InEquality, Expression.NotEqual },
        { Operator.LessThan, Expression.LessThan },
        { Operator.LessThanOrEqual, Expression.LessThanOrEqual },
    };

    private static readonly IReadOnlyDictionary<StringOperator, MethodInfo> _compareStringMethods = new Dictionary<StringOperator, MethodInfo>
    {
        { StringOperator.In, typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) }, null) },
        { StringOperator.Contains, typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) }, null) },
        { StringOperator.Equality, typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) }, null) },
        { StringOperator.StartsWith, typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) }, null) },
        { StringOperator.EndsWith, typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) }, null) },
    };

    public static void BuildCompareExpression<TFilter, TFilterProperty>(ExpressionParameterReplacer parameterReplacer, TFilter filter, CompareData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        if (data.ignore == null || !data.ignore(filter))
        {
            var entityPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityPropertyExpression);
            var value = (data.filterFunc as Func<TFilter, TFilterProperty>)!.Invoke(filter);
            var reverse = data.reverse?.Invoke(filter) ?? false;
            Expression exp = _compareFunctions[reverse ? data.op.GetReversed() : data.op](
                data.entityPropertyConvertTo != null ? Expression.Convert(entityPropertyExpression, data.entityPropertyConvertTo!) : entityPropertyExpression,
                data.filterPropertyConvertTo != null ? Expression.Convert(Expression.Constant(value, data.filterPropertyType), data.filterPropertyConvertTo!) : Expression.Constant(value, data.filterPropertyType));

            if (data.includeNull != null)
            {
                exp = data.includeNull(filter) ^ reverse
                    ? Expression.OrElse(Expression.Equal(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)), exp)
                    : Expression.AndAlso(Expression.NotEqual(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)), exp);
            }

            result = result == null ? exp : Expression.AndAlso(result, exp);
        }
    }

    public static void BuildStringCompareExpression<TFilter>(ExpressionParameterReplacer parameterReplacer, TFilter filter, CompareStringData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        if (data.ignore == null || !data.ignore(filter))
        {
            var compareType = GetBasicStringCompareType(data.op, out var isReversed);
            var methodInfo = _compareStringMethods[compareType];
            var value = (data.filterFunc as Func<TFilter, string>)!.Invoke(filter);
            var entityPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityPropertyExpression);
            Expression exp;
            if (value == null)
            {
                exp = data.includeNull != null
                    ? data.includeNull(filter)
                        ? Expression.OrElse(Expression.Equal(entityPropertyExpression, Expression.Constant(null, typeof(string))), Expression.Constant(isReversed))
                        : Expression.AndAlso(Expression.NotEqual(entityPropertyExpression, Expression.Constant(null, typeof(string))), Expression.Constant(isReversed))
                    : Expression.Constant(isReversed);
            }
            else
            {
                Expression compareExpression = compareType == StringOperator.In
                    ? Expression.Call(Expression.Constant(value, typeof(string)), methodInfo, entityPropertyExpression)
                    : Expression.Call(entityPropertyExpression, methodInfo, Expression.Constant(value, typeof(string)));

                if (isReversed)
                {
                    compareExpression = Expression.Not(compareExpression);
                }

                exp = data.includeNull != null && data.includeNull(filter)
                    ? Expression.OrElse(Expression.Equal(entityPropertyExpression, Expression.Constant(null, typeof(string))), compareExpression)
                    : Expression.AndAlso(Expression.NotEqual(entityPropertyExpression, Expression.Constant(null, typeof(string))), compareExpression);
            }

            if (data.reverse?.Invoke(filter) ?? false)
            {
                exp = Expression.Not(exp);
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
        if (data.ignore == null || !data.ignore(filter))
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
    }

    public static void BuildInArrayExpression<TFilter, TFilterProperty>(ExpressionParameterReplacer parameterReplacer, TFilter filter, InData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        if (data.ignore == null || !data.ignore(filter))
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
    }

    public static void BuildFilterRangeExpression<TFilter, TFilterMinProperty, TFilterMaxProperty>(ExpressionParameterReplacer parameterReplacer, TFilter filter, FilterRangeData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        Expression minExp = null!;
        Expression maxExp = null!;

        var ignoreMin = data.ignoreMin != null && data.ignoreMin(filter);
        var entityPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityPropertyExpression);
        if (ignoreMin)
        {
            var min = (data.filterMinFunc as Func<TFilter, TFilterMinProperty>)!.Invoke(filter);
            minExp = _compareFunctions[Convert(data.minOp)](
                data.entityMinPropertyConvertTo != null ? Expression.Convert(entityPropertyExpression, data.entityMinPropertyConvertTo!) : entityPropertyExpression,
                data.filterMinPropertyConvertTo != null ? Expression.Convert(Expression.Constant(min, typeof(TFilterMinProperty)), data.filterMinPropertyConvertTo!) : Expression.Constant(min, data.filterMinPropertyType));
        }

        var ignoreMax = data.ignoreMax != null && data.ignoreMax(filter);
        if (ignoreMax)
        {
            var max = (data.filterMaxFunc as Func<TFilter, TFilterMinProperty>)!.Invoke(filter);
            maxExp = _compareFunctions[Opposite(data.maxOp)](
                data.entityMaxPropertyConvertTo != null ? Expression.Convert(entityPropertyExpression, data.entityMaxPropertyConvertTo!) : entityPropertyExpression,
                data.filterMaxPropertyConvertTo != null ? Expression.Convert(Expression.Constant(max, typeof(TFilterMaxProperty)), data.filterMaxPropertyConvertTo!) : Expression.Constant(max, data.filterMaxPropertyType));
        }

        Expression? exp;
        if (ignoreMin)
        {
            exp = ignoreMax ? null : maxExp;
        }
        else if (ignoreMax)
        {
            exp = minExp;
        }
        else
        {
            exp = Expression.AndAlso(minExp, maxExp);
        }

        if (exp != null)
        {
            if (data.includeNull != null)
            {
                exp = data.includeNull(filter)
                    ? Expression.OrElse(Expression.Equal(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)), exp)
                    : Expression.AndAlso(Expression.NotEqual(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)), exp);
            }

            if (data.reverse?.Invoke(filter) ?? false)
            {
                exp = Expression.Not(exp);
            }

            result = result == null ? exp : Expression.AndAlso(result, exp);
        }
    }

    public static void BuildEntityRangeExpression<TFilter, TFilterProperty>(ExpressionParameterReplacer parameterReplacer, TFilter filter, EntityRangeData<TFilter> data, ref Expression? result)
        where TFilter : class
    {
        if (data.ignore == null || !data.ignore(filter))
        {
            var filterPropertyType = typeof(TFilterProperty);
            var value = (data.filterFunc as Func<TFilter, TFilterProperty>)!.Invoke(filter);
            var entityMinPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityMinPropertyExpression);
            Expression minExp = _compareFunctions[Convert(data.minOp)](
                data.entityMinPropertyConvertTo != null ? Expression.Convert(entityMinPropertyExpression, data.entityMinPropertyConvertTo!) : entityMinPropertyExpression,
                data.filterMinPropertyConvertTo != null ? Expression.Convert(Expression.Constant(value, filterPropertyType), data.filterMinPropertyConvertTo!) : Expression.Constant(value, filterPropertyType));

            if (data.includeNullMin != null)
            {
                minExp = data.includeNullMin(filter)
                    ? Expression.OrElse(Expression.Equal(entityMinPropertyExpression, Expression.Constant(null, data.entityMinPropertyType)), minExp)
                    : Expression.AndAlso(Expression.NotEqual(entityMinPropertyExpression, Expression.Constant(null, data.entityMinPropertyType)), minExp);
            }

            var entityMaxPropertyExpression = parameterReplacer.GetBodyWithReplacedParameter(data.entityMaxPropertyExpression);
            Expression maxExp = _compareFunctions[Opposite(data.maxOp)](
                data.entityMaxPropertyConvertTo != null ? Expression.Convert(entityMaxPropertyExpression, data.entityMaxPropertyConvertTo!) : entityMaxPropertyExpression,
                data.filterMaxPropertyConvertTo != null ? Expression.Convert(Expression.Constant(value, filterPropertyType), data.filterMaxPropertyConvertTo!) : Expression.Constant(value, filterPropertyType));

            if (data.includeNullMax != null)
            {
                maxExp = data.includeNullMax(filter)
                    ? Expression.OrElse(Expression.Equal(entityMaxPropertyExpression, Expression.Constant(null, data.entityMaxPropertyType)), maxExp)
                    : Expression.AndAlso(Expression.NotEqual(entityMaxPropertyExpression, Expression.Constant(null, data.entityMaxPropertyType)), maxExp);
            }

            Expression exp = Expression.AndAlso(minExp, maxExp);
            if (data.reverse?.Invoke(filter) ?? false)
            {
                exp = Expression.Not(exp);
            }

            result = result == null ? exp : Expression.AndAlso(result, exp);
        }
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
        var notContains = data.op == Operator.NotContains;

        // can't call contains, if entity property isn't null then not contains
        if (data.nullValueNotCovered && value == null)
        {
            exp = data.includeNull != null && data.includeNull(filter)
                ? Expression.OrElse(Expression.Equal(entityPropertyExpression, Expression.Constant(null, containerType)), Expression.Constant(notContains))
                : Expression.Constant(notContains);
        }
        else
        {
            var containsExpression = makeContainsExpression();
            if (notContains)
            {
                containsExpression = Expression.Not(containsExpression);
            }

            exp = data.includeNull != null
                ? data.includeNull(filter)
                    ? Expression.OrElse(Expression.Equal(entityPropertyExpression, Expression.Constant(null, containerType)), containsExpression)
                    : Expression.AndAlso(Expression.NotEqual(entityPropertyExpression, Expression.Constant(null, containerType)), containsExpression)
                : Expression.Condition(
                    Expression.Equal(entityPropertyExpression, Expression.Constant(null, containerType)),
                    Expression.Constant(notContains),
                    containsExpression);
        }

        if (data.reverse?.Invoke(filter) ?? false)
        {
            exp = Expression.Not(exp);
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
        var notIn = data.op == Operator.NotIn;
        Expression exp;
        if (value == null)
        {
            exp = data.includeNull != null
                ? data.includeNull(filter)
                    ? Expression.OrElse(Expression.Equal(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)), Expression.Constant(notIn))
                    : Expression.AndAlso(Expression.NotEqual(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)), Expression.Constant(notIn))
                : Expression.Constant(notIn);
        }
        else
        {
            Expression containsExpression = makeContainsExpression();
            if (notIn)
            {
                containsExpression = Expression.Not(containsExpression);
            }

            if (data.nullValueNotCovered)
            {
                exp = Expression.Condition(
                    Expression.Equal(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)),
                    Expression.Constant(data.includeNull != null ? data.includeNull(filter) : notIn),
                    containsExpression);
            }
            else
            {
                exp = data.includeNull != null
                    ? data.includeNull(filter)
                        ? Expression.OrElse(Expression.Equal(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)), containsExpression)
                        : Expression.AndAlso(Expression.NotEqual(entityPropertyExpression, Expression.Constant(null, data.entityPropertyType)), containsExpression)
                    : containsExpression;
            }
        }

        if (data.reverse?.Invoke(filter) ?? false)
        {
            exp = Expression.Not(exp);
        }

        result = result == null ? exp : Expression.AndAlso(result, exp);
    }

    private static StringOperator GetBasicStringCompareType(StringOperator type, out bool isReversed)
    {
        switch (type)
        {
            case StringOperator.NotIn:
                isReversed = true;
                return StringOperator.In;
            case StringOperator.InEquality:
                isReversed = true;
                return StringOperator.Equality;
            case StringOperator.NotEndsWith:
                isReversed = true;
                return StringOperator.EndsWith;
            case StringOperator.NotStartsWith:
                isReversed = true;
                return StringOperator.StartsWith;
            case StringOperator.NotContains:
                isReversed = true;
                return StringOperator.Contains;
            default:
                isReversed = false;
                return type;
        }
    }

    private static Operator GetReversed(this Operator filterType)
    {
        return filterType switch
        {
            Operator.Contains => Operator.NotContains,
            Operator.Equality => Operator.InEquality,
            Operator.GreaterThan => Operator.LessThanOrEqual,
            Operator.GreaterThanOrEqual => Operator.LessThan,
            Operator.In => Operator.NotIn,
            Operator.InEquality => Operator.Equality,
            Operator.LessThan => Operator.GreaterThanOrEqual,
            Operator.LessThanOrEqual => Operator.GreaterThan,
            Operator.NotContains => Operator.Contains,
            _ => Operator.In,
        };
    }

    private static Operator Opposite(RangeOperator type)
    {
        return type switch
        {
            RangeOperator.LessThan => Operator.GreaterThan,
            _ => Operator.GreaterThanOrEqual,
        };
    }

    private static Operator Convert(RangeOperator type)
    {
        return type switch
        {
            RangeOperator.LessThan => Operator.LessThan,
            _ => Operator.LessThanOrEqual,
        };
    }
}
