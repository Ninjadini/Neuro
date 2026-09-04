---
name: neuro
description: Use when working with Ninjadini Neuro in a C#/Unity project - defining `[Neuro(#)]` data types, `Referencable` config objects, `Reference<T>` links, reading/writing Neuro binary or JSON, saving player progress, NeuroData JSON files and RefIds, content validators, the Neuro Editor window, or diagnosing Neuro codegen errors (Neuro022/101/102/300/303/312/404/406...).
---

# Neuro

The Neuro reference for agents is tool-neutral markdown shipped inside the Neuro package, so that every
assistant reads the same copy. This skill only points at it.

**Read `Docs/AI/neuro.md` from the Neuro package now**, then the one reference file it routes you to
(`data-model.md`, `serialization.md` or `unity.md` in that same folder). Do not read the library source
to answer Neuro questions until those files have failed you.

Where the package lives depends on how it was installed:

| Install | Path |
|---|---|
| Working in the Neuro repo itself | `Docs/AI/neuro.md` |
| Embedded package | `Packages/com.ninjadini.neuro-unity/Docs/AI/neuro.md` |
| Git URL / registry | `Library/PackageCache/com.ninjadini.neuro-unity*/Docs/AI/neuro.md` |

If none of those resolve, glob for `**/Docs/AI/neuro.md`.
