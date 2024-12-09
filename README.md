# Neuro

**Your Essential Data Management Toolset for Unity**

The problem:
- In Unity, you might use prefabs or scriptable objects to hold some config data, like what types of troops and how much health-points a troop type have.   
You learned some tricks and workarounds to get it close to how you want it. But we all know it has never been great…
- To save your player progress in game, you use a different mechanism, such as json - with different constraints that you learn to work around also.
- You might also use another mechanism to send and receive data from server.
- You might use asset bundles to change game balancing data over the air which comes with its own specific constraints that you learn to work around, yet again.
- Why do we have to deal with all these constraints and secrets? They are all just data in the end...

**Neuro is here with a unified data driven solution**

At its core, Neuro is a C# binary and JSON serializer.

For Unity, it integrates very powerful, yet flexible, editor tools to let you manipulate your data.  
Provides toolsets to store, retrieve, content test and debug your data with ease.

For binary, it is inspired by Google’s Protocol Buffers where it is superfast, compact and backwards comparable.  
Neuro is C# focused. It supports structs, dictionary, nullable primitives, inheritance, interfaces.  
You also don’t need schema files.

JSON is custom written so that it is superfast, with no type reflections and allocates minimally.

In fact for both binary and JSON, if you pre allocate buffer caches ahead, there are zero allocations from neuro, apart from the new objects that are returned per your request.

It leverages C# roslyn code analysis APIs to generate highly optimised and minimal code so serialisation methods do not need reflection methods.

Let's get started ⤵️

## [Getting Started >](Docs/GettingStarted.md)
> Or watch walkthrough video https://www.youtube.com/watch?v=tftZfd2boDQ

## [Demo Project >](Docs/DemoProject.md)

## [Advanced usages >](Docs/AdvancedUsages.md)

## [Editor Customisation >](Docs/EditorCustomisation.md)