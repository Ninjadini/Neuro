# Neuro tests

These test sources live inside the package so Unity's Test Runner can compile them, but they are the
*same files* the .NET test projects under `Development~/` compile — those `.csproj`s pull them back in
with `<Compile Include>` links rather than keeping a second copy.

Unity only compiles a package's `Tests/` folder when the consuming project opts in, so this costs
nothing to anyone who just adds the package. The [example project](https://github.com/Ninjadini/NeuroExampleProject)
opts in via `"testables"` in its `Packages/manifest.json`.
