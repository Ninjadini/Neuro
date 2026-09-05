; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
Neuro002 | Syntax | Error | Invalid class neuro tag
Neuro022 | Syntax | Error | Readonly Neuro field on primitive types
Neuro023 | Syntax | Error | Readonly Neuro fields without an initializer
Neuro101 | Syntax | Error | Unsupported type / invalid dictionary key type / non-partial Neuro class
Neuro102 | Syntax | Error | Unsupported number type
Neuro300 | Syntax | Error | Field attribute tag already used
Neuro301 | Syntax | Error | Invalid field neuro tag
Neuro303 | Syntax | Error | Class attribute tag already used
Neuro304 | Syntax | Error | Class attribute tag reserved
Neuro305 | Syntax | Error | Class attribute tag not set
Neuro310 | Syntax | Error | Global type id already used
Neuro311 | Syntax | Error | Invalid global neuro type id
Neuro312 | Syntax | Error | Global neuro type attribute missing
Neuro313 | Syntax | Error | Global type id not set
Neuro314 | Syntax | Error | Global type id on an interface
Neuro404 | Syntax | Error | Missing neuro class attribute
Neuro405 | Syntax | Error | Multiple inheritance paths not supported
Neuro406 | Syntax | Error | Missing neuro class attribute (fast codegen)
Neuro911 | Syntax | Error | Exception was thrown while generating Neuro source
