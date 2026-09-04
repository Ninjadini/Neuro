
Ninjadini.Neuro.SyncTests

This C# project is for testing manually written sync (serialise+deserialise) code.

It does not have any code gen hooked up so even if you type [Neuro(123], it will not generate the code for it.

Most of the project now lives in ../../Tests/Sync so that Unity's Test Runner compiles it too; this
project links those files back in. What is still physically here are the files that cannot go to Unity:

  UberTestClass.cs, FeaturesDemo.cs   - [Neuro] attributed partial types with hand written static Sync
                                        methods. Unity applies the code gen to everything referencing
                                        Ninjadini.Neuro, so it would emit a duplicate Sync.
  ...Tests.cs that use their types    - NeuroEditVisitorTests, NeuroRefIdTests, ReferenceTests,
                                        RefIdRewriteAcrossDatabaseTests, Json/JsonTests
  Json/NumberWritingComparisonTests   - needs Newtonsoft.Json

See ../../Tests/README.md for the full picture.
