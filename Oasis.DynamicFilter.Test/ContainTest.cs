namespace Oasis.DynamicFilter.Test;

using System.Linq;
using Oasis.DynamicFilter;
using Oasis.DynamicFilter.Test.BasicTests;

public sealed class CollectionEntity<T>
{
    public CollectionEntity(List<T>? v)
    {
        Value = v;
    }

    public IList<T>? Value { get; init; }
}

public sealed class ArrayEntity<T>
{
    public ArrayEntity(T[]? v)
    {
        Value = v;
    }

    public T[]? Value { get; init; }
}

public sealed class ContainFilter<T>
{
    public ContainFilter(T v)
    {
        Value = v;
    }

    public T Value { get; init; }
}

public sealed class ContainTest
{
    [Fact]
    public void TestIntIntContains()
    {
        List<List<int>> ints = new()
        {
            new List<int> { 1, 2 },
            new List<int> { 3, 4 },
            new List<int> { 1, 4 },
            new List<int> { 2, 5 },
            new List<int> { 4, 6 },
        };

        var result = TestCollectionContain(ints, 3)!;
        Assert.Equal(3, result[0]);
        Assert.Equal(4, result[1]);
    }

    [Fact]
    public void TestNullableIntByteContains()
    {
        List<int?[]> ints = new()
        {
            new int?[] { 1, 2 },
            new int?[] { 3, 4 },
            new int?[] { 1, 4 },
            new int?[] { 2, 5 },
            new int?[] { 4, 6 },
        };

        var result = TestArrayContain(ints, (byte)3)!;
        Assert.Equal(3, result[0]);
        Assert.Equal(4, result[1]);
    }

    [Fact]
    public void TestNullableIntNullableByteContains()
    {
        List<int?[]> ints = new()
        {
            new int?[] { 1, 2 },
            new int?[] { 3, 4 },
            new int?[] { 1, 4 },
            new int?[] { 2, 5 },
            new int?[] { 4, 6 },
        };

        var filter = new FilterBuilder()
            .Configure<ArrayEntity<int?>, ContainFilter<byte?>>()
                .FilterByProperty(e => e.Value, FilterByPropertyType.Contains, f => f.Value, null, null, f => false)
                .Finish()
            .Build();
        var arr = ints.Select(v => new ArrayEntity<int?>(v));
        var comparisonFilter = new ContainFilter<byte?>(null);
        var exp = filter.GetExpression<ArrayEntity<int?>, ContainFilter<byte?>>(comparisonFilter);
        var result = arr.Where(exp.Compile()).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void TestContainsDefaultIgnore()
    {
        List<int[]> ints = new()
        {
            new int[] { 1, 2 },
            new int[] { 3, 4 },
            new int[] { 1, 4 },
            new int[] { 2, 5 },
            new int[] { 4, 6 },
        };

        var filter = new FilterBuilder().Register<ArrayEntity<int>, ContainFilter<byte?>>().Build();
        var arr = ints.Select(v => new ArrayEntity<int>(v));
        var comparisonFilter = new ContainFilter<byte?>(null);
        var exp = filter.GetExpression<ArrayEntity<int>, ContainFilter<byte?>>(comparisonFilter);
        var result = arr.Where(exp.Compile()).ToList();
        Assert.Equal(5, result.Count);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, true, 1, 3)]
    [InlineData(FilterByPropertyType.Contains, true, 2, 2)]
    [InlineData(FilterByPropertyType.Contains, true, 3, 2)]
    [InlineData(FilterByPropertyType.Contains, true, 4, 2)]
    [InlineData(FilterByPropertyType.Contains, true, 5, 3)]
    [InlineData(FilterByPropertyType.NotContains, true, 1, 2)]
    [InlineData(FilterByPropertyType.NotContains, true, 2, 3)]
    [InlineData(FilterByPropertyType.NotContains, true, 3, 3)]
    [InlineData(FilterByPropertyType.NotContains, true, 4, 3)]
    [InlineData(FilterByPropertyType.NotContains, true, 5, 2)]
    [InlineData(FilterByPropertyType.Contains, false, 1, 2)]
    [InlineData(FilterByPropertyType.Contains, false, 2, 3)]
    [InlineData(FilterByPropertyType.Contains, false, 3, 3)]
    [InlineData(FilterByPropertyType.Contains, false, 4, 3)]
    [InlineData(FilterByPropertyType.Contains, false, 5, 2)]
    [InlineData(FilterByPropertyType.NotContains, false, 1, 3)]
    [InlineData(FilterByPropertyType.NotContains, false, 2, 2)]
    [InlineData(FilterByPropertyType.NotContains, false, 3, 2)]
    [InlineData(FilterByPropertyType.NotContains, false, 4, 2)]
    [InlineData(FilterByPropertyType.NotContains, false, 5, 3)]
    public void TestReverseContain(FilterByPropertyType type, bool reverse, int filterNumber, int number)
    {
        var filter = new FilterBuilder()
            .Configure<CollectionEntity<int?>, ContainFilter<int>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => reverse)
                .Finish()
            .Build();
        var list = new List<List<int?>>
        {
            new List<int?> { null, 1, 2 },
            new List<int?> { 1, 2, 3 },
            new List<int?> { 2, 3, 4 },
            new List<int?> { 3, 4, 5 },
            new List<int?> { 4, 5, null },
        }.Select(v => new CollectionEntity<int?>(v));
        var containFilter = new ContainFilter<int>(filterNumber);
        var exp = filter.GetExpression<CollectionEntity<int?>, ContainFilter<int>>(containFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(number, result.Count);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, true, true, null, 3)]
    [InlineData(FilterByPropertyType.Contains, true, false, null, 2)]
    [InlineData(FilterByPropertyType.Contains, false, true, null, 1)]
    [InlineData(FilterByPropertyType.Contains, false, false, null, 0)]
    [InlineData(FilterByPropertyType.NotContains, true, true, null, 1)]
    [InlineData(FilterByPropertyType.NotContains, true, false, null, 0)]
    [InlineData(FilterByPropertyType.NotContains, false, true, null, 3)]
    [InlineData(FilterByPropertyType.NotContains, false, false, null, 2)]
    [InlineData(FilterByPropertyType.Contains, true, true, TestEnum.Value1, 2)]
    [InlineData(FilterByPropertyType.Contains, true, false, TestEnum.Value1, 1)]
    [InlineData(FilterByPropertyType.Contains, false, true, TestEnum.Value1, 2)]
    [InlineData(FilterByPropertyType.Contains, false, false, TestEnum.Value1, 1)]
    [InlineData(FilterByPropertyType.NotContains, true, true, TestEnum.Value1, 2)]
    [InlineData(FilterByPropertyType.NotContains, true, false, TestEnum.Value1, 1)]
    [InlineData(FilterByPropertyType.NotContains, false, true, TestEnum.Value1, 2)]
    [InlineData(FilterByPropertyType.NotContains, false, false, TestEnum.Value1, 1)]
    public void TestEnumReverseCollectionContains(FilterByPropertyType type, bool reverse, bool includeNull, TestEnum? value, int number)
    {
        var filter = new FilterBuilder()
            .Configure<CollectionEntity<TestEnum>, ContainFilter<TestEnum?>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse, f => false)
                .Finish()
            .Build();
        var list = new List<List<TestEnum>?>
        {
            null,
            new List<TestEnum> { TestEnum.Value1, TestEnum.Value2 },
            new List<TestEnum> { TestEnum.Value2, TestEnum.Value3 },
        }.Select(v => new CollectionEntity<TestEnum>(v));
        var containFilter = new ContainFilter<TestEnum?>(value);
        var exp = filter.GetExpression<CollectionEntity<TestEnum>, ContainFilter<TestEnum?>>(containFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(number, result.Count);
    }

    [Theory]
    [InlineData(FilterByPropertyType.In, true, false, 2)]
    [InlineData(FilterByPropertyType.In, false, false, 1)]
    [InlineData(FilterByPropertyType.NotIn, true, false, 1)]
    [InlineData(FilterByPropertyType.NotIn, false, false, 2)]
    [InlineData(FilterByPropertyType.In, true, true, 2)]
    [InlineData(FilterByPropertyType.In, false, true, 2)]
    [InlineData(FilterByPropertyType.NotIn, true, true, 2)]
    [InlineData(FilterByPropertyType.NotIn, false, true, 2)]
    public void TestStructReverseArrayContains(FilterByPropertyType type, bool reverse, bool includeNull, int number)
    {
        var filter = new FilterBuilder()
            .Configure<InEntity<TestStruct2?>, InArrayFilter<TestStruct2>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse)
                .Finish()
            .Build();
        var list = new List<TestStruct2?> { null, new TestStruct2 { X = 1 }, new TestStruct2 { X = 2 } }.Select(v => new InEntity<TestStruct2?>(v));
        var inFilter = new InArrayFilter<TestStruct2>(new TestStruct2[] { new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } });
        var exp = filter.GetExpression<InEntity<TestStruct2?>, InArrayFilter<TestStruct2>>(inFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(number, result.Count);
    }

    private IList<TEntityPropertyItem>? TestCollectionContain<TEntityPropertyItem, TFilterProperty>(ICollection<List<TEntityPropertyItem>> entityValues, TFilterProperty filterValue)
    {
        var filter = new FilterBuilder().Register<CollectionEntity<TEntityPropertyItem>, ContainFilter<TFilterProperty>>().Build();
        var list = entityValues.Select(v => new CollectionEntity<TEntityPropertyItem>(v));
        var containFilter = new ContainFilter<TFilterProperty>(filterValue);
        var exp = filter.GetExpression<CollectionEntity<TEntityPropertyItem>, ContainFilter<TFilterProperty>>(containFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value;
    }

    private IList<TEntityPropertyItem> TestArrayContain<TEntityPropertyItem, TFilterProperty>(ICollection<TEntityPropertyItem[]> entityValues, TFilterProperty filterValue)
    {
        var filter = new FilterBuilder().Register<ArrayEntity<TEntityPropertyItem>, ContainFilter<TFilterProperty>>().Build();
        var arr = entityValues.Select(v => new ArrayEntity<TEntityPropertyItem>(v));
        var comparisonFilter = new ContainFilter<TFilterProperty>(filterValue);
        var exp = filter.GetExpression<ArrayEntity<TEntityPropertyItem>, ContainFilter<TFilterProperty>>(comparisonFilter);
        var result = arr.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value!;
    }
}
