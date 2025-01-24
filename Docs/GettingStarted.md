# Getting started

See walkthrough video, Unity 2022.2 or higher:  
https://youtu.be/AZOHbK-prHo

### Import to your Unity project
- Requires Unity 2022.2 or higher
- `Window` > `Package Manager` > `Add` > `Install package from git URL...`
- Enter `https://github.com/Ninjadini/Neuro.git` for latest.
> [!TIP]
> To target a specific tag / release - to be safe from surprise API changes, use this format:  
`https://github.com/Ninjadini/Neuro.git#v0.1.2`

### Your first Neuro reference type
They are essentially your ScriptableObjects that hold some config data.  
You can reference these items from other places via Reference<T> type - similar to linking objects in Unity, e.g. linking a Material to a Renderer's material field.  
Each reference has a RefId (uint) which is unique, and a RefName (string) which does not need to be unique.  
RefId number is what's used to link to the references.
RefName is used for easy identification of the item.
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
        [Neuro(22)] public Dictionary<string, MyChildObject> SomeDictionary;
        
        [Neuro(30)] public MyChildStruct SomeStruct;
        
        [Neuro(31)] public float? ANullableFloat;
        [Neuro(32)] public MyChildStruct? SomeNullableStruct;
        
        [Neuro(40)] public Reference<MyFirstNeuroObject> AReference;
        [Neuro(41)] public List<Reference<MyFirstNeuroObject>> AListOfReferences;
    }
    public class MyChildObject
    {
        [Neuro(1)] public string Value = "abcd"; // default values are supported
    }
    public struct MyChildStruct
    {
        [Neuro(1)] public string Value;
    }
    public enum MyEnum
    {
        A, B, C
    }
    
    public partial class MyClassWithPrivateFields 
    {
        // ^ If you want to use private fields, you must make the class partial
        // This is so the code gen can access your private fields
        
        [Neuro(1)] private string _privateValue;
        [Neuro(2)] private List<string> _privateValues;
        // ^ these fields will still be exposed in Neuro Editor so you can set the values.
        
        public string PrivateValue => _privateValue;
        public IReadOnlyList<string> PrivateValues => _privateValues;
        // ^ exposing the readonly values for outside world
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
// Get the table of certain type
var table = NeuroDataProvider.GetSharedTable<MyFirstNeuroObject>();

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
    var firstObj = obj.RefObj.GetValue();
    Debug.Log("MyFirstString: " + firstObj.MyFirstString);
    Debug.Log("MyFirstInt: " + firstObj.MyFirstInt);
}
```

### References in serialised MonoBehaviour
```
public class MyMonoBehaviour : MonoBehaviour
{
    public Reference<MyFirstNeuroObject> RefObj;

    void Start()
    {
        var obj = RefObj.GetValue();
        Debug.Log("My first string says: " + obj?.MyFirstString);
    }
}
```

# Polymorphic types 
```
public class BaseEntity
{
    [Neuro(1)] string Name; // < This # only needs to be unique locally in this class
}

[Neuro(1)] // < This # needs to be unique in all subclasses of BaseEntity
public class VehicleEntity : BaseEntity
{
    [Neuro(1)] float Speed; // < This # only needs to be unique locally in this class
}

[Neuro(2)] // < This # needs to be unique in all subclasses of BaseEntity
public class CharacterEntity : BaseEntity
{
    [Neuro(1)] string SomeSting;
}
[Neuro(3)] // < This # also need to be unique for subclasses of BaseEntity - Note, this one extends from CharacterEntity
public class PlayerCharacterEntity : CharacterEntity
{
    [Neuro(1)] int SomeInt;
}
```

## Polymorphic types with interface as root
```
[Neuro(0)] // Because you will not have any fields, this is how you tell neuro that this is the root
  // for interfaces, the number can be zero, but if you change it to class later, it will break back-compact
public interface IBaseEntity
{
}

[Neuro(1)] // Because you will not have any fields, this is how you tell neuro that this is the root
  // for classes it needs to be a non-zero number.
public class BaseEntity
{
// A class with no neuro fields
}
```

# Singleton style data
Guarantees there is only 1 of this type in table.
```
[NeuroGlobalType(2)]
public class GameSettings : ISingletonReferencable
{
    [Neuro(1)] public string GameName;
}

// Get the object
var settings = NeuroDataProvider.GetSharedSingleton<GameSettings>();
```

# Loading unity assets
> [!IMPORTANT]
> We can not directly load assets unless it is an Addressable asset or located in Resources folder.
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

# Saving player progress
The easiest in Unity is to use LocalNeuroContinuousSave MonoBehaviour.
See Save() in example [CraftClickerLogic.cs](../ExampleProject~/Assets/Scripts/CraftClicker/CraftClickerLogic.cs)

```
public class MyPlayerSaveData
{
    [Neuro(1)] public int PlayerLevel;
}

public class MyGameLogic : MonoBehaviour
{
    [SerialisedField] LocalNeuroContinuousSave _gameSave;
    // ^ You need add LocalNeuroContinuousSave component in the same GameObject and link to this field in Unity.
    
    public MyPlayerSaveData GetData()
    {
        return _gameSave.GetData<MyPlayerSaveData>();
    }
    
    public void SaveData()
    {
        _gameSave.Save();
    }
}
```


# What's next ?

[Demo Project >](DemoProject.md)

[Advanced usages >](AdvancedUsages.md)

[BackwardCompatibility >](BackwardCompatibility.md)

[Editor Customisation >](EditorCustomisation.md)
