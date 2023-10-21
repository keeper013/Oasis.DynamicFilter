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

    [Theory]
    [InlineData(3, 1L, 2, true, true)]
    [InlineData(3, 1L, 2, false, false)]
    [InlineData(2, 1L, 3, true, false)]
    [InlineData(2, 1L, 3, false, true)]
    [InlineData(2, null, 3, false, false)]
    [InlineData(2, null, 3, true, true)]
    [InlineData(2, 1L, null, false, false)]
    [InlineData(2, 1L, null, true, true)]
    [InlineData(null, 1L, 3, false, false)]
    [InlineData(null, 1L, 3, true, true)]
    [InlineData(2, null, null, false, false)]
    [InlineData(2, null, null, true, true)]
    public void TestWithoutIncludeNull(int? value, long? min, int? max, bool reverse, bool result)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<FilterRangeEntity<int?>, FilterRangeFilter<long?, int?>>()
                .FilterByRangedFilter(f => f.Min, FilterByRange.LessThan, e => e.Value, FilterByRange.LessThan, f => f.Max, null, f => reverse, f => false, f => false)
                .Finish()
            .Build();
        var entity = new FilterRangeEntity<int?>(value);
        var filter = new FilterRangeFilter<long?, int?>(min, max);
        Assert.Equal(result, expressionMaker.GetFunc<FilterRangeEntity<int?>, FilterRangeFilter<long?, int?>>(filter)(entity));
    }

    [Theory]
    [InlineData(3, 1L, 2, false, true, true)]
    [InlineData(3, 1L, 2, false, false, false)]
    [InlineData(2, 1L, 3, false, true, false)]
    [InlineData(2, 1L, 3, false, false, true)]
    [InlineData(2, null, 3, false, false, false)]
    [InlineData(2, null, 3, false, true, true)]
    [InlineData(2, 1L, null, false, false, false)]
    [InlineData(2, 1L, null, false, true, true)]
    [InlineData(null, 1L, 3, false, false, false)]
    [InlineData(null, 1L, 3, false, true, true)]
    [InlineData(2, null, null, false, false, false)]
    [InlineData(2, null, null, false, true, true)]
    [InlineData(3, 1L, 2, true, true, true)]
    [InlineData(3, 1L, 2, true, false, false)]
    [InlineData(2, 1L, 3, true, true, false)]
    [InlineData(2, 1L, 3, true, false, true)]
    [InlineData(2, null, 3, true, false, false)]
    [InlineData(2, null, 3, true, true, true)]
    [InlineData(2, 1L, null, true, false, false)]
    [InlineData(2, 1L, null, true, true, true)]
    [InlineData(null, 1L, 3, true, false, true)]
    [InlineData(null, 1L, 3, true, true, false)]
    [InlineData(2, null, null, true, false, false)]
    [InlineData(2, null, null, true, true, true)]
    public void TestWithIncludeNull(int? value, long? min, int? max, bool includeNull, bool reverse, bool result)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<FilterRangeEntity<int?>, FilterRangeFilter<long?, int?>>()
                .FilterByRangedFilter(f => f.Min, FilterByRange.LessThan, e => e.Value, FilterByRange.LessThan, f => f.Max, f => includeNull, f => reverse, f => false, f => false)
                .Finish()
            .Build();
        var entity = new FilterRangeEntity<int?>(value);
        var filter = new FilterRangeFilter<long?, int?>(min, max);
        Assert.Equal(result, expressionMaker.GetFunc<FilterRangeEntity<int?>, FilterRangeFilter<long?, int?>>(filter)(entity));
    }

    private static TEntityProperty Test<TEntityProperty, TFilterMinProperty, TFilterMaxProperty>(List<TEntityProperty> entityValues, TFilterMinProperty min, TFilterMaxProperty max)
    {
        var filter = new FilterBuilder()
            .Configure<FilterRangeEntity<TEntityProperty>, FilterRangeFilter<TFilterMinProperty, TFilterMaxProperty>>()
                .FilterByRangedFilter(f => f.Min, FilterByRange.LessThan, e => e.Value, FilterByRange.LessThan, f => f.Max)
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
