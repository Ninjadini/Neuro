# Neuro — condensed reference for AI coding assistants

> This folder is written for coding agents (Claude Code, Cursor, Copilot, Codex, Aider, …). It is the
> whole Neuro API surface in ~25KB so an agent does not have to read the library source. Humans should
> start at [../GettingStarted.md](../GettingStarted.md) instead.

C# binary + JSON serializer with a Unity data-authoring layer. Protobuf-like tagged fields, no schema
files, no reflection at runtime — a Roslyn source generator emits the serialisation code from your
attributed types. Namespace `Ninjadini.Neuro`.

Three things it does, which most projects use together:
1. **Serialisation** — `[Neuro(#)]` tagged types <-> compact binary or JSON.
2. **Referencable config** — authored data items with a `RefId`, edited in a Unity window, stored as
   JSON files, linked to each other with `Reference<T>`. This is the ScriptableObject replacement.
3. **Player saves / network payloads** — the same types, written as binary.

## Defining data

```csharp
using Ninjadini.Neuro;

[NeuroGlobalType(1)]                       // required on every root IReferencable; unique across the whole project
public class Troop : Referencable          // Referencable gives you RefId (uint) + RefName (string)
{
    [Neuro(1)] public string DisplayName;  // tag unique within this class, never reused after removal
    [Neuro(2)] public int Health;
    [Neuro(3)] public Reference<Troop> Upgrade;   // link to another item
    [Neuro(4)] public List<Weapon> Weapons;
}

public class Weapon                        // plain nested neuro object - no global type needed
{
    [Neuro(1)] public float Damage = 1f;   // field initialisers work as defaults
}
```

Hard rules the generator enforces (full diagnostic list in [data-model.md](data-model.md)):
- Tag numbers are the wire format. Unique per class, never reuse a removed one — mark it
  `[ReservedNeuroTag(#)]` instead. Change a field's type => change its tag.
- Whole numbers must be `int`/`uint`/`long`/`ulong` — `byte`, `short`, `char`, `decimal` are rejected
  (varint means narrow types save nothing). Enums backed by those are fine.
- Collections: `List<T>` and `Dictionary<K,V>` only. No arrays, no `HashSet`, no `IReadOnlyList`.
  Dictionary keys must be string, struct or enum.
- A class with `[Neuro]` **private** fields must be `partial`.
- Subclasses of a neuro class each need their own class-level `[Neuro(#)]`, unique among siblings.

## Reading config at runtime (Unity)

```csharp
var table  = NeuroDataProvider.GetSharedTable<Troop>();
var troop  = table.Get(42u);           // by RefId
var byName = table.Get("goblin");      // by RefName
foreach (var t in table.SelectAll()) { }   // loads everything

var settings = NeuroDataProvider.GetSharedSingleton<GameSettings>();   // for ISingletonReferencable
var target   = troop.Upgrade.GetValue();   // resolve a Reference<T>
```

Outside Unity, or with static resolution disabled: `NeuroReferences.Default.GetTable<T>()` and
`reference.GetValue(references)`.

## Serialising

```csharp
byte[] bytes = NeuroBytesWriter.Shared.Write(obj).ToArray();   // .ToArray() - see gotcha below
var  back    = NeuroBytesReader.Shared.Read<MyData>(bytes);

string json  = NeuroJsonWriter.Shared.Write(obj);
var  fromJs  = NeuroJsonReader.Shared.Read<MyData>(json);

var copy     = NeuroBytesWriter.Clone(obj);
```

**Gotcha:** `Write()` returns a `ReadOnlySpan<byte>` over the writer's own reusable buffer. The next
`Write()` on the same writer overwrites it in place, silently. Call `.ToArray()` unless you consume it
first. Same for `GetCurrentBytesChunk()`.

The generic parameter is a hint only — the runtime type is what gets written, so writing through a base
class and reading back as the subclass works. Top-level value must be a neuro object; a bare `int`,
`string` or `List<>` throws. Empty/`null`/`"null"` input reads back as `null` rather than throwing.

Use `WriteGlobalTyped`/`ReadGlobalTyped` only when the reader cannot know the type — it embeds the
`[NeuroGlobalType]` id and is **not** interchangeable with the plain calls.

## The rest of this folder

| Topic | File |
|---|---|
| Supported types, attributes, polymorphism, back-compat rules, every codegen diagnostic | [data-model.md](data-model.md) |
| Read/write call matrix, JSON shape, custom type registration, visitors, pooling, no-static-refs builds, fast codegen | [serialization.md](serialization.md) |
| Neuro Editor window, NeuroData files, base36 RefIds, player saves, AssetAddress, validators, build stripping, editor drawers | [unity.md](unity.md) |

Prose docs for humans live one folder up: [GettingStarted](../GettingStarted.md),
[AdvancedUsages](../AdvancedUsages.md), [EditorCustomisation](../EditorCustomisation.md),
[BackwardCompatibility](../BackwardCompatibility.md).
