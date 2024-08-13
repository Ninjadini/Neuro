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
- It should already have selected your first type
- Press `＋ Add` to add your first item.
- Note that all items has a unique uint `RefId` and string `RefName`
- This is reflected in the JSON file name
- You can see the location of the file by clicking `⊙ File`

### How to read from referencable/config at runtime
```
// Get the neuro data provider, static shared version read data from neuro editor automatically.
var references = NeuroDataProvider.SharedReferences;

// Get the table of certain type
var table = references.GetTable<MyFirstNeuroObject>();

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
    var firstObj = NeuroDataProvider.SharedReferences.Get(obj.RefObj);
    Debug.Log("MyFirstString: " + firstObj.MyFirstString);
    Debug.Log("MyFirstInt: " + firstObj.MyFirstInt);
}
```

### References in serialised MonoBehaviour
```
public class MyMonoBehaviour : MonoBehaviour
{
    public Reference<MyFirstNeuroObject> RefObj;
}
```

### Polymorphic types 
```
public class BaseEntity
{
    [Neuro(1)] string Name; // This # only needs to be unique locally in this class
}

[Neuro(1)] // This # needs to be unique in all subclasses of BaseEntity
public class VehicleEntity : BaseEntity
{
    [Neuro(1)] float Speed; // This # only needs to be unique locally in this class
}

[Neuro(2)] // This # needs to be unique in all subclasses of BaseEntity
public class CharacterEntity : BaseEntity
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
var settings = NeuroDataProvider.SharedReferences.Get<GameSettings>();
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

### Basic editor customisation
```
[DisplayName("Test > AnotherReferencableObject")] // < The type name used in editor dropdown list
[ToolTip("Tooltip for this class... Shows up when you mouse over any of the elements of this clas (but not if you are in play mode)")] // You can also use [Description()] 
public class AnotherReferencableObject : Referencable
{
    [ToolTip("Tooltip for this particular field")]
    [Neuro(1)] public string Name;
    
    [Header("A foldout header")]
    [Neuro(2)] public string Value1;
    [Neuro(3)] public string Value2;
}
```

### Full editor customisation
Say you want to show the 3 values in one line without the name labels.
And it will say an error message if any of the values have lower than 1 value

> [!TIP]
> If you want to do validation it's actually better to use INeuroContentValidator

> [!CAUTION]
> API MAY CHANGE FOR THIS
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
    
    // This class should live in Editor folder (it will still work if you don't)
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

Your validator will also show up as one of the tests in Unity's edit mode test runner under NeuroContentTestsRunner > TestRefTables

### Converting external objects to be neuro friendly
Say you want to use an object in Neuro world, but you can not modify the code, e.g. 3rd party
You can write the 'sync' code manually. This is how Unity's build in data types such as Vector3 are registered.
See full example of Unity ones in this class: NeuroDefaultUnityTypesHook

Short example using FloatABC:
```
    // This is auto picked up by code gen to be registered because it extends from INeuroCustomTypesRegistryHook
    public struct MyCustomTypeRegistryHook : INeuroCustomTypesRegistryHook
    {
        public void Register()
        {
            NeuroSyncTypes.Register((INeuroSync neuro, ref FloatABC value) => {
                    neuro.Sync(1, "a", ref value.a);
                    neuro.Sync(2, "b", ref value.b);
                    neuro.Sync(3, "c", ref value.c);
            // number is used for binary, name string is used for json, ref value is used for actual data read/write.
        }
    }
```

### Preprocessing / stripping data for build
Say you have some data you don't want to expose onto public facing builds.
Perhaps you have dev notes stored in the data.

Example:
```
    public class MyFirstNeuroObjectProcessor : INeuroBundledDataResourcesForBuildProcessor
    {
        public void PrepBeforeBuildProcessing(NeuroReferences neuroReferences, BuildReport buildReport)
        {
        }

        public bool ProcessForInclusion(IReferencable referencable)
        {
            if(referencable is MyFirstNeuroObject obj)
            {
                obj.MyFirstString = null;
            }
            return true;
        }
    }
```

### Saving neuro reference changes in editor scripts 
```
// grab the item we want to modify... really same as using NeuroDataProvider here.
var itemToModify = NeuroEditorDataProvider.Shared.References.Get<MyFirstNeuroObject>("myItem");

// do the modification
itemToModify.MyFirstString = "My modified string";

// save it back out to neuro json file.
NeuroEditorDataProvider.Shared.SaveData(itemToModify);
```

### Visit every values of a neuro object

Example:
```
var vistor = new NeuroVisitor();
var refs = NeuroDataProvider.SharedReferences;
vistor.Visit(myObjToVist, new MyCustomVisitor(refs));

    public class MyCustomVisitor : NeuroVisitor.IInterface
    {
        NeuroReferences _refs;
        public MyCustomVisitor(NeuroReferences refs)
        {
            _refs = refs;
        }
        
        public void BeginVisit<T>(ref T obj, string name, int? listIndex)
        {
            Debug.Log("BeginVisit: " + name +": "+ obj);
        }

        public void EndVisit()
        {
        }

        public void VisitRef<T>(ref Reference<T> reference) where T : class, IReferencable
        {
            Debug.Log("VisitRef: " + reference.TryGetIdAndName(_refs));
        }
    }
```
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