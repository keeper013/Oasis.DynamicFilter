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
    [InlineData(1, FilterByPropertyType.Contains, 1, true, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, false, true)]
    public void TestIntContainsIntWithIncludeNull(int entityValue, FilterByPropertyType type, int filterValue, bool includeNull, bool reverse, bool result)
    {
        TestContainsWithIncludeNull(entityValue, type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, true)]
    public void TestIntContainsIntWithoutIncludeNull(int entityValue, FilterByPropertyType type, int filterValue, bool reverse, bool result)
    {
        TestContainsWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, 1, true, true, false)]
    [InlineData(FilterByPropertyType.Contains, 1, true, false, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, true, true)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, false, false)]
    public void TestIntNullContainsIntWithIncludeNull(FilterByPropertyType type, int filterValue, bool includeNull, bool reverse, bool result)
    {
        TestNullContainsWithIncludeNull<int, int>(type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, true)]
    public void TestIntNullContainsIntWithoutIncludeNull(FilterByPropertyType type, int filterValue, bool reverse, bool result)
    {
        TestNullContainsWithoutIncludeNull<int, int>(type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, true, true, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, true, false, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, false, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, false, true, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, false, false, true)]
    public void TestNullableIntContainsIntWithIncludeNull(int? entityValue, FilterByPropertyType type, int filterValue, bool includeNull, bool reverse, bool result)
    {
        TestContainsWithIncludeNull(entityValue, type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, false, true)]
    public void TestNullableIntContainsIntWithoutIncludeNull(int? entityValue, FilterByPropertyType type, int filterValue, bool reverse, bool result)
    {
        TestContainsWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, 1, true, true, false)]
    [InlineData(FilterByPropertyType.Contains, 1, true, false, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, true, true)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, false, false)]
    public void TestNullableIntNullContainsIntWithIncludeNull(FilterByPropertyType type, int filterValue, bool includeNull, bool reverse, bool result)
    {
        TestNullContainsWithIncludeNull<int?, int>(type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, true)]
    public void TestNullableIntNullContainsIntWithoutIncludeNull(FilterByPropertyType type, int filterValue, bool reverse, bool result)
    {
        TestNullContainsWithoutIncludeNull<int?, int>(type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, true, true, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, true, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, true, true, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, true, false, true)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, false, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, false, true, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, false, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, false, true, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, false, false, true)]
    public void TestIntContainsNullableIntWithIncludeNull(int entityValue, FilterByPropertyType type, int? filterValue, bool includeNull, bool reverse, bool result)
    {
        TestContainsWithIncludeNull(entityValue, type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, true, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, true, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, false, true)]
    public void TestIntContainsNullableIntWithoutIncludeNull(int entityValue, FilterByPropertyType type, int? filterValue, bool reverse, bool result)
    {
        TestContainsWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, 1, true, true, false)]
    [InlineData(FilterByPropertyType.Contains, 1, true, false, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, true, true)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, false, false)]
    [InlineData(FilterByPropertyType.Contains, null, true, true, false)]
    [InlineData(FilterByPropertyType.Contains, null, true, false, true)]
    [InlineData(FilterByPropertyType.Contains, null, false, true, true)]
    [InlineData(FilterByPropertyType.Contains, null, false, false, false)]
    [InlineData(FilterByPropertyType.NotContains, null, true, true, false)]
    [InlineData(FilterByPropertyType.NotContains, null, true, false, true)]
    [InlineData(FilterByPropertyType.NotContains, null, false, true, false)]
    [InlineData(FilterByPropertyType.NotContains, null, false, false, true)]
    public void TestIntNullContainsNullableIntWithIncludeNull(FilterByPropertyType type, int? filterValue, bool includeNull, bool reverse, bool result)
    {
        TestNullContainsWithIncludeNull<int, int?>(type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, true)]
    [InlineData(FilterByPropertyType.Contains, null, true, true)]
    [InlineData(FilterByPropertyType.Contains, null, false, false)]
    [InlineData(FilterByPropertyType.NotContains, null, true, false)]
    [InlineData(FilterByPropertyType.NotContains, null, false, true)]
    public void TestIntNullContainsNullableIntWithoutIncludeNull(FilterByPropertyType type, int? filterValue, bool reverse, bool result)
    {
        TestNullContainsWithoutIncludeNull<int, int?>(type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, true, true, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, true, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, true, true, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, true, false, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, true, true, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, true, false, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(null, FilterByPropertyType.Contains, null, true, true, false)]
    [InlineData(null, FilterByPropertyType.Contains, null, true, false, true)]
    [InlineData(null, FilterByPropertyType.NotContains, null, true, true, true)]
    [InlineData(null, FilterByPropertyType.NotContains, null, true, false, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, false, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, false, true, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, false, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, false, true, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, false, false, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, false, true, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, false, false, true)]
    [InlineData(null, FilterByPropertyType.Contains, null, false, true, false)]
    [InlineData(null, FilterByPropertyType.Contains, null, false, false, true)]
    [InlineData(null, FilterByPropertyType.NotContains, null, false, true, true)]
    [InlineData(null, FilterByPropertyType.NotContains, null, false, false, false)]
    public void TestNullableIntContainsNullableIntWithIncludeNull(int? entityValue, FilterByPropertyType type, int? filterValue, bool includeNull, bool reverse, bool result)
    {
        TestContainsWithIncludeNull(entityValue, type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterByPropertyType.Contains, 1, true, false)]
    [InlineData(1, FilterByPropertyType.Contains, 1, false, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(2, FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, true, true)]
    [InlineData(1, FilterByPropertyType.NotContains, 1, false, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(2, FilterByPropertyType.NotContains, 1, false, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, true, true)]
    [InlineData(1, FilterByPropertyType.Contains, null, false, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, true, false)]
    [InlineData(1, FilterByPropertyType.NotContains, null, false, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(null, FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(null, FilterByPropertyType.NotContains, 1, false, true)]
    [InlineData(null, FilterByPropertyType.Contains, null, true, false)]
    [InlineData(null, FilterByPropertyType.Contains, null, false, true)]
    [InlineData(null, FilterByPropertyType.NotContains, null, true, true)]
    [InlineData(null, FilterByPropertyType.NotContains, null, false, false)]
    public void TestNullableIntContainsNullableIntWithoutIncludeNull(int? entityValue, FilterByPropertyType type, int? filterValue, bool reverse, bool result)
    {
        TestContainsWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, 1, true, true, false)]
    [InlineData(FilterByPropertyType.Contains, 1, true, false, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, true, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, false, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, true, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, false, true)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, true, true)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, false, false)]
    [InlineData(FilterByPropertyType.Contains, null, true, true, false)]
    [InlineData(FilterByPropertyType.Contains, null, true, false, true)]
    [InlineData(FilterByPropertyType.Contains, null, false, true, true)]
    [InlineData(FilterByPropertyType.Contains, null, false, false, false)]
    [InlineData(FilterByPropertyType.NotContains, null, true, true, false)]
    [InlineData(FilterByPropertyType.NotContains, null, true, false, true)]
    [InlineData(FilterByPropertyType.NotContains, null, false, true, true)]
    [InlineData(FilterByPropertyType.NotContains, null, false, false, false)]
    public void TestNullableIntNullContainsNullableIntWithIncludeNull(FilterByPropertyType type, int? filterValue, bool includeNull, bool reverse, bool result)
    {
        TestNullContainsWithIncludeNull<int?, int?>(type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(FilterByPropertyType.Contains, 1, true, true)]
    [InlineData(FilterByPropertyType.Contains, 1, false, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, true, false)]
    [InlineData(FilterByPropertyType.NotContains, 1, false, true)]
    [InlineData(FilterByPropertyType.Contains, null, true, true)]
    [InlineData(FilterByPropertyType.Contains, null, false, false)]
    [InlineData(FilterByPropertyType.NotContains, null, true, false)]
    [InlineData(FilterByPropertyType.NotContains, null, false, true)]
    public void TestNullableIntNullContainsNullableIntWithoutIncludeNull(FilterByPropertyType type, int? filterValue, bool reverse, bool result)
    {
        TestNullContainsWithoutIncludeNull<int?, int?>(type, filterValue, reverse, result);
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

    private void TestContainsWithIncludeNull<TEntity, TFilter>(TEntity entityValue, FilterByPropertyType type, TFilter filterValue, bool includeNull, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<ArrayEntity<TEntity>, ContainFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new ArrayEntity<TEntity>(new TEntity[] { entityValue });
        var filter = new ContainFilter<TFilter>(filterValue);
        Assert.Equal(result, expressionBuilder.GetFunc<ArrayEntity<TEntity>, ContainFilter<TFilter>>(filter)(entity));
    }

    private void TestContainsWithoutIncludeNull<TEntity, TFilter>(TEntity entityValue, FilterByPropertyType type, TFilter filterValue, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<CollectionEntity<TEntity>, ContainFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new CollectionEntity<TEntity>(new List<TEntity> { entityValue });
        var filter = new ContainFilter<TFilter>(filterValue);
        Assert.Equal(result, expressionBuilder.GetFunc<CollectionEntity<TEntity>, ContainFilter<TFilter>>(filter)(entity));
    }

    private void TestNullContainsWithIncludeNull<TEntity, TFilter>(FilterByPropertyType type, TFilter filterValue, bool includeNull, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<ArrayEntity<TEntity>, ContainFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new ArrayEntity<TEntity>(null);
        var filter = new ContainFilter<TFilter>(filterValue);
        Assert.Equal(result, expressionBuilder.GetFunc<ArrayEntity<TEntity>, ContainFilter<TFilter>>(filter)(entity));
    }

    private void TestNullContainsWithoutIncludeNull<TEntity, TFilter>(FilterByPropertyType type, TFilter filterValue, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<ArrayEntity<TEntity>, ContainFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new ArrayEntity<TEntity>(null);
        var filter = new ContainFilter<TFilter>(filterValue);
        Assert.Equal(result, expressionBuilder.GetFunc<ArrayEntity<TEntity>, ContainFilter<TFilter>>(filter)(entity));
    }
}
