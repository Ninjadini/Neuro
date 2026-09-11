# Neuro in Unity

Package `com.ninjadini.neuro-unity`, Unity 2022.2+. Two assemblies: `Ninjadini.Neuro` (pure C#,
runtime) and `Ninjadini.Neuro.Unity` (runtime + editor integration).

## Authoring config data

`Tools > Neuro > ❖ Editor` (or `Window > ❖ Neuro Editor`) lists every root `IReferencable` type and lets
you add, edit, duplicate and delete items. Each item is one JSON file on disk.

Files live under the project's `NeuroData/` folder by default, configurable in
`Project Settings > Ninjadini ❖ Neuro`. The layout is
`NeuroData/<globalTypeId>-<TypeName>/<refId>-<ref_name>.json`, e.g. `NeuroData/1-Troop/4zbc-goblin.json`.
`⊙ File` in the editor reveals the file for the selected item.

Edits made in the editor - field changes, RefName and RefId changes, add, clone, delete - go on Unity's
undo stack, so Ctrl+Z / Ctrl+Y and `Edit > Undo` work on them and are saved to disk like any other edit
(`Undo Redo Enabled` in project settings, default on). Implemented in `NeuroEditorUndoRedos`: one hidden
ScriptableObject whose serialized state is a json snapshot of the one item that changed, registered with
`Undo.RegisterCompleteObjectUndo`; `Undo.undoRedoPerformed` writes whichever state lands back into the
data via `NeuroEditorDataProvider.ApplyJson` / `AddAtPath` / `Delete`. Only edits that go through the
editor are recorded - a script calling `SaveData` is not - so a custom drawer that changes an item
without the editor's own value-changed path should call `NeuroEditorUndoRedos.RecordChange(dataFile,
"Edit")` after the change.

Other menu items:

| Menu | Does |
|---|---|
| `Tools > Neuro > Content Debugger` | Inspect/round-trip arbitrary neuro data, including runtime saves. |
| `Tools > Neuro > Type Mapping Debugger` | Shows every registered type, its global id and subtype tags. The only view spanning all assemblies - the compile-time tag reports cover one assembly each. |
| `Tools > Neuro > Reload` / `Reload + Read all data` | Re-read the JSON files after external edits. |
| `Tools > Neuro > Migrate Renamed Field...` | Moves a value from an old json field name onto the renamed one - see below. |
| `Tools > Neuro > Migrate RefIds to base36...` | One-time migration for pre-base36 data. |

Also under that menu: `Save Data To Resources` / `Save Resources data as JSON` (bake and dump the binary
blob builds use) and `Bake AutoTypesRegister Script` (static type registry, skips assembly scanning).

## Data files changing on disk

Data paths are watched, so edits made outside the editor are picked up. With `Auto Reload Changed Data
Files` on (project settings, the default), a changed file is re-read into the object already loaded, so
anything holding the item sees the new values rather than a stale copy.

Changes that need a full `Reload()` instead - a file added, deleted or renamed, or the setting being off -
stay pending in `HasPendingFileChanges`, and entering play mode reloads everything. Subscribe to
`NeuroEditorDataProvider.DataFileReloaded` if your editor UI has to redraw when an item is re-read.

## RefIds are base36 in files and JSON

A `RefId` is always a `uint` in memory and in binary. In **file names and JSON** it is spelled in base36
(`0-9a-z`), which keeps a generated id at 4 characters and 3 bytes. Generated ids fall in
`NeuroRefId.GeneratedMinValue`..`GeneratedMaxValue` (46656..1679615) so they are always exactly 4 chars,
and are random rather than sequential so branches do not collide.

File names and JSON ref values print the id and the RefName together (`4zbc-my_item.json`,
`"myItem": "4zbc:my_item"`); only the id resolves, everything after the `-` / `:` is ignored on load.

Watch out: `20` in a file name is base36, so it is the number **72**, not 20. Every id has exactly one
spelling. Hover the `RefId` field in the editor to see the plain number, or turn on
`Show Raw Ref Id Numbers` in project settings to see `1v83 (87123)` everywhere. Conversions:
`NeuroRefId.ToString(id)`, `NeuroRefId.Parse(chars)`, `NeuroRefId.TryParse(...)`.

**Changing an item's RefId:** type the new id into the editor's `RefId` field. Neuro checks the id is
free, repoints every `Reference<>` in the data, renames the file and saves everything it touched. Undo
moves the id back and repoints the other items again.

The RefId box in `NeuroEditorItemElement` is the one path that moves an id (`TryChangeRefId`). If a custom
drawer writes `RefId` on the drawn object directly, the item element notices on the next value change,
puts the old id back and routes the request through that same confirm-and-rename flow, so the table,
the file name and undo never disagree with the object. `NeuroObjectInspector` itself has no id guard.

The confirmation offers `Change & update assets` as well as `Change data only`. The asset sweep walks
every prefab, ScriptableObject and scene under `Assets/` for `Reference<>` fields holding the old id and
repoints those too, saving what it changed. It runs after the data change is committed, is **not**
undoable, and skips scenes in play mode or when the open scenes have unsaved changes the user declines to
save - whatever it skipped is reported. Ids stored anywhere else (save games, hard-coded constants) are
still on you.

```csharp
// the same sweep on its own, e.g. from a migration script
NeuroAssetRefIdRewriter.Rewrite(typeof(Troop), oldRefId, newRefId, includeScenes: true, dryRun: false);
// -> Result { Matches, ChangedFiles, Problems }
```

## Renaming a [Neuro] field

JSON is keyed by field name and binary by tag, so renaming a `[Neuro]` field costs nothing in binary but
leaves every NeuroData json holding the old key - an unknown field the reader drops. `Tools > Neuro >
Migrate Renamed Field...` renames the key in the data instead. Pick the class, pick the field's new name
from its `[Neuro]` fields, type what it used to be called, `Preview`, then `Migrate`.

Only the key is rewritten, at its exact character range, so formatting and field order in the file are
untouched. The files are walked with the C# types alongside them - each json object knows its type,
`-subType` included - so the rename is scoped to that one class and a same-named field on another class is
left alone. An object that already has the new name is skipped rather than ending up with both, and every
file is checked to still read back before it is written.

```csharp
new NeuroJsonFieldRenamer().Rename(typeof(TowerAttack), "Damage", "Dmg", dryRun: false);
// -> Result { Renamed, Skipped, Problems, ChangedFiles }, each match carrying the json path it was found at
```

`NeuroJsonFieldRenamer.GetNeuroFields(type)` lists a type's `[Neuro]` fields, base classes' included.

## Runtime access

```csharp
NeuroDataProvider.SharedReferences                  // the NeuroReferences root
NeuroDataProvider.GetSharedTable<Troop>()           // NeuroReferenceTable<Troop>
NeuroDataProvider.GetSharedTable<Troop>(42u)        // item by RefId
NeuroDataProvider.GetSharedTable<Troop>("goblin")   // item by RefName
NeuroDataProvider.GetSharedSingleton<GameSettings>()
NeuroDataProvider.GetShared(someReference)
```

Table API: `Get(uint)`, `Get(string)`, `SelectAll()`, `GetIds()`, `GetDictionary()`,
`GetNameToIdMap()`, `Count`, `IsLoaded(id)`, `IsAllLoaded()`. Items load lazily per id; `SelectAll()`
forces the lot.

In the editor, data is read from the JSON files. In builds it comes from the baked
`Resources/NeuroData.bytes` (path and whether to bake are project settings).
`NeuroDataProvider.Shared.LoadFromResAsync()` preloads it - the resource load is async and the
decompress+parse runs on a background thread; poll `NeuroDataProvider.Shared.LoadingAsync` for progress.

`Reference<T>` fields on a `MonoBehaviour` or `ScriptableObject` serialise and draw with a searchable
dropdown, so you can link config from scenes and prefabs.

## Saving player progress

```csharp
public class MyGameLogic : MonoBehaviour
{
    [SerializeField] LocalNeuroContinuousSave _gameSave;   // component on the same GameObject

    MySaveData Data => _gameSave.GetData<MySaveData>();    // creates it on first call
    void OnCoinEarned() { Data.Coins++; _gameSave.DelayedSave(2f); }
}
```

`LocalNeuroContinuousSave`: `GetData<T>()`, `SetData<T>()`, `Save()`, `DelayedSave(seconds)`,
`FileExists()`, `SetSaveFileName(name)`, `SetCustomCreationFunction<T>(func)`, `DeleteAndDispose()`,
static `GetSavePath(name)`. There is a non-MonoBehaviour generic form too:
`LocalNeuroContinuousSave<T>.CreateInPersistedData(fileName, createFunc)`.

Lower level: `LocalNeuroStorage` - `Save<T>(obj, name)`, `TryLoad<T>(name)`, `Delete(name)`,
`GetPath(name)`, over binary files in `Application.persistentDataPath`.

## Assets

Unity objects cannot be embedded in neuro data. Reference them by address instead - the asset must be
Addressable or in a `Resources` folder **to load at runtime**.

The address itself is not an Addressables key: `AssetAddressEditorUtils.GetAddress` writes the
`Resources` path for an asset under `Resources`, and otherwise the plain asset **GUID**
(`<guid>[SubName]` for a sub-asset). So in the editor an address resolves through the AssetDatabase
whether or not the asset is Addressable - `AssetAddressEditorUtils.LoadObjectFromAddress(address)`
is the synchronous editor-only load, which is what an editor tool that has to resolve an asset
during a repaint wants. `NeuroAssetAddressValidator` only checks `Resources.Load` or
`AssetDatabase.GUIDToAssetPath`, so content tests pass on a non-Addressable asset too, and the
missing Addressables entry surfaces only as a failed load in a build.

```csharp
[AssetType(typeof(Sprite))]                  // optional; filters the editor's picker
[Neuro(1)] public AssetAddress Icon;

var sprite = await obj.Icon.LoadAssetAsync<Sprite>();
obj.Icon.LoadAssetAsync<Sprite>(s => image.sprite = s);   // callback form
obj.Icon.LoadFromResources<Sprite>();                      // sync, Resources only
obj.Icon.LoadSceneAsync();
```

## Built-in Unity types

`NeuroDefaultUnityTypesHook` registers the common Unity structs so they need no attributes:
`Vector2/3/4`, `Vector2Int`, `Vector3Int`, `Quaternion`, `Matrix4x4`, `Color`, `Color32`,
`Gradient`, `AnimationCurve`, `Hash128`, `LayerMask`, `BoundingSphere`, `RangeInt`, `Plane`,
`Ray`, `Ray2D`, `RectOffset`. When `com.unity.mathematics` is in the project (built into the editor
from 6000.5, a registry package before that) it also covers `int2/3/4`, `uint2/3/4`, `float2/3/4`,
`bool2/3/4`, `quaternion` (json `x y z w`, like `Quaternion`) and `float4x4` (by column `c0..c3`). The
vectors draw as one row with their letters; the block is gated on the `NEURO_UNITY_MATHEMATICS` define
the runtime asmdef sets from that package. `double`/`half` vectors and other matrix sizes are not
registered - add them from your own hook if you author them.

Most write as an object (`"Pos": {"x": 1, "y": 2}`). **`Color` and `Color32` are hex strings** -
`"FFCC00"`, or `"FFCC0080"` when the alpha is not fully opaque - so hand-writing one as
`{"r": 0.5, ...}` fails the whole file's load. Reading also takes `RGB`/`RGBA`/`RRGGBB`/`RRGGBBAA`,
any case, optional `#`, plus the packed number they used to be. Those are told apart by json token
type (`NeuroJsonReader.CurrentValueIsString`), so `"281420"` is hex and `281420` is packed. Bad hex
reads as opaque black rather than throwing.

Binary stays packed: `Color` is `r | g<<12 | b<<24 | a<<36` scaled by 4095, `Color32` is
`r | g<<8 | b<<16 | a<<24`. **So json is 8 bits per channel where binary `Color` keeps 12.** `Color`
is LDR - channels clamp to 0..1, use a `Vector4` for HDR. `Gradient` stops use the `Color` codec.

Both directions are allocation free. Implemented in `NeuroDefaultUnityTypesHook.RegisterColorJson`
via `NeuroJsonSyncTypes.Register<T>`, which overrides only the json path.

Field initialisers work as defaults on these - `public Vector2Int Size = Vector2Int.one;` reads back
as `(1, 1)` from json that omits `Size`. The exception is a struct that does not implement
`IEquatable<>` of itself, which the defaulted `Sync` overload requires: `LayerMask`, `RangeInt`, `Ray`,
`Ray2D`, `BoundingSphere`, `Keyframe`, `GradientColorKey` and `GradientAlphaKey` read back as `default`
whatever the initialiser says, so an initialiser there is `Neuro025` - write the value into the data
instead. See [data-model.md](data-model.md#defaults) for what an
initialiser may be.

## Content validation

```csharp
public class TroopValidator : INeuroContentValidator<Troop>
{
    public void Test(Troop value, NeuroContentValidatorContext context)
    {
        if (value.Health < 1) context.AddProblem("Health must be at least 1");
    }
}
```

Found by assembly scan - no registration, and it works anywhere, though an `Editor` folder is the tidy
home. Runs live in the editor's `Tests` section (turns red on failure) and as an edit-mode test under
`NeuroContentTestsRunner > TestRefTables`, so validation failures break CI.

`NeuroContentValidatorContext` gives you `References`, `Stack` (where in the object graph you are),
`GetParentInStack(depth)`, `AddProblem` / `AddProblemWithoutPath`, and `SkipHeavyTests` for the live
in-editor pass. Built-in validators already check that every `AssetAddress` resolves and every
`Reference<>` points at something real.

## Editing data from your own editor scripts

```csharp
var item = NeuroDataProvider.GetSharedTable<Troop>("goblin");
item.Health = 120;
NeuroEditorDataProvider.Shared.SaveData(item);
```

`NeuroEditorDataProvider.Shared` also has `Add(newObj)`, `Delete(dataFile)`, `Find(type, id)`,
`FindNextId(type)`, `SetRefName(...)`, `ChangeRefId(...)` (returns everything it repointed) and
`Reload()`.

## Stripping data for builds

```csharp
public class TroopBuildProcessor : INeuroBundledDataResourcesForBuildProcessor
{
    public void PrepBeforeBuildProcessing(NeuroReferences refs, BuildReport report) { }

    public bool ProcessForInclusion(IReferencable referencable)
    {
        if (referencable is Troop t) t.DesignerNotes = null;
        return true;              // false excludes the item from the build entirely
    }
}
```

## Customising the editor UI

Cheap wins: `[DisplayName]` (on a **type** - renames it in dropdowns, and `"Core / Stats []"` nests it under
a "Core" header like a Unity menu path; it is ignored on a field),
`[Tooltip]` / `[Description]` (type or field), `[Header("> foldout")]`,
`[Multiline]` / `[TextArea(minLines, maxLines)]` (string - `[TextArea]` also sizes the box),
`[Range(min, max)]` (draws `int`/`uint`/`long`/`float`/`double` as a slider with a number box, like
Unity's own inspector), `[Min]` (same numeric types - clamps on edit; ignored next to a `[Range]`),
`[Space]`, `[HideInInspector]` (field or property), `[InspectorStyle]` (`spaceBefore` / `spaceAfter`;
`horizontal: px` puts neighbouring fields on one row, `HideName = true` drops a field's name label - for a
horizontal row of a dropdown, an enum and a number that read fine on their own). Still **not** read - they
compile but do nothing here: `[Delayed]`, `[InspectorName]`, `[ColorUsage]`, `[GradientUsage]`,
`[NonReorderable]`, `[ContextMenuItem]`.

**One-line structs.** `[InspectorStyle(Inline = true)]` on a struct or class draws it as a single row
instead of a foldout - wherever it appears: a list entry becomes `0  [stat ▾] [value]`, a field becomes
`Name  [stat ▾] [value]`. The first field takes the row's name, the rest are unlabelled; give a field
`[InspectorStyle(horizontal: 90)]` for a fixed width, otherwise it shares the remaining space. Add
`InlineFieldNames = true` when every field needs its own name - a vector's `x` `y` - and the row becomes
`Name  x [ ] y [ ]` with the row's name as its own leading label. `[Header]` inside the type is ignored,
and a null class value still shows the foldout so it can be created. For a struct you cannot attribute
(Unity's, a package's) call `NeuroSyncEditorFields.SetInline(typeof(float2), showFieldNames: true)` from
the same `INeuroCustomTypesRegistryHook` that registers its fields; the effect is identical, and the call
is stripped from player builds like `AddField`.

```csharp
[InspectorStyle(Inline = true)]
public struct StatValue
{
    [Neuro(1)] public Reference<Stat> Stat;
    [InspectorStyle(horizontal: 90)] [Neuro(2)] public long Value;
}
```

**Narrowing a reference dropdown.** Subclass `NeuroReferenceFilterAttribute` (runtime assembly, so the
attribute can sit on model fields) and decide per item in `Include(IReferencable, NeuroReferences)`; put
the attribute on the `Reference<T>` field or property. Both the Neuro Editor and the Unity inspector's
`Reference<T>` drawer list only the items it accepts, plus the current value, which is kept and marked
"(filtered out)" when it fails, so a stale selection can still be seen and changed. Several attributes on
one field must all accept an item. The dropdown never restricts the data; instead a stored value the
filter rejects is a content validation problem (`NeuroReferenceFilterValidator`, built in), so it shows
red in the editor's Tests section and fails `NeuroContentTestsRunner`. Pass `Validate = false` for a
filter that is only a convenience. The dropdown says when it is narrowed: a footer reads
"Showing 32 of 50, filtered by [ArmourOnly]".

**It reaches down.** Put it on a list, struct or class field and every reference nested under that field
inherits it - each `Stat` inside a `List<StatValue>`, say - which is how a shared struct gets a different
filter per place it is used. The nearest member on the way out from the reference that carries any filter
attribute wins outright (no merging with outer ones); collection elements have no member of their own and
fall through to the collection's field. Override `AppliesTo(Type)` so a filter for one referencable type
is skipped by other reference types sharing the container - a `Reference<LocText>` next to the stat.
The Unity inspector drawer follows the same rule along the serialized property path, and so does the
validator, which reports the path (`Values[3].Property: #1e:damage is not allowed here by [ArmourOnly]`).

**Add a filter whenever you declare a `Reference<T>` field whose valid targets are a known subset** -
a category, a slot, a tag, a subtype - rather than leaving the dropdown open and relying on a comment.
Reuse an existing filter attribute if the project has one for that type; write a small subclass if not.
A filter you are unsure about is still worth adding with `Validate = false`.

```csharp
public class ArmourOnlyAttribute : NeuroReferenceFilterAttribute
{
    public override bool Include(IReferencable item, NeuroReferences refs)
        => item is Item i && i.Slot == ItemSlot.Armour;
    public override bool AppliesTo(Type refType) => typeof(Item).IsAssignableFrom(refType);
}

[ArmourOnly] [Neuro(1)] public Reference<Item> Chest;
[ArmourOnly(Validate = false)] [Neuro(2)] public Reference<Item> Preferred;   // dropdown only
[ArmourOnly] [Neuro(3)] public List<ItemStack> Wardrobe;   // every ItemStack.Item inside inherits it
```

Reference dropdown labels/icons: implement `INeuroRefDropDownCustomizable` /
`INeuroRefDropDownIconCustomizable`. Full custom drawers: `ICustomNeuroEditorProvider.CreateCustomDrawer`
returns a `VisualElement` for types you take over, `null` otherwise, with helpers on
`ObjectInspectorFields`.

This API is documented as liable to change and is rarely what you want — prefer `INeuroContentValidator`
over drawing your own validation UI. If you are actually writing a drawer, read
[../EditorCustomisation.md](../EditorCustomisation.md), which has the full worked examples.
