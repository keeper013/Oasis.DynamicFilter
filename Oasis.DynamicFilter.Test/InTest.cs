namespace Oasis.DynamicFilter.Test.BasicTests;

using System.Linq;
using Oasis.DynamicFilter;

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
    public InCollectionFilter(List<T>? v)
    {
        Value = v;
    }

    public List<T>? Value { get; init; }
}

public sealed class InArrayFilter<T>
{
    public InArrayFilter(T[] v)
    {
        Value = v;
    }

    public T[] Value { get; init; }
}

public sealed class InTest
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

    [Theory]
    [InlineData(FilterByPropertyType.In, true, 2)]
    [InlineData(FilterByPropertyType.In, false, 3)]
    [InlineData(FilterByPropertyType.NotIn, true, 3)]
    [InlineData(FilterByPropertyType.NotIn, false, 2)]
    public void TestReverseIn(FilterByPropertyType type, bool reverse, int number)
    {
        var filter = new FilterBuilder()
            .Configure<InEntity<int>, InCollectionFilter<int?>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => reverse)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new InEntity<int>(v));
        var inFilter = new InCollectionFilter<int?>(new List<int?> { 1, 2, 4, 7, null });
        var exp = filter.GetExpression<InEntity<int>, InCollectionFilter<int?>>(inFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(number, result.Count);
    }

    [Theory]
    [InlineData(FilterByPropertyType.In, true, true, null, true)]
    [InlineData(FilterByPropertyType.In, true, false, null, false)]
    [InlineData(FilterByPropertyType.In, false, true, null, true)]
    [InlineData(FilterByPropertyType.In, false, false, null, false)]
    [InlineData(FilterByPropertyType.In, true, true, 1, false)]
    [InlineData(FilterByPropertyType.In, true, false, 1, false)]
    [InlineData(FilterByPropertyType.In, false, true, 1, true)]
    [InlineData(FilterByPropertyType.In, false, false, 1, true)]
    public void NullInTestWithIncludeNull(FilterByPropertyType type, bool reverse, bool includeNull, int? entityValue, bool isIn)
    {
        var filter = new FilterBuilder()
            .Configure<InEntity<int?>, InCollectionFilter<int?>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse)
                .Finish()
            .Build();
        var inFilter = new InCollectionFilter<int?>(new List<int?> { 1, 2, 4, 7, null });
        var exp = filter.GetExpression<InEntity<int?>, InCollectionFilter<int?>>(inFilter);
        Assert.Equal(isIn, exp.Compile()(new InEntity<int?>(entityValue)));
    }

    [Theory]
    [InlineData(FilterByPropertyType.In, true, null, false)]
    [InlineData(FilterByPropertyType.In, false, null, true)]
    [InlineData(FilterByPropertyType.In, true, 1, false)]
    [InlineData(FilterByPropertyType.In, false, 1, true)]
    public void NullInTestWithoutIncludeNull(FilterByPropertyType type, bool reverse, int? entityValue, bool isIn)
    {
        var filter = new FilterBuilder()
            .Configure<InEntity<int?>, InCollectionFilter<int?>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => reverse)
                .Finish()
            .Build();
        var inFilter = new InCollectionFilter<int?>(new List<int?> { 1, 2, 4, 7, null });
        var exp = filter.GetExpression<InEntity<int?>, InCollectionFilter<int?>>(inFilter);
        Assert.Equal(isIn, exp.Compile()(new InEntity<int?>(entityValue)));
    }

    [Theory]
    [InlineData(FilterByPropertyType.In, true, true, null, true)]
    [InlineData(FilterByPropertyType.In, true, false, null, true)]
    [InlineData(FilterByPropertyType.In, false, true, null, true)]
    [InlineData(FilterByPropertyType.In, false, false, null, false)]
    [InlineData(FilterByPropertyType.In, true, true, 1, true)]
    [InlineData(FilterByPropertyType.In, true, false, 1, true)]
    [InlineData(FilterByPropertyType.In, false, true, 1, false)]
    [InlineData(FilterByPropertyType.In, false, false, 1, false)]
    public void InNullTestWithIncludeNull(FilterByPropertyType type, bool reverse, bool includeNull, int? entityValue, bool isIn)
    {
        var filter = new FilterBuilder()
            .Configure<InEntity<int?>, InCollectionFilter<int?>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse)
                .Finish()
            .Build();
        var inFilter = new InCollectionFilter<int?>(null);
        var exp = filter.GetExpression<InEntity<int?>, InCollectionFilter<int?>>(inFilter);
        Assert.Equal(isIn, exp.Compile()(new InEntity<int?>(entityValue)));
    }

    [Theory]
    [InlineData(FilterByPropertyType.In, true, null, true)]
    [InlineData(FilterByPropertyType.In, false, null, false)]
    [InlineData(FilterByPropertyType.In, true, 1, true)]
    [InlineData(FilterByPropertyType.In, false, 1, false)]
    public void InNullTestWithoutIncludeNull(FilterByPropertyType type, bool reverse, int? entityValue, bool isIn)
    {
        var filter = new FilterBuilder()
            .Configure<InEntity<int?>, InCollectionFilter<int?>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => reverse)
                .Finish()
            .Build();
        var inFilter = new InCollectionFilter<int?>(null);
        var exp = filter.GetExpression<InEntity<int?>, InCollectionFilter<int?>>(inFilter);
        Assert.Equal(isIn, exp.Compile()(new InEntity<int?>(entityValue)));
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
    public void TestEnumReverseInCollection(FilterByPropertyType type, bool reverse, bool includeNull, int number)
    {
        var filter = new FilterBuilder()
            .Configure<InEntity<TestEnum?>, InCollectionFilter<TestEnum>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse)
                .Finish()
            .Build();
        var list = new List<TestEnum?> { null, TestEnum.Value1, TestEnum.Value3 }.Select(v => new InEntity<TestEnum?>(v));
        var inFilter = new InCollectionFilter<TestEnum>(new List<TestEnum> { TestEnum.Value2, TestEnum.Value3 });
        var exp = filter.GetExpression<InEntity<TestEnum?>, InCollectionFilter<TestEnum>>(inFilter);
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
    public void TestStructReverseInArray(FilterByPropertyType type, bool reverse, bool includeNull, int number)
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
