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
    [InlineData(null, StringOperator.Equality, "a", true, true)]
    [InlineData(null, StringOperator.Equality, "a", false, false)]
    [InlineData("a", StringOperator.Equality, null, true, true)]
    [InlineData("a", StringOperator.Equality, null, false, false)]
    [InlineData(null, StringOperator.Equality, null, true, true)]
    [InlineData(null, StringOperator.Equality, null, false, false)]
    [InlineData("A", StringOperator.Equality, "A", true, false)]
    [InlineData("A", StringOperator.Equality, "A", false, true)]
    [InlineData("Abc", StringOperator.InEquality, "Acb", false, true)]
    [InlineData("Abc", StringOperator.InEquality, "Acb", true, false)]
    [InlineData("Abcde", StringOperator.Contains, "cd", false, true)]
    [InlineData("Abcde", StringOperator.NotContains, "cd", false, false)]
    [InlineData("Abcde", StringOperator.Contains, "ac", false, false)]
    [InlineData("Abcde", StringOperator.NotContains, "ac", false, true)]
    [InlineData("Abcde", StringOperator.StartsWith, "Ab", false, true)]
    [InlineData("Abcde", StringOperator.NotStartsWith, "Ab", false, false)]
    [InlineData("Abcde", StringOperator.StartsWith, "bc", false, false)]
    [InlineData("Abcde", StringOperator.NotStartsWith, "bc", false, true)]
    [InlineData("Abcde", StringOperator.EndsWith, "de", false, true)]
    [InlineData("Abcde", StringOperator.NotEndsWith, "de", false, false)]
    [InlineData("Abcde", StringOperator.EndsWith, "ed", false, false)]
    [InlineData("Abcde", StringOperator.NotEndsWith, "ed", false, true)]
    [InlineData("Cd", StringOperator.In, "AbCde", false, true)]
    [InlineData("Cd", StringOperator.NotIn, "AbCde", false, false)]
    [InlineData("ac", StringOperator.In, "AbCde", false, false)]
    [InlineData("ac", StringOperator.NotIn, "AbCde", false, true)]
    public void TestWithoutIncludeNull(string? entityValue, StringOperator type, string? filterValue, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<StringEntity, StringFilter>()
                .FilterByStringProperty(e => e.Value, type, f => f.Value, null, f => reverse, f => false)
                .Finish()
            .Build();

        var entity = new StringEntity { Value = entityValue };
        var filter = new StringFilter { Value = filterValue };
        Assert.Equal(result, expressionBuilder.GetFunc<StringEntity, StringFilter>(filter)(entity));
    }

    [Theory]
    [InlineData(null, StringOperator.Equality, "a", true, true, false)]
    [InlineData(null, StringOperator.Equality, "a", true, false, true)]
    [InlineData("a", StringOperator.Equality, null, true, true, true)]
    [InlineData("a", StringOperator.Equality, null, true, false, false)]
    [InlineData(null, StringOperator.Equality, null, true, true, false)]
    [InlineData(null, StringOperator.Equality, null, true, false, true)]
    [InlineData(null, StringOperator.Equality, "a", false, true, true)]
    [InlineData(null, StringOperator.Equality, "a", false, false, false)]
    [InlineData("a", StringOperator.Equality, null, false, true, true)]
    [InlineData("a", StringOperator.Equality, null, false, false, false)]
    [InlineData(null, StringOperator.Equality, null, false, true, true)]
    [InlineData(null, StringOperator.Equality, null, false, false, false)]
    public void TestWithIncludeNull(string? entityValue, StringOperator type, string? filterValue, bool includeNull, bool reverse, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<StringEntity, StringFilter>()
                .FilterByStringProperty(e => e.Value, type, f => f.Value, f => includeNull, f => reverse, f => false)
                .Finish()
            .Build();

        var entity = new StringEntity { Value = entityValue };
        var filter = new StringFilter { Value = filterValue };
        Assert.Equal(result, expressionBuilder.GetFunc<StringEntity, StringFilter>(filter)(entity));
    }
}
