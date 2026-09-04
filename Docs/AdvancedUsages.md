### Read write binary data
```
// Write a neuro object to byte array
var bytes = NeuroBytesWriter.Shared.Write(myData).ToArray();

// Read byte array to neuro object
var myReadData = NeuroBytesReader.Shared.Read<MyData>(bytes);
```
> [!WARNING]
> `Write()` returns a span over the writer's own buffer, which the next `Write()` on the same writer overwrites
> in place - a span you are still holding silently turns into the other object's bytes:
> ```
> var a = NeuroBytesWriter.Shared.Write(objA);   // span into the shared buffer
> var b = NeuroBytesWriter.Shared.Write(objB);   // a now reads as objB's bytes, no error
> ```
> So call `.ToArray()` on the result before writing anything else, unless you are done with it by then.
> The same goes for `GetCurrentBytesChunk()`.

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

### Which read / write call do I use?
Both the binary and JSON reader/writer have the same three pairs. They differ only in how the type is worked out on the reading side.

| | Write | Read | Use when |
|---|---|---|---|
| **Generic** | `Write(value)` | `Read<MyType>(data)` | You know the type at compile time. Almost always this one. |
| **Runtime type** | `WriteObject(value)` | `ReadObject(data, type)` | You only have a `System.Type`. Same data format as above, just a bit slower - it uses reflection once per type. |
| **Global type** | `WriteGlobalTyped(value)` | `ReadGlobalTyped(data)` | The reading side has no idea what to expect. The type id is stored in the data, so the type needs a `[NeuroGlobalType]` attribute. Not interchangeable with the other two. |

#### Sub classes
The generic parameter is only a hint - what gets written is the object's actual runtime type.
So you can write through a base class or interface and read it back as the sub class:
```
Animal animal = new Dog() { Barks = 7 };

var bytes = NeuroBytesWriter.Shared.Write(animal).ToArray();   // writes it as a Dog
var result = NeuroBytesReader.Shared.Read<Animal>(bytes);      // result is a Dog
```
You can read as any base type of the written object. Reading as an unrelated type (`Read<Cat>` above) throws.

#### Empty input
Reading nothing gives you back nothing rather than an exception - empty bytes, and `null` / `""` / `"null"` json,
all read back as `null` on all three read calls. That mirrors the writers, which emit exactly those for a null value.

#### Top level types
`Write` / `Read` need a neuro object - a class or struct with `[Neuro]` fields.
A bare `int`, `string`, `List<>` etc has no header of its own so there would be nothing to read it back with; those calls throw.
Put the value in a field of a neuro object and write that instead.
> [!TIP]
> JSON prints references and enums as `"myItem": "4zbc:mySecondItemName"` - only the part before the `:` is
> read, the name is there for you. See
> [RefName is only there to read](GettingStarted.md#the-refname-next-to-a-refid-is-only-there-to-read).
>
> `NeuroJsonWriter.Options.TagValuesOnly` drops the name suffixes if you want the shorter output.

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
See full example of Unity ones in this class: [NeuroDefaultUnityTypesHook.cs](../Ninjadini.Neuro.Unity/RunTime/NeuroDefaultUnityTypesHook.cs)

Short example using Unity.Mathematics.int2:
```
    // This is auto picked up by code gen to be registered because it extends from INeuroCustomTypesRegistryHook
    public struct NeuroMathematicsTypeHooks : INeuroCustomTypesRegistryHook
    {
        public void Register()
        {
            if (NeuroSyncTypes.IsEmpty<int2>())
            {
                NeuroSyncTypes.Register((INeuroSync neuro, ref int2 value) =>
                {
                    neuro.Sync(1, nameof(value.x), ref value.x);
                    neuro.Sync(2, nameof(value.y), ref value.y);
                });
                // number is used for binary, name string is used for json, ref value is used for actual data read/write.

                // Tell the Neuro editor which members to draw for this type.
                // Use AddProperty(...) instead if the member is a property rather than a field.
                NeuroSyncEditorFields.AddField(typeof(int2), nameof(int2.x));
                NeuroSyncEditorFields.AddField(typeof(int2), nameof(int2.y));
            }
            
            NeuroSyncTypes.Register((INeuroSync neuro, ref int3 value) => 
            ...etc...
        }
    }
```
The `NeuroSyncTypes.Register(...)` call teaches Neuro how to serialise the type.   
The `NeuroSyncEditorFields.AddField` / `AddProperty` calls then tell the Neuro editor which instance members of the type to draw in the inspector — without these, the editor wouldn't know which fields of a non-`[Neuro]` type to render.   
As long as those members have drawable types (primitives, Unity types with built-in editors, other Neuro types), no custom drawer code is required.

> [!TIP]
> `NeuroSyncEditorFields.AddField` / `AddProperty` are marked `[Conditional("UNITY_EDITOR")]`, so the calls (and their argument expressions) are stripped from player builds at compile time — there's no runtime cost outside the editor, which is why it's safe to put them right next to your serialisation registration.

#### When you need more control over how it's drawn

If per-field rendering isn't enough for a particular type (e.g. you'd rather draw `int2` as a single combined `Vector2IntField` widget), skip the `NeuroSyncEditorFields` calls for that type and register a custom drawer via `ICustomNeuroEditorProvider` instead:

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


# Object pooling / zero allocations

Once the buffers are warm, Neuro allocates nothing except the objects it hands back to you.
To avoid those too, read with a pool and give the objects back when you are done with them.

```
var options = new ReaderOptions(myPool); // myPool : INeuroObjectPool
var obj = NeuroBytesReader.Shared.Read<MyData>(bytes, options);

// ... later, walks the whole object graph and returns every neuro object in it
NeuroPoolCollector.Shared.ReturnAllToPool(obj, myPool);
```

`INeuroObjectPool` is just `T Borrow<T>()` and `void Return(object)`.
`NeuroPoolCollector.BasicPool` is a usable default if you don't want to write one.

Only worth it for data you read repeatedly - network messages, replay frames. Config data is read once.


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

Reserved tags count as taken everywhere Neuro reports tag usage - the tag map at the top of the
generated `NeuroTypesRegister` file, and the conflict errors - so the "next free" number it gives you
will always skip past them. See [BackwardCompatibility.md](BackwardCompatibility.md) for what that
looks like.




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


# Fast code gen for faster compile time
By default Neuro looks at every type in every assembly to work out what to generate, which may be slow in a
large project. Fast code gen mode narrows that down so it can tell from the source text alone whether a type
is worth looking at properly.

1. In all the assemblies where you define Neuro types, add `[assembly:Neuro]` - not the assemblies where you
   *use* Neuro, just the places you define Neuro objects with `[Neuro(123)]`
2. In Unity Project Settings > Player > Scripting Define Symbols, add `NEURO_FAST_CODEGEN` and apply.

### The extra rule
Normally a type takes part in Neuro if the class *or any of its fields* is attributed. Under fast code gen only
the class level attribute counts:

```csharp
// Fine, the class says what it is.
[Neuro(1)]
public partial class Item
{
    [Neuro(1)] public int Id;
}

// Error Neuro406 - the fields are attributed but the class never opted in.
public partial class Item
{
    [Neuro(1)] public int Id;
}
```

`[NeuroGlobalType(#)]` counts as opting in too, and `INeuroCustomTypesRegistryHook` implementations are still
found without any attribute. A subclass of a Neuro class still has to carry its own `[Neuro(#)]`, same as always.

If you forget one you get a compile error pointing at the type, not a silent "type is not registered" at runtime.

### Notes
- `NEURO_SELECTIVE_ASSEMBLIES`, the old name for this define, still works.
- The check that decides whether to look at a type reads the source text, so it does not follow `using` aliases.
  Writing `using N = Ninjadini.Neuro.NeuroAttribute;` and then `[N(1)]` will not be picked up in this mode.


# What's next ?

[Backward Compatibility >](BackwardCompatibility.md)

[Editor Tools & Settings >](EditorTools.md)

[Editor Customisation >](EditorCustomisation.md)
