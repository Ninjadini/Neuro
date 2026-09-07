# Neuro tests

These test sources live inside the package so Unity's Test Runner can compile them, but they are the
*same files* the .NET test projects under `Development~/` compile — those `.csproj`s pull them back in
with `<Compile Include>` links rather than keeping a second copy.

Unity only compiles a package's `Tests/` folder when the consuming project opts in, so this costs
nothing to anyone who just adds the package. The [example project](https://github.com/Ninjadini/NeuroExampleProject)
opts in via `"testables"` in its `Packages/manifest.json`.

## Tests/Unity

`Tests/Unity` is the exception: those tests need `UnityEngine` types (`Color`, `Color32`, `Gradient`),
so they can only run in the Editor and are **not** linked into the `Development~` .NET projects.
Keep anything that references `UnityEngine` here - the `.csproj`s glob `Tests/Integration/**` and
`Tests/Sync/**`, so a Unity-dependent file dropped in either of those breaks the .NET build.
