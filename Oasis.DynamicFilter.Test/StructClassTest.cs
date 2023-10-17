namespace Oasis.DynamicFilter.Test;

public class StructTestEntity1
{
    public TestStruct1 Property { get; set; }
}

public class StructTestFilter1
{
    public TestStruct1? Property { get; set; }
}

public class StructTestEntity2
{
    public TestStruct2 Property { get; set; }
}

public class StructTestFilter2
{
    public TestStruct2? Property { get; set; }
}

public class ClassTestEntity1
{
    public TestClass1 Property { get; set; } = null!;
}

public class ClassTestFilter1
{
    public TestClass1? Property { get; set; }
}

public class ClassTestEntity2
{
    public TestClass2 Property { get; set; } = null!;
}

public class ClassTestFilter2
{
    public TestClass2? Property { get; set; }
}

public sealed class StructClassTest
{
    [Fact]
    public void TestCompareWithoutOperatorShouldFail_Struct()
    {
        var expressionMaker = new FilterBuilder().Register<StructTestEntity1, StructTestFilter1>().Build();
        var entity = new StructTestEntity1 { Property = new TestStruct1 { X = 1 } };
        var filter = new StructTestFilter1 { Property = new TestStruct1 { X = 2 } };

        // TestStruct1 doesn't have an equality operator defined, so it's not used for filtering
        Assert.True(expressionMaker.GetFunc<StructTestEntity1, StructTestFilter1>(filter)(entity));
    }

    [Fact]
    public void TestCompareWithoutOperatorShouldFail_Class()
    {
        var expressionMaker = new FilterBuilder().Register<ClassTestEntity1, ClassTestFilter1>().Build();
        var entity = new ClassTestEntity1 { Property = new TestClass1 { X = 1 } };
        var filter = new ClassTestFilter1 { Property = new TestClass1 { X = 2 } };

        // TestStruct1 doesn't have an equality operator defined, so it's not used for filtering
        Assert.True(expressionMaker.GetFunc<ClassTestEntity1, ClassTestFilter1>(filter)(entity));
    }

    [Fact]
    public void TestCompareWithOperatorShouldSucceed_Struct()
    {
        var expressionMaker = new FilterBuilder().Register<StructTestEntity2, StructTestFilter2>().Build();
        var entity = new StructTestEntity2 { Property = new TestStruct2 { X = 1 } };
        var filter = new StructTestFilter2 { Property = new TestStruct2 { X = 2 } };

        // TestStruct1 doesn't have an equality operator defined, so it's not used for filtering
        Assert.False(expressionMaker.GetFunc<StructTestEntity2, StructTestFilter2>(filter)(entity));
    }

    [Fact]
    public void TestCompareWithOperatorShouldSucceed_Class()
    {
        var expressionMaker = new FilterBuilder().Register<ClassTestEntity2, ClassTestFilter2>().Build();
        var entity = new ClassTestEntity2 { Property = new TestClass2 { X = 1 } };
        var filter = new ClassTestFilter2 { Property = new TestClass2 { X = 2 } };

        // TestStruct1 doesn't have an equality operator defined, so it's not used for filtering
        Assert.False(expressionMaker.GetFunc<ClassTestEntity2, ClassTestFilter2>(filter)(entity));
    }
}
