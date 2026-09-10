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
| **Migrate Renamed Field...** | Moves data from a field's old json name onto its new one after you rename it in code - see below. |
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
[CraftClickerGameSaveContentProvider.cs](https://github.com/Ninjadini/NeuroExampleProject/blob/main/Assets/Scripts/CraftClicker/Editor/CraftClickerGameSaveContentProvider.cs).

## Migrate Renamed Field

JSON is keyed by field *name*, binary by *tag*. So renaming a `[Neuro]` field costs nothing in binary,
but every JSON data file still holds the old key - which the reader does not recognise any more, and the
value is quietly dropped the next time it loads. This tool renames the key in the data files so the value
comes back.

1. Pick the **class**.
2. Pick the field from the list of that class's `[Neuro]` fields - this is where the value should end up.
3. Type the **old field name**, the one still in the JSON.
4. **Preview** lists exactly what would change. **Migrate** does it.

Only the key is rewritten, in place, at its exact position in the file - formatting, field order and
everything else are left alone, so the diff shows nothing but the renamed keys.

The data files are walked with your C# types alongside them, so every JSON object knows which class it
belongs to, `-subType` and all. That means:

- the rename is scoped to the class you picked - a field of the same name on some other class is not touched,
- it finds the class wherever it appears, nested inside other objects, in lists and in dictionaries,
- an object that already has the new field is left alone rather than ending up with both, and reported.

Each file is read back before it is written, and left untouched if it would not parse.

You can drive the same thing from a script:

```csharp
var result = new NeuroJsonFieldRenamer().Rename(typeof(TowerAttack), "Damage", "Dmg", dryRun: false);
// result.Renamed / .Skipped / .ChangedFiles / .Problems
```

## Project Settings > Ninjadini ❖ Neuro

Shared with the team, stored in `ProjectSettings/NeuroSettings.asset`:

| Setting | |
|---|---|
| **Primary Data Path** | Where the JSON data files live. Default `NeuroData`. |
| **Bake Data Resources For Build** | Bake the data into Resources so it is available in builds. Turn off only if you load it yourself. Default on. |
| **Resources Dir** | Where that baked file goes. Default `Assets/Resources/`. |
| **Undo Redo Enabled** | Neuro Editor edits go on Unity's undo stack (Ctrl+Z / Ctrl+Y, Edit > Undo). Default on. |
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

Unity's own field attributes work too, on the numeric types (`int`, `uint`, `long`, `float`,
`double`), on strings, or on any field:

```csharp
[Range(0f, 3f)] [Neuro(4)] public float StepRate;     // slider + number box
[Min(0)]        [Neuro(5)] public int Cost;           // clamps on edit
[Space(10)]     [Neuro(6)] public float Weight;       // gap above the field
[TextArea(3, 8)][Neuro(7)] public string Notes;       // multiline box, sized to the line counts
[HideInInspector] [Neuro(8)] public int Internal;     // still serialised, just not shown
```

`[Multiline]` is accepted for strings as well. `[Min]` is ignored on a field that also has a
`[Range]`. None of these affect serialisation - a value already outside a `[Range]` or `[Min]` is
kept as it is until someone edits that field.

# What's next ?

[Editor Customisation >](EditorCustomisation.md)

[Advanced usages >](AdvancedUsages.md)
