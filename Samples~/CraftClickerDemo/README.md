# Craft Clicker Demo

The demo is **not shipped inside this package** — it is a full Unity project, not a folder of
assets, so it cannot be imported here. It needs its own `ProjectSettings` (URP render pipeline
assets, Input System actions) and a `NeuroData/` folder that lives at the project root, outside
`Assets/`.

It lives in its own repository. Clone it and open the folder directly:

```
git clone https://github.com/Ninjadini/NeuroExampleProject.git
```

Then open the cloned folder in Unity (6000.6 or newer). It pulls this package from git
automatically, so there is nothing else to set up.

It is a small idle/crafting game that exercises most of Neuro in one place: `Referencable` config
types, `Reference<T>` links, status effects via inheritance, content validators, player progress
saving, and the Neuro editor window.

Walkthrough: https://github.com/Ninjadini/Neuro/blob/main/Docs/DemoProject.md
