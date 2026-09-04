# Neuro — condensed reference for AI coding assistants

> For coding agents. Humans start at [../GettingStarted.md](../GettingStarted.md).

C# binary + JSON serializer plus a Unity data-authoring layer, namespace `Ninjadini.Neuro`.
Protobuf-like tagged fields, no schema files, no runtime reflection — a Roslyn generator emits the
serialisation code from attributed types, so most mistakes are compile errors. Used for three jobs at
once: **serialisation** (`[Neuro(#)]` types <-> binary/JSON), **referencable config** (authored items
with a `RefId`, edited in a Unity window, stored as JSON, linked by `Reference<T>` — the
ScriptableObject replacement), and **player saves / network payloads**.

## Defining data

```csharp
[NeuroGlobalType(1)]                       // required on every root IReferencable, unique project-wide
public class Troop : Referencable          // Referencable supplies RefId (uint) + RefName (string)
{
    [Neuro(1)] public string DisplayName;
    [Neuro(2)] public int Health;
    [Neuro(3)] public Reference<Troop> Upgrade;    // link to another item
    [Neuro(4)] public List<Weapon> Weapons;
}

public class Weapon                        // nested neuro object, no global type needed
{
    [Neuro(1)] public float Damage = 1f;   // field initialisers act as defaults
}
```

Rules the generator enforces — breaking these is a compile error, except the first, which silently
corrupts old data (details in [data-model.md](data-model.md)):

- **Tags are the wire format.** Unique per class; never reuse a removed one (`[ReservedNeuroTag(#)]`
  tombstones it); changing a field's type means changing its tag. To find a free one, write `0` and read
  the compile error - it lists the used tags and the next free number. Never scan the codebase for this.
- Whole numbers must be `int`/`uint`/`long`/`ulong`. `byte`, `short`, `char`, `decimal` are rejected —
  varint encoding means narrow types save nothing. Enums backed by them are fine.
- `List<T>` and `Dictionary<K,V>` only — no arrays, `HashSet` or `IReadOnlyList`. Dictionary keys must
  be string, struct or enum.
- A class with `[Neuro]` **private** fields must be `partial`.
- Each subclass of a neuro type needs its own class-level `[Neuro(#)]`, unique among siblings.

## Reading config at runtime (Unity)

```csharp
var table = NeuroDataProvider.GetSharedTable<Troop>();
table.Get(42u);                     // by RefId
table.Get("goblin");                // by RefName
table.SelectAll();                  // forces everything to load
NeuroDataProvider.GetSharedSingleton<GameSettings>();   // ISingletonReferencable
troop.Upgrade.GetValue();           // resolve a Reference<T>
```

Outside Unity, or with static resolution disabled: `NeuroReferences.Default.GetTable<T>()` and
`reference.GetValue(references)`.

## Serialising

```csharp
byte[] bytes = NeuroBytesWriter.Shared.Write(obj).ToArray();   // .ToArray() — see below
var    back  = NeuroBytesReader.Shared.Read<MyData>(bytes);
string json  = NeuroJsonWriter.Shared.Write(obj);
var    fromJ = NeuroJsonReader.Shared.Read<MyData>(json);
var    copy  = NeuroBytesWriter.Clone(obj);
```

**Gotcha:** `Write()` returns a span over the writer's own reusable buffer; the next `Write()` on that
writer overwrites it in place, silently. `.ToArray()` unless you consume it first. Same for
`GetCurrentBytesChunk()`.

The generic parameter is only a hint — the runtime type is written, so writing through a base class and
reading back as the subclass works. The top-level value must be a neuro object (a bare `int`, `string`
or `List<>` throws). Empty, `null` and `"null"` input read back as `null`. `WriteGlobalTyped` /
`ReadGlobalTyped` are for when the reader cannot know the type; they embed the `[NeuroGlobalType]` id
and are **not** interchangeable with the plain calls.

## Rest of this folder

| Read when you need | File |
|---|---|
| Attributes, supported types, polymorphism, back-compat rules, picking a free tag | [data-model.md](data-model.md) |
| Read/write call matrix, JSON shape, registering third-party types, visitors, pooling, compile defines | [serialization.md](serialization.md) |
| Neuro Editor, NeuroData files, base36 RefIds, player saves, AssetAddress, validators, build stripping | [unity.md](unity.md) |

Human prose docs are one folder up: GettingStarted, AdvancedUsages, EditorTools,
EditorCustomisation, BackwardCompatibility.
