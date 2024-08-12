# Neuro

Your friendly data orientated content management toolset for Unity.

## Getting started

### Import to your Unity project
- Requires Unity 2022 or higher
- `Window` > `Package Manager` > `Add` > `Install package from git URL...`
- Enter `https://github.com/Ninjadini/Neuro.git` for latest.
> [!TIP]
> No versioning yet. If you prefer a specific commit to be safe, you can use this format `https://github.com/Ninjadini/Neuro/commit/<COMMIT HASH HERE>`


### Your first Neuro reference type
They are essentially your ScriptableObjects that hold some config data
```
using Ninjadini.Neuro;
using System.Collections.Generic;

[NeuroGlobalType(1)] // < the number needs to be unique globally for every root IReferencable type
public class MyFirstNeuroObject : Referencable
{
    [Neuro(1)] public string MyFirstString;
    [Neuro(2)] public int MyFirstInt;
    
    // ^ The Neuro(#) need to be unique per class
}
```

### An example of supported types
```
    public class MyUberObject
    {
        [Neuro(1)] public string AString;
        [Neuro(2)] public List<string> AListOfStrings;
    
        [Neuro(10)] public float AFloat;
        [Neuro(13)] public MyEnum AnEnum;
        [Neuro(15)] public DateTime DateTime;
        [Neuro(16)] public TimeSpan TimeSpan;
        
        [Neuro(20)] public MyChildObject SomeChildObject;
        [Neuro(21)] public List<MyChildObject> SomeChildObjects;
        
        [Neuro(30)] public MyChildStruct SomeStruct;
        
        [Neuro(31)] public float? ANullableFloat;
        [Neuro(32)] public MyChildStruct? SomeNullableStruct;
        
        [Neuro(40)] public Reference<MyFirstNeuroObject> AReference;
        [Neuro(41)] public List<Reference<MyFirstNeuroObject>> AListOfReferences;
    }
    public class MyChildObject
    {
        [Neuro(1)] public string Value;
    }
    public struct MyChildStruct
    {
        [Neuro(1)] public string Value;
    }
    public enum MyEnum
    {
        A, B, C
    }
```

### See it in editor for editing the data
- `Tools` > `Neuro` > `❖ Editor`
- It should already have select your first type
- Press `＋ Add` to add your first item.
- Note that all items has a unique uint `RefId` and string `RefName`
- This is reflected in the JSON file name
- You can see the location of the file by clicking `⊙ File`

### How to read from referencable/config at runtime
```
// Get the neuro data provider, static shared version read data from neuro editor automatically.
var sharedDataProvider = NeuroDataProvider.Shared;

// Get the table of certain type
var table = sharedDataProvider.References.GetTable<MyFirstNeuroObject>();

// loop through all items (this will cause all data to be loaded if not already)
foreach (var theItem in table.SelectAll())
{
}

// Get an item by id or name
var myItem = table.GetId(<myid>);
```

### How to reference to other referencables
```
public class SomeObject
{
    [Neuro(1)] public Reference<MyFirstNeuroObject> RefObj;
    [Neuro(2)] public List<Reference<MyFirstNeuroObject>> RefObjs;
}

public static void PrintValues(SomeObject obj)
{
    var firstObj = NeuroDataProvider.Shared.Get(obj.RefObj);
    Debug.Log("MyFirstString: " + firstObj.MyFirstString);
    Debug.Log("MyFirstInt: " + firstObj.MyFirstInt);
}
```

### References in serialised MonoBehaviour
```
public class MyMonoBehaviour : MonoBehaviour
{
    public Reference<MyFirstNeuroObject> RefObj;
    [SerializeField] private Reference<MyFirstNeuroObject> RefObj_private;
}
```

### Polymorphic types 
```
public class BaseEntity
{
    [Neuro(1)] string Name; // This # only needs to be unique locally in this class
}

[Neuro(1)] // This # needs to be unique in all subclasses of BaseEntity
public class VehicleEntity
{
    [Neuro(1)] float Speed; // This # only needs to be unique locally in this class
}

[Neuro(2)] // This # needs to be unique in all subclasses of BaseEntity
public class CharacterEntity
{
    [Neuro(1)] string Name;
}
```

### Polymorphic types with interface as root
```
[Neuro(0)] // Because you will not have any fields, this is how you tell neuro that this is the root
public interface IBaseEntity
{
}

[Neuro(0)] // Because you will not have any fields, this is how you tell neuro that this is the root
public interface BaseEntity
{
// A class with no neuro fields
}
```

### Singleton style data
Guarantees there is only 1 of this type in table.
```
[NeuroGlobalType(2)]
public class GameSettings : ISingletonReferencable
{
    [Neuro(1)] public string GameName;
}

// Get the object
var settings = NeuroDataProvider.Shared.References.Get<GameSettings>();
```

### Load unity assets
```
public class SomeObject
{
    [AssetType(typeof(UnityEngine.Sprite))] // < Optional but it guides the editor to show the right types
    [Neuro(1)] public AssetAddress Icon;
}

void LoadIcon(SomeObject obj)
{
    obj.Icon.LoadAssetAsync<Sprite>(delegate(Sprite result)
    {
        image.sprite = result;
    });
}
```

### Custom reference drop down display
```
// Text customisation
public class MyOtherReferencableObject : Referencable, INeuroRefDropDownCustomizable
{
    [Neuro(1)] public string Name;
    
    string INeuroRefDropDownCustomizable.GetRefDropdownText(NeuroReferences references) => RefId + " : "+ RefName + " -- "+Name;
}

// icon display customisation
public class MyOtherReferencableObject : Referencable, INeuroRefDropDownIconCustomizable
{
    [Neuro(1)] public AssetAddress Icon;

    string INeuroRefDropDownCustomizable.GetRefDropdownText(NeuroReferences references) => null; // no custom text in this example.

    AssetAddress INeuroRefDropDownIconCustomizable.RefDropdownIcon => Icon;
}
```

### Read write binary data
```
// Write a neuro object to byte array
var bytes = NeuroBytesWriter.Shared.Write(myData).ToArray();

// Read byte array to neuro object
var myReadData = NeuroBytesReader.Shared.Read<MyData>(bytes);
```

### Read write JSON data
```
// Write a neuro object to JSON 
var jsonString = NeuroJsonWriter.Shared.Write(data);

// Read JSON string to neuro object
var myData = NeuroJsonReader.Shared.Read<MyData>(jsonString);
```

### Custom editor drawer
Say you want to show the 3 values in one line without the name labels.
And it will say an error message if any of the values have lower than 1 value
(Please note, if you want to do validation its actually better to use INeuroContentValidator)
```
public struct FloatABC
{
    [Neuro(1)] public float a;
    [Neuro(2)] public float b;
    [Neuro(3)] public float c;
}
    
public class MyGameNeuroEditorProvider : ICustomNeuroEditorProvider
{
    VisualElement ICustomNeuroEditorProvider.CreateCustomDrawer(NeuroObjectInspector inspector, ObjectInspector.Data data)
    {
        if (data.type == typeof(FloatABC))
        {
            // Return the drawer if it's the type you want to customise
            return new FloatABCFieldElement(inspector, data);
        }
        // idea is that you can have other type checks here
        return null;
    }
    
    class FloatABCFieldElement : ObjectInspector
    {
        HelpBox helpBox;
        
        internal FloatABCFieldElement(NeuroObjectInspector inspector, ObjectInspector.Data data)
        {
            var value = data.getter();
            
            style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            // Draw a
            var fieldInfo = typeof(FloatABC).GetField(nameof(FloatABC.a));
            var fieldDrawerData = CreateDataForField(data, value, fieldInfo);
            fieldDrawerData.name = data.name; // it would normally print a, but here we just want the parent field's name
            var fieldElement = ObjectInspectorFields.CreateField(fieldDrawerData);
            Add(fieldElement);
            
            // draw b
            fieldInfo = typeof(FloatABC).GetField(nameof(FloatABC.b));
            fieldDrawerData = CreateDataForField(data, value, fieldInfo);
            fieldDrawerData.name = ""; // it would normally print b, but here we just want 3 fields with no naming
            fieldElement = ObjectInspectorFields.CreateField(fieldDrawerData);
            Add(fieldElement);
            
            // draw c
            fieldInfo = typeof(FloatABC).GetField(nameof(FloatABC.c));
            fieldDrawerData = CreateDataForField(data, value, fieldInfo);
            fieldDrawerData.name = ""; // it would normally print c, but here we just want 3 fields with no naming
            fieldElement = ObjectInspectorFields.CreateField(fieldDrawerData);
            Add(fieldElement);

            helpBox = new HelpBox();
            helpBox.messageType = HelpBoxMessageType.Error;
            Add(helpBox);

            schedule.Execute(() => OnUpdate(data)).Every(10);
        }

        void OnUpdate(ObjectInspector.Data data)
        {
            var value = (FloatABC)data.getter();
            if (value.a < 1 || value.b < 1 || value.c < 1)
            {
                helpBox.text = "All values must be 1 or above";
                helpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                helpBox.style.display = DisplayStyle.None;
            }
            // This is just an example of updating the UI
            // Actual validation stuff is better done via INeuroContentValidator<T> see later section for more info
        }
    }
}
```

### Content validator
```
    public struct FloatABC
    {
        [Neuro(1)] public float a;
        [Neuro(2)] public float b;
        [Neuro(3)] public float c;
    }
    
    // This class should live in Editor folder
    public class FloatABCValidator : INeuroContentValidator<FloatABC>
    {
        public void Test(FloatABC valueToTest, NeuroContentValidatorContext context)
        {
            Assert.GreaterOrEqual(valueToTest.a, 1);
            Assert.GreaterOrEqual(valueToTest.b, 1);
            Assert.GreaterOrEqual(valueToTest.c, 1);
        }
    }
```
In the editor's 'Tests' section at the bottom, it'll turn red if the validation fails.

---

## Goals
- No schema
- Polymorphic
- Default values
- Private fields (fake public immutability)
- References
- Custom serializers (Unity's Vector3, Rect, etc)
- Object Pooling
- Fast read and write
- Minimal allocations.
- No reflection
- Ultra compact data
- Backcompat
- Any objects inspector in Unity
- Git friendly text storage
- OnDemand deserialization? (only deserialize when referenced item is requested)
- Unity asset references via Addressables
- Record type + immutable arrays ? ❌
- Non-backcompact, ultra compact type. ❌
- NonAlloc String? ❌
- Async loading? ❌
- Referring to sub-tables .e.g. Item 2's level 3's ? (could just be code pattern) ❌

## Supported Types
- Primitives: bool, byte, int, uint, long, ulong, float, double
- Enums
- List<>
- System structs: DateTime, TimeSpan
- Classes and subclasses
- Structs
- Nullable structs and primitives

## Maybe - Future Features
- Nested embeds (same ref in multiple places)
- Custom constructors ? (Unity's scriptable objects, but sounds like a bad idea)

## What (could be) bad about it?
- Only supports fields, properties are supported via a workaround
- Codegen might get slow on a larger projects? Perhaps you need to keep all the model files in one project and ignore other projcets
- Maybe hard to be able to tell which types are supported - but fixable with Roslyn validation