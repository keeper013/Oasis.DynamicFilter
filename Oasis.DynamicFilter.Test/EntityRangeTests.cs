namespace Oasis.DynamicFilter.Test.BasicTests;

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
        var result = Test<float, decimal, int>(new List<(float, decimal)> { (0.5f, 1.1m), (-2f, -1m), (1.2f, 3m), (1.5f, 4m), (1.2f, 1.3m) }, 1);
        Assert.Equal(0.5f, result.Item1);
        Assert.Equal(1.1m, result.Item2);
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
    public void TestWithoutIncludeNull(int? value, long? min, int? max, bool reverse, bool result)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<EntityRangeEntity<long?, int?>, EntityRangeFilter<int?>>()
                .FilterByRangedEntity(e => e.Min, FilterByRangeType.LessThan, f => f.Value, FilterByRangeType.LessThan, e => e.Max, null, null, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new EntityRangeEntity<long?, int?>(min, max);
        var filter = new EntityRangeFilter<int?>(value);
        Assert.Equal(result, expressionMaker.GetFunc<EntityRangeEntity<long?, int?>, EntityRangeFilter<int?>>(filter)(entity));
    }

    [Theory]
    [InlineData(3, 1L, 2, true, true, true, true)]
    [InlineData(3, 1L, 2, true, true, false, false)]
    [InlineData(2, 1L, 3, true, true, true, false)]
    [InlineData(2, 1L, 3, true, true, false, true)]
    [InlineData(2, null, 3, true, true, false, true)]
    [InlineData(2, null, 3, true, true, true, false)]
    [InlineData(2, 1L, null, true, true, false, true)]
    [InlineData(2, 1L, null, true, true, true, false)]
    [InlineData(null, 1L, 3, true, true, false, false)]
    [InlineData(null, 1L, 3, true, true, true, true)]
    [InlineData(2, null, null, true, true, false, true)]
    [InlineData(2, null, null, true, true, true, false)]
    [InlineData(3, 1L, 2, true, false, true, true)]
    [InlineData(3, 1L, 2, true, false, false, false)]
    [InlineData(2, 1L, 3, true, false, true, false)]
    [InlineData(2, 1L, 3, true, false, false, true)]
    [InlineData(2, null, 3, true, false, false, true)]
    [InlineData(2, null, 3, true, false, true, false)]
    [InlineData(2, 1L, null, true, false, false, false)]
    [InlineData(2, 1L, null, true, false, true, true)]
    [InlineData(null, 1L, 3, true, false, false, false)]
    [InlineData(null, 1L, 3, true, false, true, true)]
    [InlineData(2, null, null, true, false, false, false)]
    [InlineData(2, null, null, true, false, true, true)]
    [InlineData(3, 1L, 2, false, true, true, true)]
    [InlineData(3, 1L, 2, false, true, false, false)]
    [InlineData(2, 1L, 3, false, true, true, false)]
    [InlineData(2, 1L, 3, false, true, false, true)]
    [InlineData(2, null, 3, false, true, false, false)]
    [InlineData(2, null, 3, false, true, true, true)]
    [InlineData(2, 1L, null, false, true, false, true)]
    [InlineData(2, 1L, null, false, true, true, false)]
    [InlineData(null, 1L, 3, false, true, false, false)]
    [InlineData(null, 1L, 3, false, true, true, true)]
    [InlineData(2, null, null, false, true, false, false)]
    [InlineData(2, null, null, false, true, true, true)]
    [InlineData(3, 1L, 2, false, false, true, true)]
    [InlineData(3, 1L, 2, false, false, false, false)]
    [InlineData(2, 1L, 3, false, false, true, false)]
    [InlineData(2, 1L, 3, false, false, false, true)]
    [InlineData(2, null, 3, false, false, false, false)]
    [InlineData(2, null, 3, false, false, true, true)]
    [InlineData(2, 1L, null, false, false, false, false)]
    [InlineData(2, 1L, null, false, false, true, true)]
    [InlineData(null, 1L, 3, false, false, false, false)]
    [InlineData(null, 1L, 3, false, false, true, true)]
    [InlineData(2, null, null, false, false, false, false)]
    [InlineData(2, null, null, false, false, true, true)]
    public void TestWithIncludeNull(int? value, long? min, int? max, bool includeNullMin, bool includeNullMax, bool reverse, bool result)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<EntityRangeEntity<long?, int?>, EntityRangeFilter<int?>>()
                .FilterByRangedEntity(e => e.Min, FilterByRangeType.LessThan, f => f.Value, FilterByRangeType.LessThan, e => e.Max, f => includeNullMin, f => includeNullMax, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new EntityRangeEntity<long?, int?>(min, max);
        var filter = new EntityRangeFilter<int?>(value);
        Assert.Equal(result, expressionMaker.GetFunc<EntityRangeEntity<long?, int?>, EntityRangeFilter<int?>>(filter)(entity));
    }

    private static (TEntityMinProperty, TEntityMaxProperty) Test<TEntityMinProperty, TEntityMaxProperty, TFilterProperty>(List<(TEntityMinProperty, TEntityMaxProperty)> entityValues, TFilterProperty value)
    {
        var filter = new FilterBuilder()
            .Configure<EntityRangeEntity<TEntityMinProperty, TEntityMaxProperty>, EntityRangeFilter<TFilterProperty>>()
                .FilterByRangedEntity(e => e.Min, FilterByRangeType.LessThan, f => f.Value, FilterByRangeType.LessThan, e => e.Max)
                .Finish()
            .Build();
        var list = entityValues.Select(v => new EntityRangeEntity<TEntityMinProperty, TEntityMaxProperty>(v.Item1, v.Item2));
        var entityRangeFilter = new EntityRangeFilter<TFilterProperty>(value);
        var exp = filter.GetExpression<EntityRangeEntity<TEntityMinProperty, TEntityMaxProperty>, EntityRangeFilter<TFilterProperty>>(entityRangeFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return (result[0].Min, result[0].Max);
    }
}
