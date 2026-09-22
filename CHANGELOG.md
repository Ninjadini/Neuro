# Changelog

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.2.1]

### Added
- **Search across the data** - `⌕ Search`, next to the type dropdown in the Neuro Editor, finds text in
  field names or values, across every file or only the item being edited. One line per match with its path
  and value (`Rounds[2].Count = 12`), prefixed by the item it belongs to; click a line to open that item.
  Matching is a plain case-insensitive substring: numbers as they print, enums by name, a `Reference<>` by
  its target's RefId and RefName. `NeuroDataSearch` is the same thing for scripts.
- **Migrating a renamed field's data** - `Tools > Neuro > Migrate Renamed Field...` moves a value from its
  old json field name onto the new one. Binary is keyed by tag and does not care; json is keyed by name, so
  until now the value was silently dropped on the next read. Only the json key is rewritten, in place, so
  formatting and field order survive, and the rename is scoped to the one class - polymorphic `-subType` is
  followed, an object that already has the new name is left alone, and every file is checked to still read
  back before it is written. `NeuroJsonFieldRenamer` is the same thing for scripts.
- **Changing a RefId can now repoint prefabs and scenes too** - the confirmation dialog offers
  `Change & update assets` as well as `Change data only`: an extra pass sweeps every prefab,
  ScriptableObject and scene under `Assets/` for `Reference<>` fields holding the old id and repoints those
  as well. It is opt-in because it runs after the data change and is not undoable. Scenes are skipped in
  play mode, or when the open scenes have unsaved changes and the save prompt is declined; whatever was
  skipped is reported. `NeuroAssetRefIdRewriter.Rewrite(...)` exposes the same sweep for scripts.
- **More Unity inspector attributes work in the Neuro Editor** - `[Range]` (slider plus number box),
  `[Min]` (clamps what an edit writes, ignored alongside `[Range]`), `[Space]`, `[TextArea]` and
  `[HideInInspector]`. Editor only: none of them clamp or hide anything at serialisation time, so existing
  data is untouched until someone edits the field.
- **Undo/redo in the Neuro Editor** - field edits, RefName and RefId changes, add, clone and delete are on
  Unity's undo stack, the file on disk follows, and undoing a RefId change repoints the other items again.
  The old experimental `Undo Redos Enabled` setting, which never worked, is replaced by
  `Undo Redo Enabled`, default on.
- `NeuroVisitor` and `NeuroEditVisitor` now hand over enum fields when `visitPrimitiveValues` is true, the
  way they already did numbers and strings. Nothing changes for a walk that did not ask for primitives.
- `NeuroJsonReader.CurrentValueIsString`, for custom json codecs that take a string or a number.

### Breaking
- **`Color` is fixed and now writes as hex.** It never survived a round trip - the decoder did not invert
  the encoder, so every value read back as garbage (`FFCC33` came back as `(0, 1, 0, 0)`); `Gradient` was
  broken with it, `Color32` was fine. **Any `Color` already stored holds the old packing and will read back
  differently** - there is no migration, because no old value decoded correctly anyway, so re-author the
  affected colours. Json is now a hex string - `"FFCC00"`, or `"FFCC0080"` when not fully opaque - and
  reads `RGB`/`RGBA`/`RRGGBB`/`RRGGBBAA`, any case, optional `#`, and still the old packed number; binary
  `Color` is `r | g<<12 | b<<24 | a<<36` scaled by 4095, `Color32` unchanged. So json is 8 bits per channel
  where binary keeps 12, and `Color` is LDR now - channels clamp to 0..1, use a `Vector4` for HDR.
- **Player save files have moved.** `LocalNeuroContinuousSave` keeps saves in `<file>.0` and `<file>.1`
  rather than `<file>`, with a small header on each, so an interrupted save can not destroy the last good
  one. **A save written by an earlier version is not read** - players of an already shipped build start
  with fresh data, and the old file is left where it is. If that matters for your game, read the old file
  yourself before the first save and hand it over with `SetData()`.
- **Three things that used to fail silently at runtime are now compile errors.** `Neuro101` - a dictionary
  key made of `[Neuro]` fields; a key has to be a single string, enum, number, `DateTime`/`TimeSpan` or
  `Reference<>`. `Neuro314` - `[NeuroGlobalType]` on an interface; the id was never registered, and a
  global type id registered by hand on an interface is now found, as the type lookup follows interfaces as
  well as base classes. `Neuro315` - `Reference<>` to anything but the root referencable type; every
  subclass is registered in the root's table, so it resolved to whatever the id happened to be, and the
  editor's reference tooling did not see it at all - declare the root type and cast where needed.

### Fixed
- Reading a list of objects into an object that already held one no longer mixes the items up. Json matched
  each item against the wrong existing instance, so items were read into a neighbour's object or freshly
  allocated; binary left the previous object in place where the data said `null`.
- Lazily loading a referencable part way through another read on the same reader no longer disturbs that
  read - it used to silently drop everything after the point the load happened.
- Returning an object to a pool now reaches poolable objects held inside list items and dictionary values.
  Only items that were themselves poolable used to be walked into.
- `LocalNeuroContinuousSave` now also saves a pending change when the app goes to the background
  (`OnApplicationPause`/`OnApplicationFocus`) - on mobile `OnDestroy` is not guaranteed to run.
- The Neuro editor no longer touches Unity's APIs from background threads. The data file watcher and the
  "find references" search both did, which could corrupt lazily loaded data or throw at random.
- A batch of smaller Neuro editor fixes: picking a nested sub type in the polymorphic dropdown,
  `System.Drawing.Color` fields, selection after deleting an item, redraw after a rejected RefId change,
  unresolvable assemblies in the "create object" type list, `⌨ Code` and prefab reference search throwing
  on assets that fail to load, and a stray square corner.

### Changed
- Reading json is much faster on anything but small documents. A field lookup used to scan every node in
  the document; it now walks only the fields of the object it is in. A 145KB document reads about 10x
  faster, and it no longer gets quadratically worse as the document grows.

## [0.2.0]

### RefIds are now base36
RefIds are displayed, parsed and written into `NeuroData` file names in base36 (`0-9a-z`),
so a generated id is 4 characters instead of a long number - `NeuroData/1-MyItem/4zbc-my_item.json`.

**Existing data needs a one time migration:** `Tools > Neuro > Migrate RefIds to base36...`.
See [Getting Started](Docs/GettingStarted.md#migrating-data-from-before-base36-refids).

### Added
- `Docs/AI/` - a condensed Neuro reference for coding assistants, with Claude Code and Cursor adapters.
- Extra data path list in Neuro settings, for loading data from more than one folder.
- Support for more default types, including Unity ones.
- `NeuroEditVisitor`, and a simpler hook for external libraries to draw their own fields in the editor.
- Dragging an asset into an `AssetAddress` field now offers to make it addressable.

### Fixed
- Large numbers and small decimals in JSON. Float and double output is now exact,
  so JSON written by 0.2.0 will differ from 0.1.5 for those.
- Writing a subclass through its base type no longer loses the type.
- Clearer errors instead of silent misbehaviour when reading or writing an unsupported type.
- `Assembly.Location` being null on newer Unity versions.

### Changed
- The demo moved to its own repo, [Ninjadini/NeuroExampleProject](https://github.com/Ninjadini/NeuroExampleProject),
  and also ships as an importable sample.
- Packaging tidy up: single root `package.json`, `LICENSE.md`, and this changelog.

## [0.1.5] and earlier
See the [commit history](https://github.com/Ninjadini/Neuro/commits/main) and [tags](https://github.com/Ninjadini/Neuro/tags).

[Unreleased]: https://github.com/Ninjadini/Neuro/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/Ninjadini/Neuro/compare/v0.1.5...v0.2.0
