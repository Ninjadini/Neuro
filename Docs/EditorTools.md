# Editor Tools & Settings

## Tools > Neuro menu

| Item | What it does |
|---|---|
| **❖ Editor** | The main window - browse, add, edit and delete your referencable data. |
| **Content Debugger** | Inspect and edit any neuro data that is *not* in the Neuro Editor - a player save, a file on disk, or pasted JSON/binary text. |
| **Type Mapping Debugger** | Every registered type across every assembly, with its `[NeuroGlobalType]` and polymorphic subtype tags. Useful when a tag map in one generated file isn't enough. |
| **Reload** | Re-read the JSON data files from disk. |
| **Reload + Read all data** | Same, then deserialises everything and reports how long it took - a quick way to catch a broken data file. |
| **Save Data To Resources** | Bake the data to the binary resource used in builds. Runs automatically at build time. |
| **Save Resources data as JSON** | The reverse - writes the baked binary back out as JSON. |
| **Migrate RefIds to base36...** | One time migration, see [GettingStarted](GettingStarted.md#migrating-data-from-before-base36-refids). |
| **Bake AutoTypesRegister Script** | Writes the generated types register out as a normal script. Only needed if you turn the automatic version off. |

## Content Debugger

Point it at a source, pick the type, and you get the same inspector as the Neuro Editor - editable, and
saveable back to where it came from.

Sources:
- **Persistent data** - a save file in `Application.persistentDataPath`, by name.
- **File** - any file path.
- **Text** - paste JSON or binary text in.

Set *Format* to JSON or Binary to match. For data written with `WriteGlobalTyped`, pick the
`object with -globalType` entry instead of a concrete type.

You can add your own sources by subclassing `NeuroContentDebugger.ContentProvider` - the demo project
does this in
[CraftClickerGameSaveContentProvider.cs](../Development~/ExampleProject/Assets/Scripts/CraftClicker/Editor/CraftClickerGameSaveContentProvider.cs).

## Project Settings > Ninjadini ❖ Neuro

Shared with the team, stored in `ProjectSettings/NeuroSettings.asset`:

| Setting | |
|---|---|
| **Primary Data Path** | Where the JSON data files live. Default `NeuroData`. |
| **Bake Data Resources For Build** | Bake the data into Resources so it is available in builds. Turn off only if you load it yourself. Default on. |
| **Resources Dir** | Where that baked file goes. Default `Assets/Resources/`. |
| **Undo Redos Enabled** | Experimental undo/redo in the Neuro Editor. |
| **Bake Auto Type Registry For Build** | Leave on unless you know why you're turning it off. |

Yours only, stored in `UserSettings/` so it isn't shared:

| Setting | |
|---|---|
| **Log Timings** | `Debug.Log` how long loading takes. |
| **Show Dialog On Data File Change** | Prompt when the JSON files change on disk (e.g. after a git pull). |
| **Show Raw Ref Id Numbers** | Show the plain number next to base36 RefIds, `1v83 (87123)`. Display only. |

## Field layout attributes

Beyond `[Header]`, `[ToolTip]` and `[DisplayName]` (see
[EditorCustomisation](EditorCustomisation.md)), `[InspectorStyle]` adds spacing around a field, or puts
fields side by side:

```csharp
[InspectorStyle(spaceBefore: 10, spaceAfter: 4)]
[Neuro(1)] public string Name;

// horizontal is the field's width in px. Neighbouring fields that also set it share a row -
// the row ends at the first field without it, or at the next [Header].
[InspectorStyle(horizontal: 100)] [Neuro(2)] public int Min;
[InspectorStyle(horizontal: 100)] [Neuro(3)] public int Max;
```

# What's next ?

[Editor Customisation >](EditorCustomisation.md)

[Advanced usages >](AdvancedUsages.md)
