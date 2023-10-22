namespace Oasis.DynamicFilter.Test;

public sealed class StringEntity
{
    public string? Value { get; set; }
}

public sealed class StringFilter
{
    public string? Value { get; set; }
}

public sealed class StringComparisonTest
{
    [Theory]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, "a", true, true)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, "a", false, false)]
    [InlineData("a", StringOperator.Equality, StringComparison.Ordinal, null, true, true)]
    [InlineData("a", StringOperator.Equality, StringComparison.Ordinal, null, false, false)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, null, true, true)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, null, false, false)]
    [InlineData("A", StringOperator.Equality, StringComparison.OrdinalIgnoreCase, "a", true, false)]
    [InlineData("A", StringOperator.Equality, StringComparison.OrdinalIgnoreCase, "a", false, true)]
    [InlineData("Abc", StringOperator.InEquality, StringComparison.OrdinalIgnoreCase, "acb", false, true)]
    [InlineData("Abc", StringOperator.InEquality, StringComparison.OrdinalIgnoreCase, "acb", true, false)]
    [InlineData("Abcde", StringOperator.Contains, StringComparison.OrdinalIgnoreCase, "cD", false, true)]
    [InlineData("Abcde", StringOperator.NotContains, StringComparison.OrdinalIgnoreCase, "cD", false, false)]
    [InlineData("Abcde", StringOperator.Contains, StringComparison.OrdinalIgnoreCase, "ac", false, false)]
    [InlineData("Abcde", StringOperator.NotContains, StringComparison.OrdinalIgnoreCase, "ac", false, true)]
    [InlineData("Abcde", StringOperator.StartsWith, StringComparison.OrdinalIgnoreCase, "aB", false, true)]
    [InlineData("Abcde", StringOperator.NotStartsWith, StringComparison.OrdinalIgnoreCase, "aB", false, false)]
    [InlineData("Abcde", StringOperator.StartsWith, StringComparison.OrdinalIgnoreCase, "bc", false, false)]
    [InlineData("Abcde", StringOperator.NotStartsWith, StringComparison.OrdinalIgnoreCase, "bc", false, true)]
    [InlineData("Abcde", StringOperator.EndsWith, StringComparison.OrdinalIgnoreCase, "DE", false, true)]
    [InlineData("Abcde", StringOperator.NotEndsWith, StringComparison.OrdinalIgnoreCase, "DE", false, false)]
    [InlineData("Abcde", StringOperator.EndsWith, StringComparison.OrdinalIgnoreCase, "ed", false, false)]
    [InlineData("Abcde", StringOperator.NotEndsWith, StringComparison.OrdinalIgnoreCase, "ed", false, true)]
    [InlineData("cD", StringOperator.In, StringComparison.OrdinalIgnoreCase, "AbCde", false, true)]
    [InlineData("cD", StringOperator.NotIn, StringComparison.OrdinalIgnoreCase, "AbCde", false, false)]
    [InlineData("ac", StringOperator.In, StringComparison.OrdinalIgnoreCase, "AbCde", false, false)]
    [InlineData("ac", StringOperator.NotIn, StringComparison.OrdinalIgnoreCase, "AbCde", false, true)]
    public void TestWithoutIncludeNull(string? entityValue, StringOperator type, StringComparison stringComparison, string? filterValue, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<StringEntity, StringFilter>()
                .FilterByStringProperty(e => e.Value, type, stringComparison, f => f.Value, null, f => reverse, f => false)
                .Finish()
            .Build();

        var entity = new StringEntity { Value = entityValue };
        var filter = new StringFilter { Value = filterValue };
        Assert.Equal(result, expressionBuilder.GetFunc<StringEntity, StringFilter>(filter)(entity));
    }

    [Theory]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, "a", true, true, false)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, "a", true, false, true)]
    [InlineData("a", StringOperator.Equality, StringComparison.Ordinal, null, true, true, true)]
    [InlineData("a", StringOperator.Equality, StringComparison.Ordinal, null, true, false, false)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, null, true, true, false)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, null, true, false, true)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, "a", false, true, true)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, "a", false, false, false)]
    [InlineData("a", StringOperator.Equality, StringComparison.Ordinal, null, false, true, true)]
    [InlineData("a", StringOperator.Equality, StringComparison.Ordinal, null, false, false, false)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, null, false, true, true)]
    [InlineData(null, StringOperator.Equality, StringComparison.Ordinal, null, false, false, false)]
    public void TestWithIncludeNull(string? entityValue, StringOperator type, StringComparison stringComparison, string? filterValue, bool includeNull, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<StringEntity, StringFilter>()
                .FilterByStringProperty(e => e.Value, type, stringComparison, f => f.Value, f => includeNull, f => reverse, f => false)
                .Finish()
            .Build();

        var entity = new StringEntity { Value = entityValue };
        var filter = new StringFilter { Value = filterValue };
        Assert.Equal(result, expressionBuilder.GetFunc<StringEntity, StringFilter>(filter)(entity));
    }
}
