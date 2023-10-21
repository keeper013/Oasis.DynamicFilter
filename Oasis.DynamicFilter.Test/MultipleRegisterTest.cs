namespace Oasis.DynamicFilter.Test;

using Xunit;

public sealed class MultipleRegisterTest
{
    [Fact]
    public void Test()
    {
        var filter = new FilterBuilder()
            .Configure<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value, null, null, f => false)
                .Finish()
            .Configure<ComparisonEntity<TestEnum?>, ComparisonFilter<TestEnum?>>()
                .FilterByProperty(e => e.Value, FilterBy.Equality, f => f.Value, null, null, f => false)
                .Finish()
            .Build();
        var list1 = new List<TestStruct2?> { null, new TestStruct2 { X = 2 }, new TestStruct2 { X = 3 } }.Select(v => new ComparisonEntity<TestStruct2?>(v));
        var comparisonFilter1 = new ComparisonFilter<TestStruct2?>(null);
        var exp1 = filter.GetExpression<ComparisonEntity<TestStruct2?>, ComparisonFilter<TestStruct2?>>(comparisonFilter1);
        var result1 = list1.Where(exp1.Compile()).ToList();
        Assert.Single(result1);
        Assert.Null(result1[0].Value);

        var list2 = new List<TestEnum?> { TestEnum.Value1, TestEnum.Value2, TestEnum.Value3 }.Select(v => new ComparisonEntity<TestEnum?>(v));
        var comparisonFilter2 = new ComparisonFilter<TestEnum?>(null);
        var exp2 = filter.GetExpression<ComparisonEntity<TestEnum?>, ComparisonFilter<TestEnum?>>(comparisonFilter2);
        var result2 = list2.Where(exp2.Compile()).ToList();
        Assert.Empty(result2);
    }
}
