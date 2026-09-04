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

Other menu items:

| Menu | Does |
|---|---|
| `Tools > Neuro > Content Debugger` | Inspect/round-trip arbitrary neuro data, including runtime saves. |
| `Tools > Neuro > Type Mapping Debugger` | Shows every registered type, its global id and subtype tags. The only view spanning all assemblies - the compile-time tag reports cover one assembly each. |
| `Tools > Neuro > Reload` / `Reload + Read all data` | Re-read the JSON files after external edits. |
| `Tools > Neuro > Migrate RefIds to base36...` | One-time migration for pre-base36 data. |

Also under that menu: `Save Data To Resources` / `Save Resources data as JSON` (bake and dump the binary
blob builds use) and `Bake AutoTypesRegister Script` (static type registry, skips assembly scanning).

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
free, repoints every `Reference<>` in the data, renames the file and saves everything it touched. It
cannot fix ids stored outside the Neuro data (scenes, prefabs, save games, hard-coded constants), and
undo only covers the item itself, not the repointed ones.

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
Addressable or in a `Resources` folder.

```csharp
[AssetType(typeof(Sprite))]                  // optional; filters the editor's picker
[Neuro(1)] public AssetAddress Icon;

var sprite = await obj.Icon.LoadAssetAsync<Sprite>();
obj.Icon.LoadAssetAsync<Sprite>(s => image.sprite = s);   // callback form
obj.Icon.LoadFromResources<Sprite>();                      // sync, Resources only
obj.Icon.LoadSceneAsync();
```

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

Cheap wins, on the type or field: `[DisplayName]`, `[Tooltip]`, `[Header("> foldout")]`,
`[InspectorStyle]`. Reference dropdown labels/icons: implement `INeuroRefDropDownCustomizable` /
`INeuroRefDropDownIconCustomizable`. Full custom drawers: `ICustomNeuroEditorProvider.CreateCustomDrawer`
returns a `VisualElement` for types you take over, `null` otherwise, with helpers on
`ObjectInspectorFields`.

This API is documented as liable to change and is rarely what you want — prefer `INeuroContentValidator`
over drawing your own validation UI. If you are actually writing a drawer, read
[../EditorCustomisation.md](../EditorCustomisation.md), which has the full worked examples.
