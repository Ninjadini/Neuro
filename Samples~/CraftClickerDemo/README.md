# Craft Clicker Demo

A small idle/crafting game that exercises most of Neuro in one place: `Referencable` config types,
`Reference<T>` links, status effects via inheritance, content validators, player progress saving,
and the Neuro editor window.

There are two ways to get it.

## 1. Import the asset package

In a project that already has Neuro installed, open *Window > Package Manager*, select **Neuro**,
open the **Samples** tab and click **Import** on *Craft Clicker Demo*. That copies
`NeuroCraftClicker.unitypackage` into `Assets/Samples/…` — double-click it in the Project window to
import the demo itself.

You can also download
[NeuroCraftClicker.unitypackage](https://github.com/Ninjadini/Neuro/raw/main/Samples~/CraftClickerDemo/NeuroCraftClicker.unitypackage)
directly and use *Assets > Import Package > Custom Package…*

Either way it adds the scripts, the scene, the icons and the authored content in
`Assets/NeuroData/` — all of it under `Assets/`. It does not bring project settings with it, so the
render pipeline and input settings stay as your own project has them.

Then open `Assets/Scenes/CraftClicker.unity` and press Play.

Note that `Assets/NeuroData/` is registered as an *extra* data path. Any new Neuro ref files you
create still go to the primary data path — by default a `NeuroData` folder at the project root.

## 2. Clone the full project

```
git clone https://github.com/Ninjadini/NeuroExampleProject.git
```

Open the cloned folder in Unity (6000.6 or newer). It pulls this package from git automatically, so
there is nothing to set up. This is the version the walkthrough is written against: it carries its
own `ProjectSettings` (URP render pipeline assets, Input System actions), so it runs exactly as
intended.

Walkthrough: https://github.com/Ninjadini/Neuro/blob/main/Docs/DemoProject.md
