# Backward Compatibility

Neuro is designed so that you can add or remove fields without breaking backwards compatibility.

Basically, follow protobuf's (Protocol Buffers) rules

### ✅ Dos
- You can remove classes and fields.
- You can rename classes and fields. ⚠️ However if it is stored in config / json, the data will be lost as it is stored by name.

### ⚠️ Careful 
- If you change the type of a field, also change the Neuro tag.
- If you removed a field, don't reuse the Neuro tag. hint: use `[ReservedNeuroTag(##)]` so you don't accidentally use that tag later.
- Also avoid reusing polymorpic type tags once its deleted.
- You can not change the hierarchical structure of polymorphic types - most likely will not work

### Which tag numbers are already taken?

The two that are hard to eyeball are polymorphic subtype tags and global type ids, because they are
spread across files. Every generated `NeuroTypesRegister` file now starts with a map of both, with the
next free number worked out for you:

```
/* Neuro tag map - MyGame.Runtime
   [Neuro(#)] under Troop: used 1-7, 10, 12(reserved) | next free 8
     1 = Goblin
     2 = Orc
     ...
   [NeuroGlobalType(#)]: used 1-4, 9 | next free 5
*/
```

Open it from your IDE - in Rider/Visual Studio the generated file shows up under the project's source
generator output. It only covers the assembly it was generated for, so in a multi-asmdef project check
each one, or use `Tools/Neuro/Type Mapping Debugger` in Unity, which lists every assembly at once.

Field tags are not in the map - they are per class, so they are already sitting together in the file
you are editing. If you do double up on one, the error lists every tag in that class and the next free
one, the same way.

### Or just ask

Write `0` and let the compiler answer. Tag 0 is never valid, so it already errors - the error now tells
you what to put there instead:

```csharp
[Neuro(0)] public string myNewField;
// Neuro301: Neuro field attribute tag of `Troop.myNewField` must be between 1 and 2147483647.
//           Used tags: 1-2, 4. Next free: 3. Full list: 1=DisplayName; 2=Health; 4=Weapons
```

Same for `[Neuro(0)]` on a polymorphic subclass (Neuro305) and `[NeuroGlobalType(0)]` (Neuro313), which
report the next free number across the whole hierarchy / assembly rather than just one class. Those two
come from the code generation step, which unity does not show errors for - in unity read the tag map
above instead.