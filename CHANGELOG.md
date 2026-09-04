# Changelog

All notable changes to this package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0]

### Added
- Documentation for coding assistants in `Docs/AI/`, with adapter files for Claude Code
  (`.claude/skills/neuro/`) and Cursor (`.cursor/rules/neuro.mdc`).
- `Docs/EditorTools.md` covering the editor tools and settings.
- Extensive binary and JSON read/write test coverage, including extreme values across the
  full range of every supported type.
- Support for more default types.
- A simpler hook for external libraries to draw their own fields inside the Neuro editor.
- Dragging an asset into an `AssetAddress` field now offers to make it addressable.

### Changed
- `RefId` is now displayed and parsed in base36.
- Custom field drawers now take priority over the built-in ones by default.
- Verified against Unity 6.5.10f1 and 6.6.
- `LICENSE` is now `LICENSE.md` so the Unity Package Manager can display it.

### Fixed
- Large number handling in binary and JSON.
- Precision loss on small decimals in JSON.
- A nested child edge case in read/write.
- `Assembly.Location` being null on newer Unity versions.

### Removed
- The redundant `package.json` files under `Ninjadini.Neuro/` and `Ninjadini.Neuro.Unity/`.
  The package is installed from the repository root, so only the root manifest was ever read.

## [0.1.5] and earlier

This changelog starts at 0.2.0. For earlier versions see the
[commit history](https://github.com/Ninjadini/Neuro/commits/main) and
[tags](https://github.com/Ninjadini/Neuro/tags).

[Unreleased]: https://github.com/Ninjadini/Neuro/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/Ninjadini/Neuro/compare/v0.1.5...v0.2.0
