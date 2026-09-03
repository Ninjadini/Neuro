using NUnit.Framework;

namespace Ninjadini.Neuro.IntegrationTests;

/// Tests for the top level entry points - Write/Read, WriteObject/ReadObject and WriteGlobalTyped/ReadGlobalTyped.
/// The main thing they cover is that the object's runtime type is what gets written, not the generic parameter.
public class TopLevelReadWriteTests
{
    NeuroBytesReader BinReader => NeuroBytesReader.Shared;
    NeuroBytesWriter BinWriter => NeuroBytesWriter.Shared;
    NeuroJsonReader JsonReader => NeuroJsonReader.Shared;
    NeuroJsonWriter JsonWriter => NeuroJsonWriter.Shared;

    static TopLevelAnimal MakeDog() => new TopLevelDog() { Legs = 4, Barks = 7 };
    static TopLevelPuppy MakePuppy() => new TopLevelPuppy() { Legs = 4, Barks = 1, Weeks = 12 };

    void AssertIsDog(TopLevelAnimal value)
    {
        Assert.IsInstanceOf<TopLevelDog>(value);
        Assert.AreEqual(4, value.Legs);
        Assert.AreEqual(7, ((TopLevelDog)value).Barks);
    }

    void AssertIsPuppy(TopLevelAnimal value)
    {
        Assert.IsInstanceOf<TopLevelPuppy>(value);
        Assert.AreEqual(4, value.Legs);
        Assert.AreEqual(1, ((TopLevelPuppy)value).Barks);
        Assert.AreEqual(12, ((TopLevelPuppy)value).Weeks);
    }

    // ---- sub class written through a base class generic param ----

    [Test]
    public void Binary_WriteSubClassAsBaseType()
    {
        var bytes = BinWriter.Write(MakeDog()).ToArray();
        AssertIsDog(BinReader.Read<TopLevelAnimal>(bytes));
    }

    [Test]
    public void Json_WriteSubClassAsBaseType()
    {
        var json = JsonWriter.Write(MakeDog());
        Assert.IsTrue(json.Contains(NeuroJsonWriter.FieldName_ClassTag), json);
        AssertIsDog(JsonReader.Read<TopLevelAnimal>(json));
    }

    [Test]
    public void Binary_WriteSubSubClassAsBaseType()
    {
        TopLevelAnimal value = MakePuppy();
        AssertIsPuppy(BinReader.Read<TopLevelAnimal>(BinWriter.Write(value).ToArray()));
    }

    [Test]
    public void Json_WriteSubSubClassAsBaseType()
    {
        TopLevelAnimal value = MakePuppy();
        AssertIsPuppy(JsonReader.Read<TopLevelAnimal>(JsonWriter.Write(value)));
    }

    [Test]
    public void Binary_WriteSubSubClassAsMiddleType()
    {
        TopLevelDog value = MakePuppy();
        AssertIsPuppy(BinReader.Read<TopLevelDog>(BinWriter.Write(value).ToArray()));
    }

    [Test]
    public void Json_WriteSubSubClassAsMiddleType()
    {
        TopLevelDog value = MakePuppy();
        AssertIsPuppy(JsonReader.Read<TopLevelDog>(JsonWriter.Write(value)));
    }

    [Test]
    public void Binary_WriteInterfaceImplementationAsInterface()
    {
        ITopLevelThing value = new TopLevelThing() { Value = 5 };
        var result = BinReader.Read<ITopLevelThing>(BinWriter.Write(value).ToArray());
        Assert.IsInstanceOf<TopLevelThing>(result);
        Assert.AreEqual(5, ((TopLevelThing)result).Value);
    }

    [Test]
    public void Json_WriteInterfaceImplementationAsInterface()
    {
        ITopLevelThing value = new TopLevelThing() { Value = 5 };
        var result = JsonReader.Read<ITopLevelThing>(JsonWriter.Write(value));
        Assert.IsInstanceOf<TopLevelThing>(result);
        Assert.AreEqual(5, ((TopLevelThing)result).Value);
    }

    [Test]
    public void Binary_WriteAsBaseTypeMatchesWriteObject()
    {
        var viaGeneric = BinWriter.Write(MakeDog()).ToArray();
        var viaObject = BinWriter.WriteObject(MakeDog()).ToArray();
        CollectionAssert.AreEqual(viaObject, viaGeneric);
    }

    [Test]
    public void Json_WriteAsBaseTypeMatchesWriteObject()
    {
        Assert.AreEqual(JsonWriter.WriteObject(MakeDog()), JsonWriter.Write(MakeDog()));
    }

    // ---- base class instances still round trip without a tag ----

    [Test]
    public void Binary_WriteBaseClassItself()
    {
        var value = new TopLevelAnimal() { Legs = 2 };
        var result = BinReader.Read<TopLevelAnimal>(BinWriter.Write(value).ToArray());
        Assert.AreEqual(typeof(TopLevelAnimal), result.GetType());
        Assert.AreEqual(2, result.Legs);
    }

    [Test]
    public void Json_WriteBaseClassItself()
    {
        var value = new TopLevelAnimal() { Legs = 2 };
        var result = JsonReader.Read<TopLevelAnimal>(JsonWriter.Write(value));
        Assert.AreEqual(typeof(TopLevelAnimal), result.GetType());
        Assert.AreEqual(2, result.Legs);
    }

    [Test]
    public void Json_ReadSubClassWithoutSubTypeFieldInJson()
    {
        // Hand written json won't necessarily spell out -subType when the target type is already the sub class.
        var result = JsonReader.Read<TopLevelPuppy>("{\"Legs\": 4, \"Barks\": 1, \"Weeks\": 12}");
        AssertIsPuppy(result);
    }

    // ---- nulls ----

    [Test]
    public void NullWritesDoNotThrow()
    {
        TopLevelAnimal value = null;
        Assert.AreEqual(0, BinWriter.Write(value).Length);
        Assert.AreEqual("null", JsonWriter.Write(value));
        Assert.AreEqual("null", JsonWriter.WriteObject(null));
        Assert.AreEqual("null", JsonWriter.WriteGlobalTyped(null));
    }

    // ---- types that can not stand on their own ----

    [Test]
    public void Binary_WritingAPrimitiveOnItsOwnThrows()
    {
        Assert.Throws<Exception>(() => BinWriter.Write(5));
        Assert.Throws<Exception>(() => BinWriter.Write("hello"));
        Assert.Throws<Exception>(() => BinReader.Read<int>(new byte[] { 1, 2 }));
    }

    [Test]
    public void Json_WritingAPrimitiveOnItsOwnThrows()
    {
        Assert.Throws<Exception>(() => JsonWriter.Write(5));
        Assert.Throws<Exception>(() => JsonWriter.Write("hello"));
        Assert.Throws<Exception>(() => JsonReader.Read<int>("5"));
    }

    // ---- reading into a type the data isn't ----

    [Test]
    public void Binary_ReadingAsAnUnrelatedSubClassThrows()
    {
        var bytes = BinWriter.Write(MakeDog()).ToArray();
        Assert.Throws<Exception>(() => BinReader.Read<TopLevelCat>(bytes));
    }

    [Test]
    public void Json_ReadingAsAnUnrelatedSubClassThrows()
    {
        var json = JsonWriter.Write(MakeDog());
        Assert.Throws<Exception>(() => JsonReader.Read<TopLevelCat>(json));
    }

    // ---- the other two entry points still behave ----

    [Test]
    public void Binary_GlobalTypedRoundTrip()
    {
        AssertIsPuppy((TopLevelAnimal)BinReader.ReadGlobalTyped(BinWriter.WriteGlobalTyped(MakePuppy()).ToArray()));
    }

    [Test]
    public void Json_GlobalTypedRoundTrip()
    {
        AssertIsPuppy((TopLevelAnimal)JsonReader.ReadGlobalTyped(JsonWriter.WriteGlobalTyped(MakePuppy())));
    }

    [Test]
    public void Binary_ObjectRoundTrip()
    {
        var bytes = BinWriter.WriteObject(MakePuppy()).ToArray();
        AssertIsPuppy((TopLevelAnimal)BinReader.ReadObject(bytes, typeof(TopLevelAnimal)));
    }

    [Test]
    public void Json_ObjectRoundTrip()
    {
        var json = JsonWriter.WriteObject(MakePuppy());
        AssertIsPuppy((TopLevelAnimal)JsonReader.ReadObject(json, typeof(TopLevelAnimal)));
    }

    // ---- the reflection based entry points report the same errors as the generic ones ----

    [Test]
    public void ObjectEntryPoints_NonStandaloneTypeThrowsTheRealError()
    {
        // it used to come back wrapped in a TargetInvocationException, hiding the message.
        var bytes = BinWriter.Write(MakeDog()).ToArray();
        var json = JsonWriter.Write(MakeDog());
        Assert.Throws<Exception>(() => BinWriter.WriteObject(5));
        Assert.Throws<Exception>(() => JsonWriter.WriteObject(5));
        Assert.Throws<Exception>(() => BinReader.ReadObject(bytes, typeof(int)));
        Assert.Throws<Exception>(() => JsonReader.ReadObject(json, typeof(int)));
    }

    [Test]
    public void Binary_ReadGlobalTypedOnPlainDataThrows()
    {
        Assert.Throws<Exception>(() => BinReader.ReadGlobalTyped(BinWriter.Write(MakeDog()).ToArray()));
    }

    [Test]
    public void Binary_ReadPlainOnGlobalTypedDataThrows()
    {
        var globalBytes = BinWriter.WriteGlobalTyped(MakeDog()).ToArray();
        Assert.Throws<Exception>(() => BinReader.Read<TopLevelAnimal>(globalBytes));
        Assert.Throws<Exception>(() => BinReader.ReadObject(globalBytes, typeof(TopLevelAnimal)));
    }

    // ---- empty / null input reads back as null everywhere ----

    [Test]
    public void Binary_EmptyInputReadsAsNull()
    {
        var empty = Array.Empty<byte>();
        Assert.IsNull(BinReader.Read<TopLevelAnimal>(empty));
        Assert.IsNull(BinReader.ReadObject(empty, typeof(TopLevelAnimal)));
        Assert.IsNull(BinReader.ReadGlobalTyped(empty));
    }

    [Test]
    public void Json_EmptyInputReadsAsNull()
    {
        foreach (var json in new[] { null, "", "   ", "null" })
        {
            Assert.IsNull(JsonReader.Read<TopLevelAnimal>(json), $"Read<T>(\"{json}\")");
            Assert.IsNull(JsonReader.ReadObject(json, typeof(TopLevelAnimal)), $"ReadObject(\"{json}\")");
            Assert.IsNull(JsonReader.ReadGlobalTyped(json), $"ReadGlobalTyped(\"{json}\")");
        }
    }

    [Test]
    public void Json_NullWriteRoundTripsToNull()
    {
        TopLevelAnimal value = null;
        Assert.IsNull(JsonReader.Read<TopLevelAnimal>(JsonWriter.Write(value)));
    }

    // ---- the shared writer reuses its buffer, so copy before writing again ----

    [Test]
    public void Binary_ToArrayBeforeTheNextWriteKeepsBothResults()
    {
        var first = BinWriter.Write(new TopLevelDog { Legs = 1, Barks = 111 }).ToArray();
        var second = BinWriter.Write(new TopLevelDog { Legs = 2, Barks = 222 }).ToArray();

        Assert.AreEqual(111, ((TopLevelDog)BinReader.Read<TopLevelAnimal>(first)).Barks);
        Assert.AreEqual(222, ((TopLevelDog)BinReader.Read<TopLevelAnimal>(second)).Barks);
    }
}

[Neuro(20), NeuroGlobalType(120)]
public partial class TopLevelAnimal
{
    [Neuro(1)] public int Legs;
}

[Neuro(21)]
public partial class TopLevelDog : TopLevelAnimal
{
    [Neuro(1)] public int Barks;
}

[Neuro(22)]
public partial class TopLevelPuppy : TopLevelDog
{
    [Neuro(1)] public int Weeks;
}

[Neuro(23)]
public partial class TopLevelCat : TopLevelAnimal
{
    [Neuro(1)] public int Meows;
}

[Neuro(24)]
public partial interface ITopLevelThing
{
}

[Neuro(25)]
public partial class TopLevelThing : ITopLevelThing
{
    [Neuro(1)] public int Value;
}
