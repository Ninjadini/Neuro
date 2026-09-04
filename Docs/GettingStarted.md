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

### Types that are not supported
These are a compile error rather than a runtime surprise:
- `byte`, `sbyte`, `short`, `ushort`, `char`, `decimal` — numbers are varint encoded, so a narrow type
  saves nothing. Use `int` / `uint` / `long` / `ulong`, `string` for `char`, `double` (or a `long` of
  scaled units) for `decimal`. Enums backed by any of them are fine.
- Arrays, `HashSet<>`, `IReadOnlyList<>` and other collections — only `List<>` and `Dictionary<,>`.
- Dictionary keys that aren't string, struct or enum.

Also supported, in case you didn't expect them: `Guid`, `Uri`, `Version`, `DateTimeOffset`, and Unity's
`Vector2/3/4`, `Color`, `Gradient`, `AnimationCurve`, `Rect`, `Bounds`, `LayerMask` and friends.

### See it in editor for editing the data
- `Tools` > `Neuro` > `❖ Editor`
- It should already have selected your first type
- Press `＋ Add` to add your first item.
- Note that all items has a unique uint `RefId` and string `RefName`
- This is reflected in the JSON file name
- You can see the location of the file by clicking `⊙ File`

### RefIds in file names and JSON
A new item's `RefId` is a random number rather than the next number up, so that two people adding items on
separate branches do not both take the same id and conflict on merge.

In memory and in the binary format a `RefId` is always a `uint`. In file names and in JSON it is written in
base36 (`0-9a-z`), which keeps a generated id down to 4 characters - `NeuroData/1-MyItem/4zbc-my_item.json`.
The generated range is picked so that every generated id is exactly 4 base36 chars and costs 3 bytes in the
binary format. base36 rather than base62 because data file names have to survive a case insensitive file system.

Every id has exactly one spelling and every spelling is one id. Note that `20` is base36, so it is the number
72 - not 20. Only the text changes; the id is the same `uint` everywhere else.

Hovering the `RefId` field in the editor tells you the plain number. If you want to see it everywhere - the
reference drop downs, recent items, validator messages - turn on `Show Raw Ref Id Numbers` in
`Project Settings > Ninjadini Neuro`, and ids read as `1v83 (87123)`. It is display only: file names, json and
the id you type into the `RefId` field are always plain base36.

### Migrating data from before base36 RefIds
RefIds used to be written in plain decimal, so `20-wood.json` meant RefId 20 and now reads as 72. If you have
data from that version, Neuro warns you on load, and `Tools > Neuro > Migrate RefIds to base36...` converts it.

The migration keeps the id numbers exactly as they are and only changes how they are spelled, so ids held
outside the JSON - in save games, prefabs, or hard coded in your code - keep pointing at the same items.

It can only be run once, and it says so if you try again. A name made only of digits is a valid base36 id
(RefId 72 is `20`), so there is no way to tell a converted file from an unconverted one by looking at it - which
is why the project records that it has been done. Commit your data before running it.

### Changing an item's RefId
Type a new id into the `RefId` field at the top of the editor and press enter. You will be asked to confirm, and
told how many other items reference this one.

On confirm Neuro checks the id is free, repoints every `Reference<>` in the data that pointed at the old id,
renames the data file and saves everything it changed.

Two things it can not do for you:
- Undo only covers the item itself, not the other items that were repointed.
- Ids stored outside the Neuro data - in scenes, prefabs, save games or hard coded in your code - are not updated.

### How to read from referencable/config at runtime
```
// Get the table of certain type
var table = NeuroDataProvider.GetSharedTable<MyFirstNeuroObject>();

// loop through all items (this will cause all data to be loaded if not already)
foreach (var theItem in table.SelectAll())
{
}

// Get an item by id or name
var myItem = table.Get(myRefId);   // uint
var sameItem = table.Get("myItem"); // RefName
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
See Save() in example [CraftClickerLogic.cs](../Development~/ExampleProject/Assets/Scripts/CraftClicker/CraftClickerLogic.cs)

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

### Saving without a MonoBehaviour
Same thing, plain C#:
```
var save = LocalNeuroContinuousSave<MyPlayerSaveData>.CreateInPersistedData("save");
var data = save.GetData();   // loads from disk on first call, creates a new one if there is no file
data.PlayerLevel++;
save.Save();                 // writes straight into the open file stream, no allocations
save.DelayedSave(1f);        // or coalesce rapid changes into one write
```
It holds one file open for the life of the object, which is what makes it allocation free - so it is one
instance per file. If loading fails, the bad file is copied to `<file>-failed<timestamp>` and you get a
fresh object rather than an exception.

For several files, or one off reads and writes, use `LocalNeuroStorage` instead - `Save(obj, name)`,
`TryLoad<T>(name)`, `Delete(name)`, defaulting to `Application.persistentDataPath`.

> [!NOTE]
> Saves are binary and not encrypted - anyone can read and edit them. Nothing stops you writing your own
> bytes to disk if you need more than that.


# What's next ?

[Demo Project >](DemoProject.md)

[Advanced usages >](AdvancedUsages.md)

[BackwardCompatibility >](BackwardCompatibility.md)

[Editor Tools & Settings >](EditorTools.md)

[Editor Customisation >](EditorCustomisation.md)
