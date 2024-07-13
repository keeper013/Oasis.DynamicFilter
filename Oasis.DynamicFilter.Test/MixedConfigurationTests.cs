namespace Oasis.DynamicFilter.Test;

using Oasis.DynamicFilter;

public sealed class MixedEntity
{
    public MixedEntity(int v, int v1, string v2)
    {
        Value = v;
        Value1 = v1;
        Value2 = v2;
    }

    public int Value { get; init; }

    public int Value1 { get; init; }

    public string Value2 { get; init; }
}

public sealed class MixedFilter
{
    public MixedFilter(int v, int vf1, string vf2)
    {
        Value = v;
        FilterValue1 = vf1;
        FilterValue2 = vf2;
    }

    public int Value { get; init; }

    public int FilterValue1 { get; init; }

    public string FilterValue2 { get; init; }
}

public sealed class MixedConfigurationTests
{
    [Fact]
    public void DefaultRegistrationTest()
    {
        var filter = new FilterBuilder()
            .Register<MixedEntity, MixedFilter>()
            .Build();

        var f = new MixedFilter(1, 2, "a");
        var entities = new List<MixedEntity> { new(1, 3, "b"), new(1, 1, "a"), new(1, 1, "c"), new(2, 0, "test") };
        var exp = filter.GetExpression<MixedEntity, MixedFilter>(f);
        var result = entities.Where(exp.Compile());
        Assert.Equal(3, result.Count());
        foreach (var r in result)
        {
            Assert.Equal(1, r.Value);
        }
    }

    [Fact]
    public void RegistrationOneTest()
    {
        var filter = new FilterBuilder()
            .Configure<MixedEntity, MixedFilter>()
                .Filter(f => e => f.FilterValue1 > e.Value1)
                .Finish()
            .Build();

        var f = new MixedFilter(1, 2, "a");
        var entities = new List<MixedEntity> { new(1, 3, "b"), new(1, 1, "a"), new(1, 1, "c"), new(2, 0, "test") };
        var exp = filter.GetExpression<MixedEntity, MixedFilter>(f);
        var result = entities.Where(exp.Compile());
        Assert.Equal(2, result.Count());
        foreach (var r in result)
        {
            Assert.Equal(1, r.Value);
            Assert.Equal(1, r.Value1);
        }
    }

    [Fact]
    public void RegistrationTwoTest()
    {
        var filter = new FilterBuilder()
            .Configure<MixedEntity, MixedFilter>()
                .Filter(f => e => f.FilterValue1 > e.Value1)
                .Filter(f => e => string.Equals(f.FilterValue2, e.Value2))
                .Finish()
            .Build();

        var f = new MixedFilter(1, 2, "a");
        var entities = new List<MixedEntity> { new(1, 3, "b"), new(1, 1, "a"), new(1, 1, "c"), new(2, 0, "test") };
        var exp = filter.GetExpression<MixedEntity, MixedFilter>(f);
        var result = entities.Where(exp.Compile()).ToList();
        Assert.Single(result);
        Assert.Equal(1, result[0].Value);
        Assert.Equal(1, result[0].Value1);
        Assert.Equal("a", result[0].Value2);
    }
}
