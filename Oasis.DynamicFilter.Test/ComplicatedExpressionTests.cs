namespace Oasis.DynamicFilter.Test;

using Xunit;

public sealed class Outer
{
    public int X { get; set; }

    public Middle Middle { get; set; } = null!;
}

public sealed class Middle
{
    public int Y { get; set; }

    public Inner Inner { get; set; } = null!;
}

public sealed class Inner
{
    public int Z { get; set; }
}

public sealed class ComplicatedExpressionFilter
{
    public int Number { get; set; }
}

public sealed class ComplicatedExpressionTests
{
    [Theory]
    [InlineData(1, 2, false)]
    [InlineData(3, 3, true)]
    public void IndirectPropertyTest(int innerValue, byte filterValue, bool result)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Outer, ComplicatedExpressionFilter>()
                .Filter(f => e => e.Middle.Inner.Z == f.Number)
                .Finish()
        .Build();

        var inner = new Inner { Z = innerValue };
        var middle = new Middle { Inner = inner };
        var outer = new Outer { Middle = middle };
        var filter = new ComplicatedExpressionFilter { Number = filterValue };
        Assert.Equal(result, expressionMaker.GetFunc<Outer, ComplicatedExpressionFilter>(filter)(outer));
    }

    [Theory]
    [InlineData(1, 2, 3, 4, true)]
    [InlineData(2, 3, 4, 6, false)]
    [InlineData(1, 3, 4, 5, true)]
    public void ComplicatedExpressionTest(int x, int y, int z, int f, bool result)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Outer, ComplicatedExpressionFilter>()
                .Filter(f => e => e.Middle.Inner.Z + e.Middle.Y - e.X >= f.Number)
                .Finish()
        .Build();

        var inner = new Inner { Z = z };
        var middle = new Middle { Inner = inner, Y = y };
        var outer = new Outer { Middle = middle, X = x };
        var filter = new ComplicatedExpressionFilter { Number = f };
        Assert.Equal(result, expressionMaker.GetFunc<Outer, ComplicatedExpressionFilter>(filter)(outer));
    }
}
