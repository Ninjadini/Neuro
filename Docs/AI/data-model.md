# Neuro data model

## Attributes

| Attribute | Target | Meaning |
|---|---|---|
| `[Neuro(uint tag)]` | field | Serialised field. Tag unique within the declaring class. |
| `[Neuro(uint tag)]` | class / struct / interface | Marks a polymorphic subtype (tag unique among all subtypes of the same root) or, on a root with no fields, declares the root. |
| `[NeuroGlobalType(uint id)]` | class / interface | Globally unique type id. **Required** on every root `IReferencable`. Also what makes `WriteGlobalTyped`/`ReadGlobalTyped` possible. |
| `[ReservedNeuroTag(uint tag)]` | class | Tombstones a retired tag so it can never be reused. Repeatable. |
| `[assembly: Neuro]` | assembly | Opts the assembly in under `NEURO_FAST_CODEGEN`. |
| `[LinkedReference(Type to, string toName, string fromName, bool optional)]` | class | Declares that this referencable is paired with an item of another type sharing the same RefId. Resolve with `references.GetLinkedRef<T>(item)`. |

Unity-editor-only presentation attributes: `[DisplayName]`, `[Tooltip]` / `[Description]`, `[Header]`
(prefix the text with `"> "` for a foldout), `[InspectorStyle(spaceBefore, spaceAfter, horizontal)]`,
`[AssetType(typeof(Sprite))]`.

## Supported field types

**Primitives:** `bool`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `string`, any `enum`.

**Built-in structs:** `DateTime` (ms precision, Kind preserved), `DateTimeOffset` (ms + offset),
`TimeSpan` (ms), `Guid`, `Uri` (round-trips `OriginalString`), `Version` (unset Build/Revision stay unset).

**Composites:** nested neuro classes and structs, `List<T>`, `Dictionary<K,V>`, `T?` for any struct,
`Reference<T>`, `AssetAddress` (Unity).

**Unity types** (registered automatically by `NeuroDefaultUnityTypesHook`): `Vector2/3/4`,
`Vector2Int`, `Vector3Int`, `Quaternion`, `Matrix4x4`, `Color`, `Color32`, `Gradient`,
`GradientColorKey`, `GradientAlphaKey`, `AnimationCurve`, `Keyframe`, `Rect`, `RectInt`, `RectOffset`,
`Bounds`, `BoundsInt`, `BoundingSphere`, `Plane`, `Ray`, `Ray2D`, `RangeInt`, `LayerMask`, `Hash128`.

**Rejected**, with a compile error rather than a runtime surprise: arrays, `HashSet`, `IReadOnlyList`,
any other generic; `byte`, `sbyte`, `short`, `ushort`, `char`, `decimal`; dictionary keys that are not
string/struct/enum; generic type arguments that are themselves generic (except `Reference<>`).

For `byte`/`short` use `int` - values are varint encoded, so a narrow type saves nothing. For `char` use
`string`. For `decimal` use `double`, or a `long` of scaled units.

## Referencable items

```csharp
public interface IReferencable { uint RefId { get; set; } string RefName { get; set; } }
public abstract class Referencable : IReferencable { }        // the usual base class
public interface ISingletonReferencable : IReferencable { }   // exactly one item; RefId is fixed at 1
```

`RefId` is a `uint`, unique per root type, and is what links are stored as. `RefName` is a
human-readable label and need not be unique. Newly created ids are random rather than sequential so two
branches do not collide on merge.

`Reference<T>` is a struct wrapping just the `RefId`. It implicitly converts to/from `uint` and from `T`,
and compares by id. `HasRefId` / `HasNoRefId` test for unset. Resolve with `GetValue()`,
`GetValue(NeuroReferences)` or `GetValue(NeuroReferenceTable<T>)`.

## Polymorphism

```csharp
public class BaseEntity                     // root: has fields, so no class attribute needed
{
    [Neuro(1)] public string Name;
}

[Neuro(1)] public class Vehicle : BaseEntity { [Neuro(1)] public float Speed; }
[Neuro(2)] public class Character : BaseEntity { }
[Neuro(3)] public class Player : Character { }   // still needs its own tag, unique across the whole tree
```

A root with **no** neuro fields must declare itself: `[Neuro(1)]` on a class (non-zero),
`[Neuro(0)]` on an interface (zero is allowed there, but converting that interface to a class later
breaks compatibility).

Field tags restart at 1 in each class in the chain - they are scoped to the declaring class. Subtype
tags are scoped to the whole inheritance tree. Multiple inheritance paths (two neuro roots) are not
supported.

## Backwards compatibility

Follow protobuf's rules.

- **Safe:** adding fields; removing fields; removing classes; renaming fields and classes - though a
  rename loses the data in JSON, which is keyed by name (binary is keyed by tag).
- **Breaking:** reusing a retired tag; changing a field's type without changing its tag; reusing a
  retired subtype tag; restructuring a polymorphic hierarchy.
- Reading data with unknown tags in it is fine - they are skipped.

## Codegen errors

Violations of the rules above are compile errors prefixed `Neuro` (`Neuro022`, `Neuro101`, `Neuro102`,
`Neuro300`, `Neuro303`, `Neuro312`, `Neuro404`, `Neuro405`, `Neuro406`, ...). The messages state the
cause and the fix, so read the error text rather than guessing from the code alone. Every descriptor is
declared in one place if you need the full list: `Development~/Ninjadini.Neuro.CodeGen/NeuroSourceAnalyzer.cs`.
