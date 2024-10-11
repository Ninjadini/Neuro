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