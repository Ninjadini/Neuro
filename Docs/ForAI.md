# For AI coding assistants

[Docs/AI/](AI/) is a condensed Neuro reference for coding agents — the API surface, codegen rules and
gotchas in ~25KB. Point your tool at it once; nothing here is auto-discovered.

**Step 1.** Find your `Docs/AI/` path — it depends on the install. Substitute it for `<neuro>` below.

| Install | `<neuro>` |
|---|---|
| Embedded package | `Packages/com.ninjadini.neuro-unity` |
| Git URL / registry | `Library/PackageCache/com.ninjadini.neuro-unity*` |
| The Neuro repo itself | `.` |

**Step 2.** Copy the snippet for your tool.

### Claude Code, Codex, Copilot, Aider — your `CLAUDE.md` / `AGENTS.md`

```markdown
For Neuro (`[Neuro(#)]` types, `Referencable` config, `Reference<T>`, binary/JSON serialisation,
NeuroData RefIds, Neuro codegen errors), read `<neuro>/Docs/AI/neuro.md` before writing code —
it routes on to `data-model.md`, `serialization.md` or `unity.md` in that same folder.
```

### Claude Code, as a skill — `.claude/skills/neuro/SKILL.md`

Loads the reference only when relevant, instead of on every prompt.

```markdown
---
name: neuro
description: Use when working with Ninjadini Neuro in a C#/Unity project - defining `[Neuro(#)]` data types, `Referencable` config objects, `Reference<T>` links, reading/writing Neuro binary or JSON, saving player progress, NeuroData JSON files and RefIds, content validators, the Neuro Editor window, or diagnosing Neuro codegen errors (Neuro022/101/102/300/303/312/404/406...).
---

# Neuro

The Neuro reference for agents is tool-neutral markdown shipped inside the Neuro package. This skill
only points at it.

**Read `<neuro>/Docs/AI/neuro.md` now**, then the one reference file it routes you to
(`data-model.md`, `serialization.md` or `unity.md` in that same folder). Do not read the library
source to answer Neuro questions until those files have failed you.
```

### Cursor — `.cursor/rules/neuro.mdc`

```markdown
---
description: Ninjadini Neuro - [Neuro(#)] data types, Referencable config, Reference<T>, binary/JSON serialisation, NeuroData RefIds, Neuro codegen errors
globs: ["**/*.cs"]
alwaysApply: false
---

Neuro is a C# binary + JSON serializer with a Unity data-authoring layer, in namespace
`Ninjadini.Neuro`. A Roslyn source generator emits the serialisation code from `[Neuro(#)]` attributed
types, so most mistakes surface as compile errors (`Neuro022`, `Neuro101`, `Neuro102`, `Neuro300`,
`Neuro303`, `Neuro312`, `Neuro404`, `Neuro406`, ...).

The full condensed reference ships inside the Neuro package as tool-neutral markdown. Read
`<neuro>/Docs/AI/neuro.md` before answering Neuro questions or writing Neuro types, then the
reference file it routes you to.
```
