# Neuro Development Goals

## Things to do next
- Finish the demo project (timers and images)
- Reconsider the naming of things - should 'IReferencable' be just ConfigItem ?
- Finish the doc
- Add doc comments to core lib code and also in the demo
- JSON migration util - so you can rename fields and classes easier.
- More extreme tests like min max values and random numbers in between
- Maybe actually build example ways to deploy new content live (might need new library code)

## Feature Goals
- No schema
- Polymorphic
- Default values
- Private fields (fake public immutability)
- References
- Custom serializers (Unity's Vector3, Rect, etc)
- Object Pooling
- Fast read and write
- Minimal allocations.
- No reflection
- Ultra compact data
- Backcompat
- Any objects inspector in Unity
- Git friendly text storage
- OnDemand deserialization? (only deserialize when referenced item is requested)
- Unity asset references via Addressables
- Record type + immutable arrays ? ❌
- Non-backcompact, ultra compact type. ❌
- NonAlloc String? ❌
- Async loading? ❌
- Referring to sub-tables .e.g. Item 2's level 3's ? (could just be code pattern) ❌

## Supported Types
- Primitives: bool, byte, int, uint, long, ulong, float, double
- Enums
- List<>
- Dictionary<,>
- System structs: DateTime, TimeSpan
- Classes and subclasses
- Structs
- Nullable structs and primitives
- Array[] ❌

## Maybe - Future Features
- Nested embeds (same ref in multiple places)
- Custom constructors ? (Unity's scriptable objects, but sounds like a bad idea)

## What (could be) bad about it?
- Only supports fields, properties are supported via a workaround
- Codegen might get slow on a larger projects? Perhaps you need to keep all the model files in one project and ignore other projects
- Maybe hard to be able to tell which types are supported - but fixable with Roslyn validation
