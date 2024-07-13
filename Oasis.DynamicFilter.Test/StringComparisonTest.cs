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
    [InlineData(null, "a", false)]
    [InlineData("a", null, false)]
    [InlineData(null, null, true)]
    [InlineData("A", "A", true)]
    public void TestEqualityWithoutIncludeNull(string? entityValue, string? filterValue, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<StringEntity, StringFilter>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => string.Equals(f.Value, e.Value))
                .Finish()
            .Build();

        var entity = new StringEntity { Value = entityValue };
        var filter = new StringFilter { Value = filterValue };
        Assert.Equal(result, expressionBuilder.GetFunc<StringEntity, StringFilter>(filter)(entity));
    }

    [Theory]
    [InlineData("Abcde", "cd", true)]
    [InlineData("Abcde", "ac", false)]
    public void TestContainsWithoutIncludeNull(string? entityValue, string? filterValue, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<StringEntity, StringFilter>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value!.Contains(f.Value!))
                .Finish()
            .Build();

        var entity = new StringEntity { Value = entityValue };
        var filter = new StringFilter { Value = filterValue };
        Assert.Equal(result, expressionBuilder.GetFunc<StringEntity, StringFilter>(filter)(entity));
    }

    [Theory]
    [InlineData("Abcde", "Ab", true)]
    [InlineData("Abcde", "bc", false)]
    public void TestStartsWithWithoutIncludeNull(string? entityValue, string? filterValue, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<StringEntity, StringFilter>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value!.StartsWith(f.Value!))
                .Finish()
            .Build();

        var entity = new StringEntity { Value = entityValue };
        var filter = new StringFilter { Value = filterValue };
        Assert.Equal(result, expressionBuilder.GetFunc<StringEntity, StringFilter>(filter)(entity));
    }

    [Theory]
    [InlineData("Abcde", "de", true)]
    [InlineData("Abcde", "ed", false)]
    public void TestEndsWithWithoutIncludeNull(string? entityValue, string? filterValue, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<StringEntity, StringFilter>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => e.Value!.EndsWith(f.Value!))
                .Finish()
            .Build();

        var entity = new StringEntity { Value = entityValue };
        var filter = new StringFilter { Value = filterValue };
        Assert.Equal(result, expressionBuilder.GetFunc<StringEntity, StringFilter>(filter)(entity));
    }

    [Theory]
    [InlineData("Cd", "AbCde", true)]
    [InlineData("ac", "AbCde", false)]
    public void TestInWithoutIncludeNull(string? entityValue, string? filterValue, bool result)
    {
        var expressionBuilder = new FilterBuilder()
            .Configure<StringEntity, StringFilter>()
                .ExcludeProperties(e => e.Value)
                .Filter(f => e => f.Value!.Contains(e.Value!))
                .Finish()
            .Build();

        var entity = new StringEntity { Value = entityValue };
        var filter = new StringFilter { Value = filterValue };
        Assert.Equal(result, expressionBuilder.GetFunc<StringEntity, StringFilter>(filter)(entity));
    }
}
