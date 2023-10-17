namespace Oasis.DynamicFilter.Test;

using System.Linq;
using Oasis.DynamicFilter;

public sealed class FilterRangeEntity<T>
{
    public FilterRangeEntity(T v)
    {
        Value = v;
    }

    public T Value { get; init; }
}

public sealed class FilterRangeFilter<T1, T2>
{
    public FilterRangeFilter(T1 min, T2 max)
    {
        Min = min;
        Max = max;
    }

    public T1 Min { get; init; }

    public T2 Max { get; init; }
}

public sealed class FilterRangeTests
{
    [Fact]
    public void IntFloatDecimalTest()
    {
        Assert.Equal(1, Test(new List<int> { 1, 2, 3, 4, 5 }, 0.5f, 1.1m));
    }

    private TEntityProperty Test<TEntityProperty, TFilterMinProperty, TFilterMaxProperty>(List<TEntityProperty> entityValues, TFilterMinProperty min, TFilterMaxProperty max)
    {
        var filter = new FilterBuilder()
            .Configure<FilterRangeEntity<TEntityProperty>, FilterRangeFilter<TFilterMinProperty, TFilterMaxProperty>>()
                .FilterByRangedFilter(f => f.Min, FilterByRangeType.LessThan, e => e.Value, FilterByRangeType.LessThan, f => f.Max)
                .Finish()
            .Build();
        var list = entityValues.Select(v => new FilterRangeEntity<TEntityProperty>(v));
        var filterRangeFilter = new FilterRangeFilter<TFilterMinProperty, TFilterMaxProperty>(min, max);
        var exp = filter.GetExpression<FilterRangeEntity<TEntityProperty>, FilterRangeFilter<TFilterMinProperty, TFilterMaxProperty>>(filterRangeFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value;
    }
}
