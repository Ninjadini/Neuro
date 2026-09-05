# Changelog

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.2.1]

### Player save files have moved (breaking)
`LocalNeuroContinuousSave` now keeps saves in `<file>.0` and `<file>.1` rather than `<file>`, with a
small header on each, so that an interrupted save can not destroy the last good one. **A save written
by an earlier version is not read** - players of an already shipped build start with fresh data, and
the old file is left where it is. If that matters for your game, read the old file yourself before the
first save and hand it over with `SetData()`.

### New compile errors (breaking)
Three things that used to fail silently at runtime are now caught at compile time. Existing code that
does any of these will no longer build:
- `Neuro101` - a dictionary key made of `[Neuro]` fields. It wrote data that could not be read back.
  A key has to be a single value - a string, an enum, a number, a `DateTime`/`TimeSpan` or a `Reference<>`.
- `Neuro314` - `[NeuroGlobalType]` on an interface. The id was never registered, and only failed at the
  first `WriteGlobalTyped()`. A global type id registered by hand on an interface is now found, as the
  type lookup follows interfaces as well as base classes.
- `Neuro315` - `Reference<>` to anything but the root referencable type. Every subclass is registered in
  the root's table, so a `Reference<SubClass>` resolved to whatever the id happened to be, and the
  editor's reference tooling did not see it at all: "find references" reported none, and changing an
  item's RefId left those references dangling without warning. Declare the root type and cast where the
  subclass is needed.

### Fixed
- Reading a list of objects into an object that already held one no longer mixes the items up.
  JSON matched each item against the wrong existing instance, so items were read into a neighbour's
  object or freshly allocated, losing the reuse; binary left the previous object in place where the
  data said `null`.
- Lazily loading a referencable part way through another read on the same reader no longer disturbs
  that read - it used to silently drop everything after the point the load happened.
- Returning an object to a pool now reaches poolable objects held inside list items and dictionary
  values. Only items that were themselves poolable used to be walked into, so anything they held was
  never returned.
- `LocalNeuroContinuousSave` now also saves a pending change when the app goes to the background
  (`OnApplicationPause`/`OnApplicationFocus`). On mobile `OnDestroy` is not guaranteed to run.
- The Neuro editor no longer touches Unity's APIs from background threads. The data file watcher and
  the "find references" search both did, which could corrupt lazily loaded data or throw at random.
- A batch of smaller Neuro editor fixes: picking a nested sub type in the polymorphic dropdown,
  `System.Drawing.Color` fields, selection after deleting an item, redraw after a rejected RefId change,
  unresolvable assemblies in the "create object" type list, `⌨ Code` and prefab reference search
  throwing on assets that fail to load, and a stray square corner.

### Changed
- Reading JSON is much faster on anything but small documents. A field lookup used to scan every node
  in the document; it now walks only the fields of the object it is in. A 145KB document reads about
  10x faster, and it no longer gets quadratically worse as the document grows.

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
