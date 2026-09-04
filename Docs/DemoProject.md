# Demo Project

Easiest is to open the demo project in Unity.

It lives in its own repository: [Ninjadini/NeuroExampleProject](https://github.com/Ninjadini/NeuroExampleProject)

```
git clone https://github.com/Ninjadini/NeuroExampleProject.git
```

Open the cloned folder in Unity (6000.6 or newer). It pulls this package straight from git, so
there is nothing else to set up. This is what the walkthrough below is written against.

**Alternatively**, to drop the demo into a project you already have, import
[NeuroCraftClicker.unitypackage](https://github.com/Ninjadini/Neuro/raw/main/Samples~/CraftClickerDemo/NeuroCraftClicker.unitypackage)
via *Assets > Import Package > Custom Package…* — or grab it from *Package Manager > Neuro >
Samples > Import*. It carries the scripts, scene, icons and content,
but not project settings — so the render pipeline and input settings stay as your project has them.

 - [Scripts/CraftClicker/Model](https://github.com/Ninjadini/NeuroExampleProject/tree/main/Assets/Scripts/CraftClicker/Model/)
   * Neuro data model
- [Scripts/CraftClicker/CraftClickerLogic.cs](https://github.com/Ninjadini/NeuroExampleProject/blob/main/Assets/Scripts/CraftClicker/CraftClickerLogic.cs)
  * The logic code to load previous data... Modify+save data when the user perform interactions.
- [Scripts/CraftClicker/CraftClickerUI.cs](https://github.com/Ninjadini/NeuroExampleProject/blob/main/Assets/Scripts/CraftClicker/UI/CraftClickerUI.cs)
  * UI code to display the state of the 'game'

- [Scripts/CraftClicker/Editor/](https://github.com/Ninjadini/NeuroExampleProject/tree/main/Assets/Scripts/CraftClicker/Editor/)
    * Editor tooling scripts such as content validators and content debugger


- [Scripts/CraftClicker/Editor/CraftClickerAIContentCopier.cs](https://github.com/Ninjadini/NeuroExampleProject/blob/main/Assets/Scripts/CraftClicker/Editor/CraftClickerAIContentCopier.cs)
    * AI tool to generate the content and copy back the result to neuro data
    * Open the window via Tools > CraftClicker > AI content copier 


# What's next ?

[Advanced usages >](AdvancedUsages.md)

[Editor Tools & Settings >](EditorTools.md)

[Editor Customisation >](EditorCustomisation.md)