namespace Oasis.DynamicFilter.Test.BasicTests;

public sealed class ComparisonEntity<T>
{
    public ComparisonEntity(T v)
    {
        Value = v;
    }

    public T Value { get; init; }
}

public sealed class ComparisonFilter<T>
{
    public ComparisonFilter(T v)
    {
        Value = v;
    }

    public T Value { get; init; }
}

public sealed class SimpleConparisonTest
{
    [Fact]
    public void TestIntIntEqual()
    {
        Assert.Equal(2, TestEqual(new List<int> { 1, 2, 3, 4, 5 }, 2));
    }

    [Fact]
    public void TestIntByteEqual()
    {
        Assert.Equal(2, TestEqual<int, byte>(new List<int> { 1, 2, 3, 4, 5 }, 2));
    }

    [Fact]
    public void TestByteLongEqual()
    {
        Assert.Equal(2, TestEqual<byte, long>(new List<byte> { 1, 2, 3, 4, 5 }, 2));
    }

    [Fact]
    public void TestUShortNullableLongEqual()
    {
        Assert.Equal(2, TestEqual<ushort, long?>(new List<ushort> { 1, 2, 3, 4, 5 }, 2));
    }

    [Fact]
    public void TestDefaultIgnore()
    {
        var filter = new FilterBuilder().Register<ComparisonEntity<int>, ComparisonFilter<byte?>>().Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(null);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void TestNullableDecimalLongEqual()
    {
        Assert.Equal((ushort?)2, TestEqual<decimal?, long?>(new List<decimal?> { 1, 2, 3, 4, 5 }, 2));
    }

    private TEntityProperty TestEqual<TEntityProperty, TFilterProperty>(List<TEntityProperty> entityValues, TFilterProperty filterValue)
    {
        var filter = new FilterBuilder().Register<ComparisonEntity<TEntityProperty>, ComparisonFilter<TFilterProperty>>().Build();
        var list = entityValues.Select(v => new ComparisonEntity<TEntityProperty>(v));
        var comparisonFilter = new ComparisonFilter<TFilterProperty>(filterValue);
        var exp = filter.GetExpression<ComparisonEntity<TEntityProperty>, ComparisonFilter<TFilterProperty>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value;
    }
}