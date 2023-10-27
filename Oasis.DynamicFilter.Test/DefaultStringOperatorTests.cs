namespace Oasis.DynamicFilter.Test;

using Xunit;

public sealed class DefaultStringEntity
{
    public string Property { get; set; } = null!;
}

public sealed class DefaultStringFilter
{
    public string Property { get; set; } = null!;
}

public sealed class DefaultStringOperatorTests
{
    [Theory]
    [InlineData(StringOperator.Equality, "abcde", null, "abcde", true)]
    [InlineData(StringOperator.Equality, "abcde", null, "de", false)]
    [InlineData(StringOperator.Contains, "abcde", null, "abcde", true)]
    [InlineData(StringOperator.Contains, "abcde", null, "de", true)]
    [InlineData(StringOperator.NotContains, "abcde", null, "abcde", false)]
    [InlineData(StringOperator.NotContains, "abcde", null, "de", false)]
    [InlineData(StringOperator.Equality, "abcde", StringOperator.NotContains, "abcde", false)]
    [InlineData(StringOperator.Equality, "abcde", StringOperator.Contains, "de", true)]
    public void Test(StringOperator builderDefault, string entityValue, StringOperator? registerDefault, string filterValue, bool result)
    {
        var expressionMaker = new FilterBuilder(builderDefault).Register<DefaultStringEntity, DefaultStringFilter>(registerDefault).Build();
        var entity = new DefaultStringEntity { Property = entityValue };
        var filter = new DefaultStringFilter { Property = filterValue };
        Assert.Equal(result, expressionMaker.GetFunc<DefaultStringEntity, DefaultStringFilter>(filter)(entity));
    }
}
