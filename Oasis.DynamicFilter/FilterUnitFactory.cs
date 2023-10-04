namespace Oasis.DynamicFilter;

using System.Linq.Expressions;
using System;

public class FilterUnitFactory
{
    public IFilterUnit<TEntity> MakeCompareFilter<TEntity, TEntityProperty, TFilter, TFilterProperty>(
        Expression<Func<TFilter, TFilterProperty>> filterExp,
        Expression<Func<TEntity, TEntityProperty>> entityExp,
        CompareFilterType type,
        Func<TFilterProperty, bool>? ignoreIf)
        where TFilter : class
        where TEntity : class
    {

    }

    public static Func<TFilterProperty, bool>? IgnoreIfIsDefault<TFilterProperty>()
    {

    }

    public static Func<TFilterProperty, bool>? NeverIgnore<TFilterProperty>()
    {
        return (prop) => false;
    }

    public static Func<TFilterProperty, bool>? AlwaysIgnore<TFilterProperty>()
    {
        return (prop) => true;
    }
}
