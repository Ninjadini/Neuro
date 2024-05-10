namespace Ninjadini.Neuro.IntegrationTests;

public class UberTestObject
{
    [Neuro(1)] public int Id;
    [Neuro(2)] public float Float;
    [Neuro(3)] public string Name;
    [Neuro(4)] public DateTime Date;
    [Neuro(5)] public TimeSpan TimeSpan;

    [Neuro(11)] public TestEnum1 Enum;
    [Neuro(12)] public TestFlagEnum1 FlagEnum;
        
    [Neuro(20)] public TestChildClass ClassObj;
    [Neuro(21)] public BaseTestClass1 BaseClassObj;
    [Neuro(22)] public ITestInterface Interface;
        
    [Neuro(50)] public TestStruct Struct;
    [Neuro(51)] public Reference<ReferencableClass> Referencable;
    [Neuro(52)] public SingleNumberStruct SingleNumber;
        
    [Neuro(200)] public List<int> ListInt;
    [Neuro(201)] public List<TestEnum1> ListEnum;
    [Neuro(202)] public List<TestChildClass> ListClass;
    [Neuro(203)] public List<TestStruct> ListStruct;
    [Neuro(204)] public List<string> ListTexts;
    [Neuro(205)] public List<BaseTestClass1> ListBaseClasses;
        
    [Neuro(300)] public int? NullableId;
    [Neuro(301)] public TestEnum1? NullableEnum;
    [Neuro(302)] public DateTime? NullableDate;
    [Neuro(303)] public TestStruct? NullableStr;
}

public partial class UberTestClassWithJustLastItem
{
    [Neuro(10000)] public int LastItem;
}

public partial class TestChildClass
{
    [Neuro(1)] public int Id;
    [Neuro(2)] public string Name;

}
    
[Neuro(2), NeuroGlobalType(10)]
public partial class BaseTestClass1
{
    [Neuro(1)] public int Id;
    [Neuro(2)] public string Name;
}
    
[Neuro(3)]
public partial class SubTestClass1 : BaseTestClass1
{
    [Neuro(1)] public int NumValue;
    [Neuro(2)] public string Value;
}
    
    
[Neuro(1)]
public partial interface ITestInterface
{
}
    
[Neuro(2)]
public partial class TestInterfaceImp1 : ITestInterface
{
    [Neuro(1)] public int NumValue;
    [Neuro(2)] public string Value;
}
    
public partial struct TestStruct : IEquatable<TestStruct>
{
    [Neuro(1)] public int Id;
    [Neuro(2)] public string Name;

    public bool Equals(TestStruct other)
    {
        return Id == other.Id && Name == other.Name;
    }
}
    
public partial struct SingleNumberStruct : IEquatable<SingleNumberStruct>
{
    [Neuro(1)] public float Number;

    public bool Equals(SingleNumberStruct other)
    {
        return Math.Abs(Number - other.Number) < 0.000001;
    }
}
    
[NeuroGlobalType(11)]
public partial class ReferencableClass : IReferencable
{
    [Neuro(2)] public string Name;
        
    public uint RefId { get; set; }
    public string RefName { get; set; }
}

public enum TestEnum1
{
    A = 1,
    B = 2,
    C = 3,
}
[Flags]
public enum TestFlagEnum1
{
    None = 0,
    A = 1<<0,
    B = 1<<1,
    C = 1<<2,
}