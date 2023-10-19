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
                .FilterByProperty(e => e.Value, FilterByPropertyType.Equality, f => f.Value, null, null, f => false)
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
                .FilterByProperty(e => e.Value, FilterByPropertyType.Equality, f => f.Value1)
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
                .FilterByProperty(e => e.Value, FilterByPropertyType.Equality, f => f.Value, null, null, f => f.Value == 2)
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
                .FilterByProperty(e => e.Value, FilterByPropertyType.Equality, f => f.Value, null, null, f => false)
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
                .FilterByProperty(e => e.Value, FilterByPropertyType.Equality, f => f.Value, f => true, null, null)
                .Finish()
            .Build();
        var list = new List<TestStruct2?> { null, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter = new ComparisonFilter<TestStruct2>(new TestStruct2 { X = 1 });
        var exp = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
        Assert.Null(result[0].Value);
    }

    [Theory]
    [InlineData(true, true, 1, 3)]
    [InlineData(true, true, 2, 2)]
    [InlineData(true, false, 1, 1)]
    [InlineData(true, false, 2, 2)]
    [InlineData(false, true, 1, 2)]
    [InlineData(false, true, 2, 1)]
    [InlineData(false, false, 1, 0)]
    [InlineData(false, false, 2, 1)]
    public void TestReverseIncludeNull(bool includeNull, bool reverse, int filterValue, int number)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2>>()
                .FilterByProperty(e => e.Value, FilterByPropertyType.Equality, f => f.Value, f => includeNull, f => reverse, null)
                .Finish()
            .Build();
        var list = new List<TestStruct2?> { null, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter = new ComparisonFilter<TestStruct2>(new TestStruct2 { X = filterValue });
        var exp = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(number, result.Count);
    }

    [Fact]
    public void TestNullEnumNotEqual1()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestEnum?>, ComparisonFilter<TestEnum?>>()
                .FilterByProperty(e => e.Value, FilterByPropertyType.Equality, f => f.Value, null, null, f => false)
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
    [InlineData(FilterByPropertyType.Equality, 1)]
    [InlineData(FilterByPropertyType.GreaterThan, 3)]
    [InlineData(FilterByPropertyType.GreaterThanOrEqual, 4)]
    [InlineData(FilterByPropertyType.InEquality, 4)]
    [InlineData(FilterByPropertyType.LessThan, 1)]
    [InlineData(FilterByPropertyType.LessThanOrEqual, 2)]
    public void TestComparison(FilterByPropertyType type, int number)
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
    [InlineData(FilterByPropertyType.Equality, 4)]
    [InlineData(FilterByPropertyType.GreaterThan, 2)]
    [InlineData(FilterByPropertyType.GreaterThanOrEqual, 1)]
    [InlineData(FilterByPropertyType.InEquality, 1)]
    [InlineData(FilterByPropertyType.LessThan, 4)]
    [InlineData(FilterByPropertyType.LessThanOrEqual, 3)]
    public void TestReverseComparison(FilterByPropertyType type, int number)
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