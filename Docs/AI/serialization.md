# Neuro serialisation

## The six read/write calls

Binary (`NeuroBytesWriter` / `NeuroBytesReader`) and JSON (`NeuroJsonWriter` / `NeuroJsonReader`) expose
the same three pairs. Each has a `.Shared` singleton instance; construct your own for thread isolation.

| | Write | Read | When |
|---|---|---|---|
| Generic | `Write(value)` | `Read<T>(data)` | Type known at compile time. Almost always this. |
| Runtime type | `WriteObject(value)` | `ReadObject(data, type)` | You only hold a `System.Type`. Identical data format, one reflection lookup per type. |
| Global type | `WriteGlobalTyped(value)` | `ReadGlobalTyped(data)` | The reader cannot know the type. Embeds the `[NeuroGlobalType]` id in the data. **Not** interchangeable with the other two. |

Binary writers return `ReadOnlySpan<byte>`; readers accept a `BytesChunk`, which `byte[]` implicitly
converts to. JSON writers return `string`; `WriteTo`/`WriteObjectTo`/`WriteGlobalTypedTo` append into a
`StringBuilder` you supply instead.

```csharp
byte[] bytes = NeuroBytesWriter.Shared.Write(obj).ToArray();
var    back  = NeuroBytesReader.Shared.Read<MyData>(bytes);
var    copy  = NeuroBytesWriter.Clone(obj);               // static; round-trips through binary

string json  = NeuroJsonWriter.Shared.Write(obj);
var    obj2  = NeuroJsonReader.Shared.Read<MyData>(json);
```

### Buffer reuse - the one real footgun

`Write()` hands back a span over the writer's own buffer, which the next `Write()` on that writer
overwrites in place with no error:

```csharp
var a = NeuroBytesWriter.Shared.Write(objA);   // span into the shared buffer
var b = NeuroBytesWriter.Shared.Write(objB);   // `a` now reads as objB's bytes
```

`.ToArray()` before writing anything else, unless you are finished with the span by then. Same for
`GetCurrentBytesChunk()`.

### Semantics shared by all six

- **Subclasses:** the generic parameter is a hint; the object's runtime type is written. Write through a
  base class or interface, read back as the subclass. Reading as an unrelated type throws.
- **Top level must be a neuro object.** A bare `int`, `string` or `List<>` has no header to read back
  with, and throws. Wrap it in a field of a neuro object.
- **Empty input reads as null.** Empty bytes, and `null` / `""` / `"null"` json, all give `null` back -
  mirroring what the writers emit for a null value.
- Reading into an existing instance: the `Read`/`ReadObject` overloads taking `ref result` reuse it.

## JSON specifics

Reserved field names: `-globalType` (from `WriteGlobalTyped`) and `-subType` (polymorphic tag).

`NeuroJsonWriter.Options`: `TagValuesOnly` omits the human-readable ref/enum name suffixes;
`ExcludeTopLevelGlobalType` skips the `-globalType` field.

References and enums are written as `"field": "2:mySecondItem"`. **Only the number matters** on read -
`"field": 2` is equally valid, and renaming an item does not unlink anything. The name is there for
humans reading the file.

JSON is keyed by field *name*, binary by *tag*. So renaming a field is binary-safe but loses the value
in JSON-stored data.

## Registering types you do not own

For third-party or engine types you cannot attribute. Implement `INeuroCustomTypesRegistryHook` - the
generator finds implementations automatically, with no attribute needed.

```csharp
public struct MathematicsTypeHooks : INeuroCustomTypesRegistryHook
{
    public void Register()
    {
        if (NeuroSyncTypes.IsEmpty<int2>())
        {
            NeuroSyncTypes.Register((INeuroSync neuro, ref int2 value) =>
            {
                neuro.Sync(1, nameof(value.x), ref value.x);   // number = binary tag, string = json name
                neuro.Sync(2, nameof(value.y), ref value.y);
            });
            // tell the Neuro editor which members to draw
            NeuroSyncEditorFields.AddField(typeof(int2), nameof(int2.x));
            NeuroSyncEditorFields.AddField(typeof(int2), nameof(int2.y));
        }
    }
}
```

`NeuroSyncEditorFields.AddField` / `.AddProperty` are `[Conditional("UNITY_EDITOR")]`, so both the calls
and their arguments vanish from player builds. `NeuroSyncTypes.Register` also takes an explicit
`FieldSizeType` (`VarInt`, `Fixed32`, `Fixed64`, `Length`, `Child`) for single-value types, and
`RegisterEqualityCheck<T>` customises default-value comparison. `RegisterSubClass<TBase,TSub>(tag, d)`
registers polymorphic subtypes manually.

`Ninjadini.Neuro/Sync/NeuroDefaultSyncTypes.cs` and
`Ninjadini.Neuro.Unity/RunTime/NeuroDefaultUnityTypesHook.cs` are the worked examples.

## Visitors

`NeuroVisitor` walks every neuro field of an object - read only; lists, dictionaries and nullables are
handed out as copies, so writes there are dropped.

```csharp
new NeuroVisitor().Visit(obj, myVisitor, visitPrimitiveValues: false);

public class MyVisitor : NeuroVisitor.IInterface
{
    public void BeginVisit<T>(ref T obj, string name, int? listIndex) { }
    public void EndVisit() { }
    public void VisitRef<T>(ref Reference<T> reference) where T : class, IReferencable { }
}
```

`NeuroEditVisitor` is the same walk but writes back what the visitor changed - use it when mutating is
the point, e.g. repointing every `Reference<>` from one RefId to another.
`NeuroVisitor.GeneratePathFromStack(stack)` builds a readable path for error messages.
`NeuroHashGenerator.Shared.Generate(obj)` gives a content hash of a neuro object.

## Pooling / zero allocation

Both formats allocate nothing beyond the objects they return, once buffers are warm. To also avoid
those, pass a pool:

```csharp
var opts = new ReaderOptions(myPool);              // myPool : INeuroObjectPool
var obj  = NeuroBytesReader.Shared.Read<MyData>(bytes, opts);
...
NeuroPoolCollector.Shared.ReturnAllToPool(obj, myPool);   // walks the graph, returns everything
```

`INeuroObjectPool` is `T Borrow<T>()` + `void Return(object)`. `NeuroPoolCollector.BasicPool` is a
usable default implementation.

## Compile-time switches

| Define | Effect |
|---|---|
| `NEURO_FAST_CODEGEN` | Only types with a class-level `[Neuro(#)]`/`[NeuroGlobalType(#)]` are considered, in assemblies marked `[assembly: Neuro]`. Much faster compiles in large projects; missing attributes become `Neuro406` errors rather than silent runtime failures. (`NEURO_SELECTIVE_ASSEMBLIES` is the old name and still works.) |
| `NEURO_DISABLE_STATIC_REFERENCES` | No static reference resolution. `GetValue()` is gone; you must pass `GetValue(references)`. |
| `NEURO_THREAD_STATIC_STATIC_REFERENCES` | `NeuroReferences.Default` becomes `[ThreadStatic]`; `GetValue()` resolves per thread. |

Under the last two, stop using `NeuroDataProvider.GetSharedTable<T>()` (a Unity convenience) and use
`NeuroReferences.Default.GetTable<T>()`.

Note: the fast-codegen scan reads source text, so it does not follow `using` aliases -
`using N = Ninjadini.Neuro.NeuroAttribute;` then `[N(1)]` will not be seen.
