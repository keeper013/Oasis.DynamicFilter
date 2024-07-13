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
        Assert.Equal(1, Test<int, float, decimal>(new List<int> { 1, 2, 3, 4, 5 }, 0.5f, 1.1m));
    }

    [Theory]
    [InlineData(3, 1L, 2, false)]
    [InlineData(2, 1L, 3, true)]
    [InlineData(2, null, 3, false)]
    [InlineData(2, 1L, null, false)]
    [InlineData(null, 1L, 3, false)]
    [InlineData(2, null, null, false)]
    public void TestWithoutIncludeNull(int? value, long? min, int? max, bool result)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<FilterRangeEntity<int?>, FilterRangeFilter<long?, int?>>()
                .Filter(f => e => f.Min < e.Value && e.Value < f.Max)
                .Finish()
            .Build();
        var entity = new FilterRangeEntity<int?>(value);
        var filter = new FilterRangeFilter<long?, int?>(min, max);
        Assert.Equal(result, expressionMaker.GetFunc<FilterRangeEntity<int?>, FilterRangeFilter<long?, int?>>(filter)(entity));
    }

    [Theory]
    [InlineData(3, 1L, 2, false, false)]
    [InlineData(2, 1L, 3, false, true)]
    [InlineData(2, null, 3, false, false)]
    [InlineData(2, 1L, null, false, false)]
    [InlineData(null, 1L, 3, false, false)]
    [InlineData(2, null, null, false, false)]
    [InlineData(3, 1L, 2, true, false)]
    [InlineData(2, 1L, 3, true, true)]
    [InlineData(2, null, 3, true, false)]
    [InlineData(2, 1L, null, true, false)]
    [InlineData(null, 1L, 3, true, true)]
    [InlineData(2, null, null, true, false)]
    public void TestWithIncludeNull(int? value, long? min, int? max, bool includeNull, bool result)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<FilterRangeEntity<int?>, FilterRangeFilter<long?, int?>>()
                .Filter(f => e => includeNull ? !e.Value.HasValue || (f.Min < e.Value && e.Value < f.Max) : f.Min < e.Value && e.Value < f.Max)
                .Finish()
            .Build();
        var entity = new FilterRangeEntity<int?>(value);
        var filter = new FilterRangeFilter<long?, int?>(min, max);
        Assert.Equal(result, expressionMaker.GetFunc<FilterRangeEntity<int?>, FilterRangeFilter<long?, int?>>(filter)(entity));
    }

    private static int Test<TEntityProperty, TFilterMinProperty, TFilterMaxProperty>(List<int> entityValues, float min, decimal max)
    {
        var filter = new FilterBuilder()
            .Configure<FilterRangeEntity<int>, FilterRangeFilter<float, decimal>>()
                .Filter(f => e => f.Min < e.Value && e.Value < f.Max)
                .Finish()
            .Build();
        var list = entityValues.Select(v => new FilterRangeEntity<int>(v));
        var filterRangeFilter = new FilterRangeFilter<float, decimal>(min, max);
        var exp = filter.GetExpression<FilterRangeEntity<int>, FilterRangeFilter<float, decimal>>(filterRangeFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value;
    }
}
