# Changelog

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

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
