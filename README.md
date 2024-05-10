# Neuro

## Goals
- No schema
- Record type + immutable arrays ?
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
- Non-backcompact, ultra compact type. ❌
- Unity asset references via Addressables
- NonAlloc String? ❌
- Async loading? ❌
- OnDemand deserialization? (only deserialize when referenced item is requested) ❌
- Referring to sub-tables .e.g. Item 2's level 3's ? (could just be code pattern) ❌


## Supported Types
- Primitives: bool, byte, int, uint, long, ulong, float, double
- Enums
- List<>, Array[]
- System structs: DateTime, TimeSpan
- Classes and subclasses
- Structs
- Nullable structs and primitives

## Maybe - Future Features
- Nested embeds (same ref in multiple places)
- Custom constructors ? (Unity's scriptable objects, but sounds like a bad idea)


## What (could be) bad about it?
- Only supports fields, properties are supported via a workaround
- Codegen might get slow on a larger projects? Perhaps you need to keep all the model files in one project and ignore other projcets
- Maybe hard to be able to tell which types are supported - but fixable with Roslyn validation