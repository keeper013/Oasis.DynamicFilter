namespace Oasis.DynamicFilter.Test;

public enum TestEnum
{
    Value1,
    Value2,
    Value3,
}

public struct TestStruct1
{
    public int X { get; set; }
}

public struct TestStruct2
{
    public int X { get; set; }

    public static bool operator ==(TestStruct2 a, TestStruct2 b)
    {
        return a.X == b.X;
    }

    public static bool operator !=(TestStruct2 a, TestStruct2 b)
    {
        return a.X != b.X;
    }

    public override bool Equals(object? obj)
    {
        return obj != null && obj is TestStruct2 o1 && o1.X == this.X;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode();
    }
}

public class TestClass1
{
    public int X { get; set; }
}

public class TestClass2
{
    public int X { get; set; }

    public static bool operator ==(TestClass2? a, TestClass2? b)
    {
        return (a is null && b is null) || (a is not null && b is not null && a.X == b.X);
    }

    public static bool operator !=(TestClass2? a, TestClass2? b)
    {
        return (a is not null || b is not null) && (a is null || b is null || a.X != b.X);
    }

    public override bool Equals(object? obj)
    {
        return obj != null && obj is TestClass2 o1 && o1.X == this.X;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode();
    }
}
