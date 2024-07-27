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
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value == f.Value)
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
                .Filter(f => e => e.Value == f.Value1)
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
    public void TestNullStructNotEqual1()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => f.Value == e.Value)
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
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => !e.Value.HasValue || e.Value.Value == f.Value)
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
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value.HasValue && e.Value == f.Value)
                .Finish()
            .Build();
        var list = new List<TestStruct2?> { null, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter = new ComparisonFilter<TestStruct2?>(null);
        var exp = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void TestNullEnumNotEqual1()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestEnum?>, ComparisonFilter<TestEnum?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value == f.Value)
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

    [Fact]
    public void TestComparisonEqual()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<byte?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value == f.Value)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(2);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
    }

    [Fact]
    public void TestComparisonGreaterThan()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<byte?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value > f.Value)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(2);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void TestComparisonGreaterThanOrEqual()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<byte?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value >= f.Value)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(2);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void TestComparisonInEquality()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<byte?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value != f.Value)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(2);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void TestComparisonLessThan()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<byte?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value < f.Value)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(2);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Single(result);
    }

    [Fact]
    public void TestComparisonLessThanOrEqual()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<byte?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value <= f.Value)
                .Finish()
            .Build();
        var list = new List<int> { 1, 2, 3, 4, 5 }.Select(v => new ComparisonEntity<int>(v));
        var comparisonFilter = new ComparisonFilter<byte?>(2);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<byte?>>(comparisonFilter);
        var result = list.Where(exp.Compile()).ToList();
        Assert.Equal(2, result.Count);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(2, 1, false)]
    [InlineData(1, null, true)]
    public void TestIntCompareToNullableIntWithoutIncludeNull(int entityValue, int? filterValue, bool result)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int>, ComparisonFilter<int?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => f.Value == e.Value, f => f.Value.HasValue)
                .Finish()
            .Build();
        var entity = new ComparisonEntity<int>(entityValue);
        var comparisonFilter = new ComparisonFilter<int?>(filterValue);
        var exp = filter.GetExpression<ComparisonEntity<int>, ComparisonFilter<int?>>(comparisonFilter);
        Assert.Equal(result, exp.Compile()(entity));
    }

    [Theory]
    [InlineData(1, 1, true, true)]
    [InlineData(1, 1, false, true)]
    [InlineData(null, 1, true, true)]
    [InlineData(null, 1, false, false)]
    [InlineData(2, 1, true, false)]
    [InlineData(2, 1, false, false)]
    public void TestNullableIntCompareToIntWithIncludeNull(int? entityValue, int filterValue, bool includeNull, bool result)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int?>, ComparisonFilter<int>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => includeNull ? !e.Value.HasValue || e.Value.Value == f.Value : e.Value.HasValue && e.Value == f.Value)
                .Finish()
            .Build();
        var entity = new ComparisonEntity<int?>(entityValue);
        var comparisonFilter = new ComparisonFilter<int>(filterValue);
        var exp = filter.GetExpression<ComparisonEntity<int?>, ComparisonFilter<int>>(comparisonFilter);
        Assert.Equal(result, exp.Compile()(entity));
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(null, 1, false)]
    [InlineData(2, 1, false)]
    public void TestNullableIntCompareToIntWithoutIncludeNull(int? entityValue, int filterValue, bool result)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int?>, ComparisonFilter<int>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value == f.Value)
                .Finish()
            .Build();
        var entity = new ComparisonEntity<int?>(entityValue);
        var comparisonFilter = new ComparisonFilter<int>(filterValue);
        var exp = filter.GetExpression<ComparisonEntity<int?>, ComparisonFilter<int>>(comparisonFilter);
        Assert.Equal(result, exp.Compile()(entity));
    }

    [Theory]
    [InlineData(1, 1, true, true)]
    [InlineData(1, 1, false, true)]
    [InlineData(null, 1, true, true)]
    [InlineData(null, 1, false, false)]
    [InlineData(2, 1, true, false)]
    [InlineData(2, 1, false, false)]
    [InlineData(1, null, true, true)]
    [InlineData(1, null, false, true)]
    [InlineData(null, null, true, true)]
    [InlineData(null, null, false, true)]
    public void TestNullableIntCompareToNullableIntWithIncludeNull(int? entityValue, int? filterValue, bool includeNull, bool result)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int?>, ComparisonFilter<int?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => includeNull ? !e.Value.HasValue || e.Value == f.Value : e.Value.HasValue && e.Value == f.Value, f => f.Value.HasValue)
                .Finish()
            .Build();
        var entity = new ComparisonEntity<int?>(entityValue);
        var comparisonFilter = new ComparisonFilter<int?>(filterValue);
        var exp = filter.GetExpression<ComparisonEntity<int?>, ComparisonFilter<int?>>(comparisonFilter);
        Assert.Equal(result, exp.Compile()(entity));
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(null, 1, false)]
    [InlineData(2, 1, false)]
    [InlineData(1, null, false)]
    [InlineData(null, null, true)]
    public void TestNullableIntCompareToNullableIntWithoutIncludeNull(int? entityValue, int? filterValue, bool result)
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<int?>, ComparisonFilter<int?>>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value == f.Value)
                .Finish()
            .Build();
        var entity = new ComparisonEntity<int?>(entityValue);
        var comparisonFilter = new ComparisonFilter<int?>(filterValue);
        var exp = filter.GetExpression<ComparisonEntity<int?>, ComparisonFilter<int?>>(comparisonFilter);
        Assert.Equal(result, exp.Compile()(entity));
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
}