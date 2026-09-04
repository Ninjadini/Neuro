# For AI coding assistants

[Docs/AI/](AI/) is a condensed Neuro reference written for coding agents — the whole API surface,
the codegen rules and the gotchas in ~25KB, so the agent answers from that instead of reading
the library source. It is plain markdown with no tool-specific format, so any assistant can use it.

Point your tool at it:

| Tool | How |
|---|---|
| Claude Code | Copy [.claude/skills/neuro/](../.claude/skills/neuro/) into your project's `.claude/skills/` |
| Cursor | Copy [.cursor/rules/neuro.mdc](../.cursor/rules/neuro.mdc) into your project's `.cursor/rules/` |
| Codex / Copilot / Aider / anything else | Add a line to your `AGENTS.md`, `CLAUDE.md` or equivalent: `For Neuro, read Packages/com.ninjadini.neuro-unity/Docs/AI/neuro.md` |

Both adapter files are ~15 lines that just tell the agent where `Docs/AI/` is — the content itself lives
in one place, so there is nothing to keep in sync. They are in dot-folders, which Unity ignores, so they
need no `.meta` files and never reach your build.
