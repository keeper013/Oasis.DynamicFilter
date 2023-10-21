namespace Oasis.DynamicFilter.Test;

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

public sealed class ComparisonFilter1<T>
{
    public ComparisonFilter1(T v)
    {
        Value1 = v;
    }

    public T Value1 { get; init; }
}

public sealed class ComparisonTests
{
    [Fact]
    public void TestIntIntEqual()
    {
        Assert.Equal(2, TestEqual(new List<int> { 1, 2, 3, 4, 5 }, 2));
    }

    [Fact]
    public void TestEnumEqual()
    {
        Assert.Equal(TestEnum.Value2, TestEqual(new List<TestEnum?> { null, TestEnum.Value3, TestEnum.Value1, TestEnum.Value2 }, TestEnum.Value2));
    }

    [Fact]
    public void TestStructEqual()
    {
        Assert.Equal(
            new TestStruct2 { X = 1 },
            TestEqual(new List<TestStruct2> { new TestStruct2 { X = 1 }, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }, (TestStruct2?)new TestStruct2 { X = 1 }));
    }

    [Fact]
    public void TestNullStructEqual()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value, null, null, f => false)
                .Finish()
            .Build();
        var list = new List<TestStruct2?> { null, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter = new ComparisonFilter<TestStruct2?>(null);
        var exp = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        Assert.Null(result[0].Value);
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
    public void TestMatchDifferentName()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter1<byte?>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value1)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter1<byte?>(1);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter1<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        Assert.Equal(1, result[0].Value);
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
    public void TestConfiguredIgnore()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<byte?>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value, null, null, f => f.Value == 2)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(2);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void TestNullStructNotEqual1()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value, null, null, f => false)
                .Finish()
            .Build();
        var list = new List<TestStruct2?> { new TestStruct2 { X = 1 }, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter = new ComparisonFilter<TestStruct2?>(null);
        var exp = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void TestNullStructNotEqual2()
    {
        var filter = new FilterBuilder().Register<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2>>().Build();
        var list = new List<TestStruct2?> { null, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter = new ComparisonFilter<TestStruct2>(new TestStruct2 { X = 1 });
        var exp = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void TestIncludeNull()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value, f => true, null, null)
                .Finish()
            .Build();
        var list = new List<TestStruct2?> { null, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter = new ComparisonFilter<TestStruct2>(new TestStruct2 { X = 1 });
        var exp = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        Assert.Null(result[0].Value);
    }

    [Fact]
    public void TestNotIncludeNull()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value, f => false, null, f => false)
                .Finish()
            .Build();
        var list = new List<TestStruct2?> { null, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter = new ComparisonFilter<TestStruct2?>(null);
        var exp = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void TestNotIncludeNullWithoutSpecification()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value, null, null, f => false)
                .Finish()
            .Build();
        var list = new List<TestStruct2?> { null, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter = new ComparisonFilter<TestStruct2?>(null);
        var exp = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        Assert.Null(result[0].Value);
    }

    [Fact]
    public void TestNullEnumNotEqual1()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestEnum?>, ComparisonFilter<TestEnum?>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value, null, null, f => false)
                .Finish()
            .Build();
        var list = new List<TestEnum?> { TestEnum.Value1, TestEnum.Value2, TestEnum.Value3 }.Select(v => new ComparisonEntity<TestEnum?>(v));
        var comparisonFilter = new ComparisonFilter<TestEnum?>(null);
        var exp = filter.GetExpression<ComparisonEntity<TestEnum?>, ComparisonFilter<TestEnum?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void TestNullEnumNotEqual2()
    {
        var filter = new FilterBuilder().Register<ComparisonEntity<TestEnum?>, ComparisonFilter<TestEnum>>().Build();
        var list = new List<TestEnum?> { null, TestEnum.Value2, TestEnum.Value3 }.Select(v => new ComparisonEntity<TestEnum?>(v));
        var comparisonFilter = new ComparisonFilter<TestEnum>(TestEnum.Value1);
        var exp = filter.GetExpression<ComparisonEntity<TestEnum?>, ComparisonFilter<TestEnum>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void TestDefaultIgnoreForNullable()
    {
        var filter = new FilterBuilder().Register<ComparisonEntity<int?>, ComparisonFilter<byte?>>().Build();
        var list = new List<int?> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int?>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(null);
        var exp = filter.GetExpression<ComparisonEntity<int?>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void TestNullableDecimalLongEqual()
    {
        Assert.Equal((ushort?)2, TestEqual<decimal?, long?>(new List<decimal?> { 1, 2, 3, 4, 5 }, 2));
    }

    [Theory]
    [InlineData(FilterBy.Equality, 1)]
    [InlineData(FilterBy.GreaterThan, 3)]
    [InlineData(FilterBy.GreaterThanOrEqual, 4)]
    [InlineData(FilterBy.InEquality, 4)]
    [InlineData(FilterBy.LessThan, 1)]
    [InlineData(FilterBy.LessThanOrEqual, 2)]
    public void TestComparison(FilterBy type, int number)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<byte?>>()
                .FilterByProperty(e => e.Value, type, f => f.Value)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(2);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(number, result.Count);
    }

    [Theory]
    [InlineData(FilterBy.Equality, 4)]
    [InlineData(FilterBy.GreaterThan, 2)]
    [InlineData(FilterBy.GreaterThanOrEqual, 1)]
    [InlineData(FilterBy.InEquality, 1)]
    [InlineData(FilterBy.LessThan, 4)]
    [InlineData(FilterBy.LessThanOrEqual, 3)]
    public void TestReverseComparison(FilterBy type, int number)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<byte?>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => true, null)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(2);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(number, result.Count);
    }

    [Theory]
    [InlineData(1, FilterBy.Equality, 1, true, false)]
    [InlineData(1, FilterBy.Equality, 1, false, true)]
    [InlineData(2, FilterBy.Equality, 1, true, true)]
    [InlineData(2, FilterBy.Equality, 1, false, false)]
    [InlineData(1, FilterBy.InEquality, 1, true, true)]
    [InlineData(1, FilterBy.InEquality, 1, false, false)]
    [InlineData(2, FilterBy.InEquality, 1, true, false)]
    [InlineData(2, FilterBy.InEquality, 1, false, true)]
    public void TestIntCompareToIntWithoutIncludeNull(int entityValue, FilterBy type, int filterValue, bool reverse, bool result)
    {
        TestCompareToWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterBy.Equality, 1, true, false)]
    [InlineData(1, FilterBy.Equality, 1, false, true)]
    [InlineData(2, FilterBy.Equality, 1, true, true)]
    [InlineData(2, FilterBy.Equality, 1, false, false)]
    [InlineData(1, FilterBy.Equality, null, true, true)]
    [InlineData(1, FilterBy.Equality, null, false, false)]
    [InlineData(1, FilterBy.InEquality, 1, true, true)]
    [InlineData(1, FilterBy.InEquality, 1, false, false)]
    [InlineData(2, FilterBy.InEquality, 1, true, false)]
    [InlineData(2, FilterBy.InEquality, 1, false, true)]
    [InlineData(1, FilterBy.InEquality, null, true, false)]
    [InlineData(1, FilterBy.InEquality, null, false, true)]
    public void TestIntCompareToNullableIntWithoutIncludeNull(int entityValue, FilterBy type, int? filterValue, bool reverse, bool result)
    {
        TestCompareToWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterBy.Equality, 1, true, true, false)]
    [InlineData(1, FilterBy.Equality, 1, true, false, true)]
    [InlineData(1, FilterBy.Equality, 1, false, true, false)]
    [InlineData(1, FilterBy.Equality, 1, false, false, true)]
    [InlineData(null, FilterBy.Equality, 1, true, true, false)]
    [InlineData(null, FilterBy.Equality, 1, true, false, true)]
    [InlineData(null, FilterBy.Equality, 1, false, true, true)]
    [InlineData(null, FilterBy.Equality, 1, false, false, false)]
    [InlineData(2, FilterBy.Equality, 1, true, true, true)]
    [InlineData(2, FilterBy.Equality, 1, true, false, false)]
    [InlineData(2, FilterBy.Equality, 1, false, true, true)]
    [InlineData(2, FilterBy.Equality, 1, false, false, false)]
    [InlineData(1, FilterBy.InEquality, 1, true, true, true)]
    [InlineData(1, FilterBy.InEquality, 1, true, false, false)]
    [InlineData(1, FilterBy.InEquality, 1, false, true, true)]
    [InlineData(1, FilterBy.InEquality, 1, false, false, false)]
    [InlineData(null, FilterBy.InEquality, 1, true, true, false)]
    [InlineData(null, FilterBy.InEquality, 1, true, false, true)]
    [InlineData(null, FilterBy.InEquality, 1, false, true, true)]
    [InlineData(null, FilterBy.InEquality, 1, false, false, false)]
    [InlineData(2, FilterBy.InEquality, 1, true, true, false)]
    [InlineData(2, FilterBy.InEquality, 1, true, false, true)]
    [InlineData(2, FilterBy.InEquality, 1, false, true, false)]
    [InlineData(2, FilterBy.InEquality, 1, false, false, true)]
    public void TestNullableIntCompareToIntWithIncludeNull(int? entityValue, FilterBy type, int filterValue, bool includeNull, bool reverse, bool result)
    {
        TestCompareToWithIncludeNull(entityValue, type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterBy.Equality, 1, true, false)]
    [InlineData(1, FilterBy.Equality, 1, false, true)]
    [InlineData(null, FilterBy.Equality, 1, true, true)]
    [InlineData(null, FilterBy.Equality, 1, false, false)]
    [InlineData(2, FilterBy.Equality, 1, true, true)]
    [InlineData(2, FilterBy.Equality, 1, false, false)]
    [InlineData(1, FilterBy.InEquality, 1, true, true)]
    [InlineData(1, FilterBy.InEquality, 1, false, false)]
    [InlineData(null, FilterBy.InEquality, 1, true, false)]
    [InlineData(null, FilterBy.InEquality, 1, false, true)]
    [InlineData(2, FilterBy.InEquality, 1, true, false)]
    [InlineData(2, FilterBy.InEquality, 1, false, true)]
    public void TestNullableIntCompareToIntWithoutIncludeNull(int? entityValue, FilterBy type, int filterValue, bool reverse, bool result)
    {
        TestCompareToWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterBy.Equality, 1, true, true, false)]
    [InlineData(1, FilterBy.Equality, 1, true, false, true)]
    [InlineData(1, FilterBy.Equality, 1, false, true, false)]
    [InlineData(1, FilterBy.Equality, 1, false, false, true)]
    [InlineData(null, FilterBy.Equality, 1, true, true, false)]
    [InlineData(null, FilterBy.Equality, 1, true, false, true)]
    [InlineData(null, FilterBy.Equality, 1, false, true, true)]
    [InlineData(null, FilterBy.Equality, 1, false, false, false)]
    [InlineData(2, FilterBy.Equality, 1, true, true, true)]
    [InlineData(2, FilterBy.Equality, 1, true, false, false)]
    [InlineData(2, FilterBy.Equality, 1, false, true, true)]
    [InlineData(2, FilterBy.Equality, 1, false, false, false)]
    [InlineData(1, FilterBy.Equality, null, true, true, true)]
    [InlineData(1, FilterBy.Equality, null, true, false, false)]
    [InlineData(1, FilterBy.Equality, null, false, true, true)]
    [InlineData(1, FilterBy.Equality, null, false, false, false)]
    [InlineData(null, FilterBy.Equality, null, true, true, false)]
    [InlineData(null, FilterBy.Equality, null, true, false, true)]
    [InlineData(null, FilterBy.Equality, null, false, true, true)]
    [InlineData(null, FilterBy.Equality, null, false, false, false)]
    [InlineData(1, FilterBy.InEquality, 1, true, true, true)]
    [InlineData(1, FilterBy.InEquality, 1, true, false, false)]
    [InlineData(1, FilterBy.InEquality, 1, false, true, true)]
    [InlineData(1, FilterBy.InEquality, 1, false, false, false)]
    [InlineData(null, FilterBy.InEquality, 1, true, true, false)]
    [InlineData(null, FilterBy.InEquality, 1, true, false, true)]
    [InlineData(null, FilterBy.InEquality, 1, false, true, true)]
    [InlineData(null, FilterBy.InEquality, 1, false, false, false)]
    [InlineData(2, FilterBy.InEquality, 1, true, true, false)]
    [InlineData(2, FilterBy.InEquality, 1, true, false, true)]
    [InlineData(2, FilterBy.InEquality, 1, false, true, false)]
    [InlineData(2, FilterBy.InEquality, 1, false, false, true)]
    [InlineData(1, FilterBy.InEquality, null, true, true, false)]
    [InlineData(1, FilterBy.InEquality, null, true, false, true)]
    [InlineData(1, FilterBy.InEquality, null, false, true, false)]
    [InlineData(1, FilterBy.InEquality, null, false, false, true)]
    [InlineData(null, FilterBy.InEquality, null, true, true, false)]
    [InlineData(null, FilterBy.InEquality, null, true, false, true)]
    [InlineData(null, FilterBy.InEquality, null, false, true, true)]
    [InlineData(null, FilterBy.InEquality, null, false, false, false)]
    public void TestNullableIntCompareToNullableIntWithIncludeNull(int? entityValue, FilterBy type, int? filterValue, bool includeNull, bool reverse, bool result)
    {
        TestCompareToWithIncludeNull(entityValue, type, filterValue, includeNull, reverse, result);
    }

    [Theory]
    [InlineData(1, FilterBy.Equality, 1, true, false)]
    [InlineData(1, FilterBy.Equality, 1, false, true)]
    [InlineData(null, FilterBy.Equality, 1, true, true)]
    [InlineData(null, FilterBy.Equality, 1, false, false)]
    [InlineData(2, FilterBy.Equality, 1, true, true)]
    [InlineData(2, FilterBy.Equality, 1, false, false)]
    [InlineData(1, FilterBy.Equality, null, true, true)]
    [InlineData(1, FilterBy.Equality, null, false, false)]
    [InlineData(null, FilterBy.Equality, null, true, false)]
    [InlineData(null, FilterBy.Equality, null, false, true)]
    [InlineData(1, FilterBy.InEquality, 1, true, true)]
    [InlineData(1, FilterBy.InEquality, 1, false, false)]
    [InlineData(null, FilterBy.InEquality, 1, true, false)]
    [InlineData(null, FilterBy.InEquality, 1, false, true)]
    [InlineData(2, FilterBy.InEquality, 1, true, false)]
    [InlineData(2, FilterBy.InEquality, 1, false, true)]
    [InlineData(1, FilterBy.InEquality, null, true, false)]
    [InlineData(1, FilterBy.InEquality, null, false, true)]
    [InlineData(null, FilterBy.InEquality, null, true, true)]
    [InlineData(null, FilterBy.InEquality, null, false, false)]
    public void TestNullableIntCompareToNullableIntWithoutIncludeNull(int? entityValue, FilterBy type, int? filterValue, bool reverse, bool result)
    {
        TestCompareToWithoutIncludeNull(entityValue, type, filterValue, reverse, result);
    }

    private static TEntityProperty TestEqual<TEntityProperty, TFilterProperty>(List<TEntityProperty> entityValues, TFilterProperty filterValue)
    {
        var filter = new FilterBuilder().Register<ComparisonEntity<TEntityProperty>, ComparisonFilter<TFilterProperty>>().Build();
        var list = entityValues.Select(v => new ComparisonEntity<TEntityProperty>(v));
        var comparisonFilter = new ComparisonFilter<TFilterProperty>(filterValue);
        var exp = filter.GetExpression<ComparisonEntity<TEntityProperty>, ComparisonFilter<TFilterProperty>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        return result[0].Value;
    }

    private static void TestCompareToWithIncludeNull<TEntity, TFilter>(TEntity entityValue, FilterBy type, TFilter filterValue, bool includeNull, bool reverse, bool result)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TEntity>, ComparisonFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new ComparisonEntity<TEntity>(entityValue);
        var comparisonFilter = new ComparisonFilter<TFilter>(filterValue);
        var exp = filter.GetExpression<ComparisonEntity<TEntity>, ComparisonFilter<TFilter>>(comparisonFilter);
        Assert.Equal(result, exp.Compile()(entity));
    }

    private static void TestCompareToWithoutIncludeNull<TEntity, TFilter>(TEntity entityValue, FilterBy type, TFilter filterValue, bool reverse, bool result)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TEntity>, ComparisonFilter<TFilter>>()
                .FilterByProperty(e => e.Value, type, f => f.Value, null, f => reverse, f => false)
                .Finish()
            .Build();
        var entity = new ComparisonEntity<TEntity>(entityValue);
        var comparisonFilter = new ComparisonFilter<TFilter>(filterValue);
        var exp = filter.GetExpression<ComparisonEntity<TEntity>, ComparisonFilter<TFilter>>(comparisonFilter);
        Assert.Equal(result, exp.Compile()(entity));
    }
}