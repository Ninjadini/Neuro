### Read write binary data
```
// Write a neuro object to byte array
var bytes = NeuroBytesWriter.Shared.Write(myData).ToArray();

// Read byte array to neuro object
var myReadData = NeuroBytesReader.Shared.Read<MyData>(bytes);
```

### Clone neuro data via binary serialization
```
var copiedObject = NeuroBytesWriter.Clone(originalObject);
```

### Read write JSON data
```
// Write a neuro object to JSON 
var jsonString = NeuroJsonWriter.Shared.Write(data);

// Read JSON string to neuro object
var myData = NeuroJsonReader.Shared.Read<MyData>(jsonString);
```
> [!TIP]
> JSON output will print references and enum in this format `"myItem": "2:mySecondItemName"`
> 
> This looks like you can't change the ref name of items or it will unlink the values, but that is not the case
> 
> The only thing that matters is the number. You can just have `"myItem": 2` and it'll work
> 
> The ref name there is just so that it is easy for you to figue out what the item is.

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

Your validator will also be automatically included in Unity's edit mode test runner under NeuroContentTestsRunner > TestRefTables

### Converting external objects to be neuro friendly
Say you want to use an object in Neuro world, but you can not modify the code, e.g. 3rd party.

You can write the 'sync' code manually. This is how Unity's build in data types such as Vector3 are registered.
See full example of Unity ones in this class: [NeuroDefaultUnityTypesHook.cs](Ninjadini.Neuro.Unity/RunTime/NeuroDefaultUnityTypesHook.cs)

Short example using Unity.Mathematics.int2:
```
    // This is auto picked up by code gen to be registered because it extends from INeuroCustomTypesRegistryHook
    public struct NeuroMathematicsTypeHooks : INeuroCustomTypesRegistryHook
    {
        public void Register()
        {
            NeuroSyncTypes.Register((INeuroSync neuro, ref int2 value) =>
            {
                neuro.Sync(1, nameof(value.x), ref value.x);
                neuro.Sync(2, nameof(value.y), ref value.y);
            });
            // number is used for binary, name string is used for json, ref value is used for actual data read/write.
            
            NeuroSyncTypes.Register((INeuroSync neuro, ref int3 value) => 
            ...etc...
        }
    }
```
Unfortunately, ^ this only makes the serialisation to work, but Neuro editor still may not know how to render this item...  
We will need to also register custom editor drawer for neuro.

```
    public class NeuroMathematicsEditors : ICustomNeuroEditorProvider
    {
        VisualElement ICustomNeuroEditorProvider.CreateCustomDrawer(NeuroObjectInspector inspector, ObjectInspector.Data data)
        {
            if (data.type == typeof(int2))
            {
                return ObjectInspectorFields.CreateDrawer<Vector2Int, int2>(data, new Vector2IntField(), 
                    (c) => new Vector2Int(c.x, c.y),
                    vector2 => new int2(vector2.x, vector2.y));
            }
            if (data.type == typeof(int3))
            ...etc...
                
            return null;
        }
    }
```


# Saving neuro reference changes in editor scripts 
For example, maybe you got some scripts to modify some data in editor.
```
// grab the item we want to modify... really same as using NeuroDataProvider.SharedReferences here.
var itemToModify = NeuroDataProvider.GetSharedTable<MyFirstNeuroObject>("myItem");

// do the modification
itemToModify.MyFirstString = "My modified string";

// save it back out to neuro json file.
NeuroEditorDataProvider.Shared.SaveData(itemToModify);
```


# Preprocessing / stripping data for build
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
                obj.MyFirstString = null; // strip the string data...
            }
            return true; // < If you return false here, the object will not be included in the build.
        }
    }
```

# Visit every values of a neuro object

Example:
```
var vistor = new NeuroVisitor();
var refs = NeuroDataProvider.SharedReferences;
vistor.Visit(myObjToVisit, new MyCustomVisitor(refs));

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


# Reserve / Deprecate tags
```
public class MyObjectWithOldFields
{
    [ReservedNeuroTag(1)]
    [ReservedNeuroTag(2)]
    
    [Neuro(3)] public int MyValue;
}
```

When changing a type of an existing field, it is recommended to also change the tag number. 

This ensures it keeps the backward compatibility to old saved data.

If you are in early stage of development, it might be ok to reuse the tags and just wipe the data to keep the tag numbers tidy.




# Non-static / multi reference configs support

By default, references are resolved via a static look up in Unity.

It is the most convenient method for normal usage in Unity but if you ever need to run the multiple configs in a multithreaded environment... you can...

### Completely disable static resolving

Add compiler argument `NEURO_DISABLE_STATIC_REFERENCES`

From now on, you can not call GetValue(), you must always pass in the reference `GetValue(NeuroReferences references)`

### Thread static resolving

Add compiler argument `NEURO_THREAD_STATIC_STATIC_REFERENCES`

From now on, you can manually set your own reference root per thread via `NeuroReferences.Default`. 
You can keep using `GetValue()` and it'll resolve differently per thread.

### Additional Notes
In both cases, you will also need to stop using `NeuroDataProvider.GetSharedTable<T>()` - because that is a Unity convenience.
Instead call via `NeuroReferences.Default.GetTable<T>()`


# Selective assembly scanning for faster compile time
By default Neuro will scan all assemblies for Neuro objects, this may be slow is a large project.   
Turn on selective assemblies mode to only scan certain assemblies.
1. In all the assemblies where you define Neuro types, add `[assembly:Neuro(0)]` - not the assemblies you use Neuro, just the places you define Neuro objects with `[Neuro(123)]`
2. In Unity Project Settings > Player > Scripting Define Symbols, add NEURO_SELECTIVE_ASSEMBLIES and apply.


# What's next ?

[Backward Compatibility >](BackwardCompatibility.md)

[Editor Customisation >](EditorCustomisation.md)
