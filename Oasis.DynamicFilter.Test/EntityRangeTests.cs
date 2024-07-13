namespace Oasis.DynamicFilter.Test;

using Newtonsoft.Json.Linq;
using System.Linq;

public sealed class EntityRangeFilter<T>
{
    public EntityRangeFilter(T v)
    {
        Value = v;
    }

    public T Value { get; init; }
}

public sealed class EntityRangeEntity<T1, T2>
{
    public EntityRangeEntity(T1 min, T2 max)
    {
        Min = min;
        Max = max;
    }

    public T1 Min { get; init; }

    public T2 Max { get; init; }
}

public sealed class EntityRangeTests
{
    [Fact]
    public void IntFloatDecimalTest()
    {
        var filter = new FilterBuilder()
            .Configure<EntityRangeEntity<float, decimal>, EntityRangeFilter<int>>()
                .Filter(f => e => e.Min < f.Value)
                .Filter(f => e => f.Value < e.Max)
                .Finish()
            .Build();

        var list = new List<(float, decimal)> { (0.5f, 1.1m), (-2f, -1m), (1.2f, 3m), (1.5f, 4m), (1.2f, 1.3m) }.Select(v => new EntityRangeEntity<float, decimal>(v.Item1, v.Item2));
        var entityRangeFilter = new EntityRangeFilter<int>(1);
        var exp = filter.GetExpression<EntityRangeEntity<float, decimal>, EntityRangeFilter<int>>(entityRangeFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        Assert.Equal(0.5f, result[0].Min);
        Assert.Equal(1.1m, result[0].Max);
    }

    [Theory]
    [InlineData(3, 1L, 2, false)]
    [InlineData(2, 1L, 3, true)]
    [InlineData(2, null, 3, false)]
    [InlineData(2, 1L, null, false)]
    [InlineData(null, 1L, 3, false)]
    public void TestWithoutIncludeNull(int? value, long? min, int? max, bool result)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<EntityRangeEntity<long?, int?>, EntityRangeFilter<int?>>()
                .Filter(f => e => e.Min < f.Value)
                .Filter(f => e => f.Value < e.Max)
                .Finish()
            .Build();
        var entity = new EntityRangeEntity<long?, int?>(min, max);
        var filter = new EntityRangeFilter<int?>(value);
        Assert.Equal(result, expressionMaker.GetFunc<EntityRangeEntity<long?, int?>, EntityRangeFilter<int?>>(filter)(entity));
    }
}
