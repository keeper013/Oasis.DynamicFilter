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
    public InArrayFilter(T[]? v)
    {
        Value = v;
    }

    public T[]? Value { get; init; }
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
    [InlineData(1, Operator.In, 1, true, false)]
    [InlineData(1, Operator.In, 1, false, true)]
    [InlineData(2, Operator.In, 1, true, true)]
    [InlineData(2, Operator.In, 1, false, false)]
    [InlineData(1, Operator.NotIn, 1, true, true)]
    [InlineData(1, Operator.NotIn, 1, false, false)]
    [InlineData(2, Operator.NotIn, 1, true, false)]
    [InlineData(2, Operator.NotIn, 1, false, true)]
    public void TestIntInIntWithoutIncludeNull(int entityValue, Operator type, int filterValue, bool reverse, bool result)
    {
        TestInWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, true, true)]
    [InlineData(1, Operator.In, false, false)]
    [InlineData(1, Operator.NotIn, true, false)]
    [InlineData(1, Operator.NotIn, false, true)]
    public void TestIntInIntNullWithoutIncludeNull(int entityValue, Operator type, bool reverse, bool result)
    {
        TestInNullWithoutIncludeNull<int, int>(entityValue, type, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, 1, true, false)]
    [InlineData(1, Operator.In, 1, false, true)]
    [InlineData(2, Operator.In, 1, true, true)]
    [InlineData(2, Operator.In, 1, false, false)]
    [InlineData(1, Operator.In, null, true, true)]
    [InlineData(1, Operator.In, null, false, false)]
    [InlineData(1, Operator.NotIn, 1, true, true)]
    [InlineData(1, Operator.NotIn, 1, false, false)]
    [InlineData(2, Operator.NotIn, 1, true, false)]
    [InlineData(2, Operator.NotIn, 1, false, true)]
    [InlineData(1, Operator.NotIn, null, true, false)]
    [InlineData(1, Operator.NotIn, null, false, true)]

    public void TestIntInNullableIntWithoutIncludeNull(int entityValue, Operator type, int? filterValue, bool reverse, bool result)
    {
        TestInWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, true, true)]
    [InlineData(1, Operator.In, false, false)]
    [InlineData(1, Operator.NotIn, true, false)]
    [InlineData(1, Operator.NotIn, false, true)]
    public void TestIntInNullableIntNullWithoutIncludeNull(int entityValue, Operator type, bool reverse, bool result)
    {
        TestInNullWithoutIncludeNull<int, int?>(entityValue, type, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, 1, true, false)]
    [InlineData(1, Operator.In, 1, false, true)]
    [InlineData(null, Operator.In, 1, true, true)]
    [InlineData(null, Operator.In, 1, false, false)]
    [InlineData(2, Operator.In, 1, true, true)]
    [InlineData(2, Operator.In, 1, false, false)]
    [InlineData(1, Operator.NotIn, 1, true, true)]
    [InlineData(1, Operator.NotIn, 1, false, false)]
    [InlineData(2, Operator.NotIn, 1, true, false)]
    [InlineData(2, Operator.NotIn, 1, false, true)]
    [InlineData(null, Operator.NotIn, 1, true, false)]
    [InlineData(null, Operator.NotIn, 1, false, true)]
    public void TestNullableIntInIntWithoutIncludeNull(int? entityValue, Operator type, int filterValue, bool reverse, bool result)
    {
        TestInWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, 1, false, true, false)]
    [InlineData(1, Operator.In, 1, false, false, true)]
    [InlineData(null, Operator.In, 1, false, true, true)]
    [InlineData(null, Operator.In, 1, false, false, false)]
    [InlineData(2, Operator.In, 1, false, true, true)]
    [InlineData(2, Operator.In, 1, false, false, false)]
    [InlineData(1, Operator.NotIn, 1, false, true, true)]
    [InlineData(1, Operator.NotIn, 1, false, false, false)]
    [InlineData(2, Operator.NotIn, 1, false, true, false)]
    [InlineData(2, Operator.NotIn, 1, false, false, true)]
    [InlineData(null, Operator.NotIn, 1, false, true, true)]
    [InlineData(null, Operator.NotIn, 1, false, false, false)]
    [InlineData(1, Operator.In, 1, true, true, false)]
    [InlineData(1, Operator.In, 1, true, false, true)]
    [InlineData(null, Operator.In, 1, true, true, false)]
    [InlineData(null, Operator.In, 1, true, false, true)]
    [InlineData(2, Operator.In, 1, true, true, true)]
    [InlineData(2, Operator.In, 1, true, false, false)]
    [InlineData(1, Operator.NotIn, 1, true, true, true)]
    [InlineData(1, Operator.NotIn, 1, true, false, false)]
    [InlineData(2, Operator.NotIn, 1, true, true, false)]
    [InlineData(2, Operator.NotIn, 1, true, false, true)]
    [InlineData(null, Operator.NotIn, 1, true, true, false)]
    [InlineData(null, Operator.NotIn, 1, true, false, true)]
    public void TestNullableIntInIntWithInclueNull(int? entityValue, Operator type, int filterValue, bool includeNull, bool reverse, bool result)
    {
        TestInWithIncludeNull(entityValue, type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, true, true)]
    [InlineData(1, Operator.In, false, false)]
    [InlineData(1, Operator.NotIn, true, false)]
    [InlineData(1, Operator.NotIn, false, true)]
    [InlineData(null, Operator.In, true, true)]
    [InlineData(null, Operator.In, false, false)]
    [InlineData(null, Operator.NotIn, true, false)]
    [InlineData(null, Operator.NotIn, false, true)]
    public void TestNullableIntInIntNullWithoutIncludeNull(int? entityValue, Operator type, bool reverse, bool result)
    {
        TestInNullWithoutIncludeNull<int?, int>(entityValue, type, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, false, true, true)]
    [InlineData(1, Operator.In, false, false, false)]
    [InlineData(1, Operator.NotIn, false, true, false)]
    [InlineData(1, Operator.NotIn, false, false, true)]
    [InlineData(null, Operator.In, false, true, true)]
    [InlineData(null, Operator.In, false, false, false)]
    [InlineData(null, Operator.NotIn, false, true, true)]
    [InlineData(null, Operator.NotIn, false, false, false)]
    [InlineData(1, Operator.In, true, true, true)]
    [InlineData(1, Operator.In, true, false, false)]
    [InlineData(1, Operator.NotIn, true, true, false)]
    [InlineData(1, Operator.NotIn, true, false, true)]
    [InlineData(null, Operator.In, true, true, false)]
    [InlineData(null, Operator.In, true, false, true)]
    [InlineData(null, Operator.NotIn, true, true, false)]
    [InlineData(null, Operator.NotIn, true, false, true)]
    public void TestNullableIntInIntNullWithIncludeNull(int? entityValue, Operator type, bool includeNull, bool reverse, bool result)
    {
        TestInNullWithIncludeNull<int?, int>(entityValue, type, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, 1, true, false)]
    [InlineData(1, Operator.In, 1, false, true)]
    [InlineData(null, Operator.In, 1, true, true)]
    [InlineData(null, Operator.In, 1, false, false)]
    [InlineData(2, Operator.In, 1, true, true)]
    [InlineData(2, Operator.In, 1, false, false)]
    [InlineData(1, Operator.NotIn, 1, true, true)]
    [InlineData(1, Operator.NotIn, 1, false, false)]
    [InlineData(2, Operator.NotIn, 1, true, false)]
    [InlineData(2, Operator.NotIn, 1, false, true)]
    [InlineData(null, Operator.NotIn, 1, true, false)]
    [InlineData(null, Operator.NotIn, 1, false, true)]
    [InlineData(1, Operator.In, null, true, true)]
    [InlineData(1, Operator.In, null, false, false)]
    [InlineData(null, Operator.In, null, true, false)]
    [InlineData(null, Operator.In, null, false, true)]
    [InlineData(1, Operator.NotIn, null, true, false)]
    [InlineData(1, Operator.NotIn, null, false, true)]
    [InlineData(null, Operator.NotIn, null, true, true)]
    [InlineData(null, Operator.NotIn, null, false, false)]
    public void TestNullableIntInNullableIntWithoutIncludeNull(int? entityValue, Operator type, int? filterValue, bool reverse, bool result)
    {
        TestInWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, 1, false, true, false)]
    [InlineData(1, Operator.In, 1, false, false, true)]
    [InlineData(null, Operator.In, 1, false, true, true)]
    [InlineData(null, Operator.In, 1, false, false, false)]
    [InlineData(2, Operator.In, 1, false, true, true)]
    [InlineData(2, Operator.In, 1, false, false, false)]
    [InlineData(1, Operator.NotIn, 1, false, true, true)]
    [InlineData(1, Operator.NotIn, 1, false, false, false)]
    [InlineData(2, Operator.NotIn, 1, false, true, false)]
    [InlineData(2, Operator.NotIn, 1, false, false, true)]
    [InlineData(null, Operator.NotIn, 1, false, true, true)]
    [InlineData(null, Operator.NotIn, 1, false, false, false)]
    [InlineData(1, Operator.In, null, false, true, true)]
    [InlineData(1, Operator.In, null, false, false, false)]
    [InlineData(null, Operator.In, null, false, true, true)]
    [InlineData(null, Operator.In, null, false, false, false)]
    [InlineData(1, Operator.NotIn, null, false, true, false)]
    [InlineData(1, Operator.NotIn, null, false, false, true)]
    [InlineData(null, Operator.NotIn, null, false, true, true)]
    [InlineData(null, Operator.NotIn, null, false, false, false)]
    [InlineData(1, Operator.In, 1, true, true, false)]
    [InlineData(1, Operator.In, 1, true, false, true)]
    [InlineData(null, Operator.In, 1, true, true, false)]
    [InlineData(null, Operator.In, 1, true, false, true)]
    [InlineData(2, Operator.In, 1, true, true, true)]
    [InlineData(2, Operator.In, 1, true, false, false)]
    [InlineData(1, Operator.NotIn, 1, true, true, true)]
    [InlineData(1, Operator.NotIn, 1, true, false, false)]
    [InlineData(2, Operator.NotIn, 1, true, true, false)]
    [InlineData(2, Operator.NotIn, 1, true, false, true)]
    [InlineData(null, Operator.NotIn, 1, true, true, false)]
    [InlineData(null, Operator.NotIn, 1, true, false, true)]
    [InlineData(1, Operator.In, null, true, true, true)]
    [InlineData(1, Operator.In, null, true, false, false)]
    [InlineData(null, Operator.In, null, true, true, false)]
    [InlineData(null, Operator.In, null, true, false, true)]
    [InlineData(1, Operator.NotIn, null, true, true, false)]
    [InlineData(1, Operator.NotIn, null, true, false, true)]
    [InlineData(null, Operator.NotIn, null, true, true, false)]
    [InlineData(null, Operator.NotIn, null, true, false, true)]
    public void TestNullableIntInNullableIntWithIncludeNull(int? entityValue, Operator type, int? filterValue, bool includeNull, bool reverse, bool result)
    {
        TestInWithIncludeNull(entityValue, type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, true, true)]
    [InlineData(1, Operator.In, false, false)]
    [InlineData(1, Operator.NotIn, true, false)]
    [InlineData(1, Operator.NotIn, false, true)]
    [InlineData(null, Operator.In, true, true)]
    [InlineData(null, Operator.In, false, false)]
    [InlineData(null, Operator.NotIn, true, false)]
    [InlineData(null, Operator.NotIn, false, true)]
    public void TestNullableIntInNullableIntNullWithoutIncludeNull(int? entityValue, Operator type, bool reverse, bool result)
    {
        TestInNullWithoutIncludeNull<int?, int?>(entityValue, type, reverse, result);
    }

    [Theory]
    [InlineData(1, Operator.In, false, true, true)]
    [InlineData(1, Operator.In, false, false, false)]
    [InlineData(1, Operator.NotIn, false, true, false)]
    [InlineData(1, Operator.NotIn, false, false, true)]
    [InlineData(null, Operator.In, false, true, true)]
    [InlineData(null, Operator.In, false, false, false)]
    [InlineData(null, Operator.NotIn, false, true, true)]
    [InlineData(null, Operator.NotIn, false, false, false)]
    [InlineData(1, Operator.In, true, true, true)]
    [InlineData(1, Operator.In, true, false, false)]
    [InlineData(1, Operator.NotIn, true, true, false)]
    [InlineData(1, Operator.NotIn, true, false, true)]
    [InlineData(null, Operator.In, true, true, false)]
    [InlineData(null, Operator.In, true, false, true)]
    [InlineData(null, Operator.NotIn, true, true, false)]
    [InlineData(null, Operator.NotIn, true, false, true)]
    public void TestNullableIntInNullableIntNullWithIncludeNull(int? entityValue, Operator type, bool includeNull, bool reverse, bool result)
    {
        TestInNullWithIncludeNull<int?, int?>(entityValue, type, includeNull, reverse, result);
    }

    private static TEntityProperty TestInCollection<TEntityProperty, TFilterPropertyItem>(ICollection<TEntityProperty> entityValues, List<TFilterPropertyItem> filterValue)
    {
        var filter = new FilterBuilder().Register<InEntity<TEntityProperty>, InCollectionFilter<TFilterPropertyItem>>().Build();
        var list = entityValues.Select(v => new InEntity<TEntityProperty>(v));
        var inFilter = new InCollectionFilter<TFilterPropertyItem>(filterValue);
        var exp = filter.GetExpression<InEntity<TEntityProperty>, InCollectionFilter<TFilterPropertyItem>>(inFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value;
    }

    private static TEntityProperty TestInArray<TEntityProperty, TFilterPropertyItem>(ICollection<TEntityProperty> entityValues, TFilterPropertyItem[] filterValue)
    {
        var filter = new FilterBuilder().Register<InEntity<TEntityProperty>, InArrayFilter<TFilterPropertyItem>>().Build();
        var list = entityValues.Select(v => new InEntity<TEntityProperty>(v));
        var inFilter = new InArrayFilter<TFilterPropertyItem>(filterValue);
        var exp = filter.GetExpression<InEntity<TEntityProperty>, InArrayFilter<TFilterPropertyItem>>(inFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value;
    }

    private static void TestInWithIncludeNull<TEntity, TFilter>(TEntity entityValue, Operator type, TFilter filterValue, bool includeNull, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<InEntity<TEntity>, InArrayFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new InEntity<TEntity>(entityValue);
        var filter = new InArrayFilter<TFilter>(new TFilter[] { filterValue });
        Assert.Equal(result, expressionBuilder.GetFunc<InEntity<TEntity>, InArrayFilter<TFilter>>(filter)(entity));
    }

    private static void TestInWithoutIncludeNull<TEntity, TFilter>(TEntity entityValue, Operator type, TFilter filterValue, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<InEntity<TEntity>, InCollectionFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new InEntity<TEntity>(entityValue);
        var filter = new InCollectionFilter<TFilter>(new List<TFilter> { filterValue });
        Assert.Equal(result, expressionBuilder.GetFunc<InEntity<TEntity>, InCollectionFilter<TFilter>>(filter)(entity));
    }

    private static void TestInNullWithIncludeNull<TEntity, TFilter>(TEntity entityValue, Operator type, bool includeNull, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<InEntity<TEntity>, InArrayFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new InEntity<TEntity>(entityValue);
        var filter = new InArrayFilter<TFilter>(null);
        Assert.Equal(result, expressionBuilder.GetFunc<InEntity<TEntity>, InArrayFilter<TFilter>>(filter)(entity));
    }

    private static void TestInNullWithoutIncludeNull<TEntity, TFilter>(TEntity entityValue, Operator type, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<InEntity<TEntity>, InCollectionFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new InEntity<TEntity>(entityValue);
        var filter = new InCollectionFilter<TFilter>(null);
        Assert.Equal(result, expressionBuilder.GetFunc<InEntity<TEntity>, InCollectionFilter<TFilter>>(filter)(entity));
    }
}
