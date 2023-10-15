namespace Oasis.DynamicFilter.Test;

using System.Linq;

public sealed class InEntity<T>
{
    public InEntity(T v)
    {
        Value = v;
    }

    public T Value { get; init; }
}

public sealed class InCollectionFilter<T>
{
    public InCollectionFilter(List<T> v)
    {
        Value = v;
    }

    public List<T> Value { get; init; }
}

public sealed class InArrayFilter<T>
{
    public InArrayFilter(T[] v)
    {
        Value = v;
    }

    public T[] Value { get; init; }
}

public sealed class SimpleInTest
{
    [Fact]
    public void TestIntIntContains()
    {
        var result = TestInCollection(new List<int> { 1, 2, 3, 4, 5 }, new List<int> { 1, 6, 7 });
        Assert.Equal(1, result);
    }

    [Fact]
    public void TestNullableIntByteContains()
    {
        var result = TestInArray(new List<byte> { 1, 2, 3, 4, 5 }, new int?[] { 1, 6, 7 });
        Assert.Equal(1, result);
    }

    [Fact]
    public void TestIntNullableByteContains()
    {
        var result = TestInArray(new List<byte?> { 1, 2, 3, 4, 5 }, new int[] { 1, 6, 7 });
        Assert.Equal((byte?)1, result);
    }

    [Fact]
    public void TestIntNullableByteNullContains()
    {
        var filter = new FilterBuilder().Register<InEntity<byte?>, InArrayFilter<int>>().Build();
        var list = new List<byte?> { null, 2, 3, 4, 5 }.Select(v => new InEntity<byte?>(v));
        var inFilter = new InArrayFilter<int>(new int[] { 1, 6, 7 });
        var exp = filter.GetExpression<InEntity<byte?>, InArrayFilter<int>>(inFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Empty(result);
    }

    private TEntityProperty TestInCollection<TEntityProperty, TFilterPropertyItem>(ICollection<TEntityProperty> entityValues, List<TFilterPropertyItem> filterValue)
    {
        var filter = new FilterBuilder().Register<InEntity<TEntityProperty>, InCollectionFilter<TFilterPropertyItem>>().Build();
        var list = entityValues.Select(v => new InEntity<TEntityProperty>(v));
        var inFilter = new InCollectionFilter<TFilterPropertyItem>(filterValue);
        var exp = filter.GetExpression<InEntity<TEntityProperty>, InCollectionFilter<TFilterPropertyItem>>(inFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value;
    }

    private TEntityProperty TestInArray<TEntityProperty, TFilterPropertyItem>(ICollection<TEntityProperty> entityValues, TFilterPropertyItem[] filterValue)
    {
        var filter = new FilterBuilder().Register<InEntity<TEntityProperty>, InArrayFilter<TFilterPropertyItem>>().Build();
        var list = entityValues.Select(v => new InEntity<TEntityProperty>(v));
        var inFilter = new InArrayFilter<TFilterPropertyItem>(filterValue);
        var exp = filter.GetExpression<InEntity<TEntityProperty>, InArrayFilter<TFilterPropertyItem>>(inFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value;
    }
}
