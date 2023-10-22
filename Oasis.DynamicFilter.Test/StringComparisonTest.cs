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
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, "a", true, true)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, "a", false, false)]
    [InlineData("a", FilterStringBy.Equality, StringComparison.Ordinal, null, true, true)]
    [InlineData("a", FilterStringBy.Equality, StringComparison.Ordinal, null, false, false)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, null, true, true)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, null, false, false)]
    [InlineData("A", FilterStringBy.Equality, StringComparison.OrdinalIgnoreCase, "a", true, false)]
    [InlineData("A", FilterStringBy.Equality, StringComparison.OrdinalIgnoreCase, "a", false, true)]
    [InlineData("Abc", FilterStringBy.InEquality, StringComparison.OrdinalIgnoreCase, "acb", false, true)]
    [InlineData("Abc", FilterStringBy.InEquality, StringComparison.OrdinalIgnoreCase, "acb", true, false)]
    [InlineData("Abcde", FilterStringBy.Contains, StringComparison.OrdinalIgnoreCase, "cD", false, true)]
    [InlineData("Abcde", FilterStringBy.NotContains, StringComparison.OrdinalIgnoreCase, "cD", false, false)]
    [InlineData("Abcde", FilterStringBy.Contains, StringComparison.OrdinalIgnoreCase, "ac", false, false)]
    [InlineData("Abcde", FilterStringBy.NotContains, StringComparison.OrdinalIgnoreCase, "ac", false, true)]
    [InlineData("Abcde", FilterStringBy.StartsWith, StringComparison.OrdinalIgnoreCase, "aB", false, true)]
    [InlineData("Abcde", FilterStringBy.NotStartsWith, StringComparison.OrdinalIgnoreCase, "aB", false, false)]
    [InlineData("Abcde", FilterStringBy.StartsWith, StringComparison.OrdinalIgnoreCase, "bc", false, false)]
    [InlineData("Abcde", FilterStringBy.NotStartsWith, StringComparison.OrdinalIgnoreCase, "bc", false, true)]
    [InlineData("Abcde", FilterStringBy.EndsWith, StringComparison.OrdinalIgnoreCase, "DE", false, true)]
    [InlineData("Abcde", FilterStringBy.NotEndsWith, StringComparison.OrdinalIgnoreCase, "DE", false, false)]
    [InlineData("Abcde", FilterStringBy.EndsWith, StringComparison.OrdinalIgnoreCase, "ed", false, false)]
    [InlineData("Abcde", FilterStringBy.NotEndsWith, StringComparison.OrdinalIgnoreCase, "ed", false, true)]
    [InlineData("cD", FilterStringBy.In, StringComparison.OrdinalIgnoreCase, "AbCde", false, true)]
    [InlineData("cD", FilterStringBy.NotIn, StringComparison.OrdinalIgnoreCase, "AbCde", false, false)]
    [InlineData("ac", FilterStringBy.In, StringComparison.OrdinalIgnoreCase, "AbCde", false, false)]
    [InlineData("ac", FilterStringBy.NotIn, StringComparison.OrdinalIgnoreCase, "AbCde", false, true)]
    public void TestWithoutIncludeNull(string? entityValue, FilterStringBy type, StringComparison stringComparison, string? filterValue, bool reverse, bool result)
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
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, "a", true, true, false)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, "a", true, false, true)]
    [InlineData("a", FilterStringBy.Equality, StringComparison.Ordinal, null, true, true, true)]
    [InlineData("a", FilterStringBy.Equality, StringComparison.Ordinal, null, true, false, false)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, null, true, true, false)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, null, true, false, true)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, "a", false, true, true)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, "a", false, false, false)]
    [InlineData("a", FilterStringBy.Equality, StringComparison.Ordinal, null, false, true, true)]
    [InlineData("a", FilterStringBy.Equality, StringComparison.Ordinal, null, false, false, false)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, null, false, true, true)]
    [InlineData(null, FilterStringBy.Equality, StringComparison.Ordinal, null, false, false, false)]
    public void TestWithIncludeNull(string? entityValue, FilterStringBy type, StringComparison stringComparison, string? filterValue, bool includeNull, bool reverse, bool result)
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
