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

    private (TEntityMinProperty, TEntityMaxProperty) Test<TEntityMinProperty, TEntityMaxProperty, TFilterProperty>(List<(TEntityMinProperty, TEntityMaxProperty)> entityValues, TFilterProperty value)
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
