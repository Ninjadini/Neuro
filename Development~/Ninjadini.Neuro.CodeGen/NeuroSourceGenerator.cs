using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Ninjadini.Neuro.CodeGen
{
    [Generator]
    public partial class NeuroSourceGenerator : ISourceGenerator
    {
        public static bool Verbose;
        
        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                if (NeuroCodeGenUtils.GetScanMode(context.Compilation) == NeuroScanMode.Skip)
                {
                    return;
                }
                var src = Generate(context.Compilation, context.ReportDiagnostic);
                if (!string.IsNullOrEmpty(src))
                {
                    context.AddSource("NeuroTypesRegister", SourceText.From(src, Encoding.UTF8));
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(NeuroSourceAnalyzer.ExceptionThrown, null, e.Message+"\n"+e.StackTrace)
                    );
                
                // This is a unity specific problem where it doesn't show an error for ReportDiagnostic() during the 'generation' step.
                // For now, we'll just write the error to the file so that you can tell its happening.

                //throw;
                context.AddSource("NeuroTypesRegister", SourceText.From(
                    $"using ThereWasAnExceptionWhileGeneratingForNeuro_SeeGeneratedFile_NeuroTypesRegister_InYourIDEForDetails;" +
                    $"\n/*" +
                    $"\nSorry, there seems to be a bug in Unity where errors during code generate step doesn't show up." +
                    $"\nHere is the exception thrown:" +
                    $"\n/*{e}" +
                    $"\n*/", 
                    Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context) { }
        
        public string Generate(Compilation compilation, Action<Diagnostic> onError_ = null)
        {
            var generationResult = new CodeWalker().Walk(compilation, onError_);
            if (generationResult.FatalMessages != null && generationResult.FatalMessages.Count > 0)
            {
                return CreateFatalSource(generationResult.FatalMessages);
            }
            if ((generationResult.Classes?.Count ?? 0) == 0 && (generationResult.RegistryHooks?.Count ?? 0) == 0)
            {
                return "";
            }

            // its too simple to bother with special tools. Also this is going to be super fast
            
            var strBuilder = new StringBuilder(2048);
            AppendTagMap(compilation, strBuilder, generationResult);
            
            AppendRegistry(compilation, strBuilder, generationResult);
            
            AppendPartialClasses(strBuilder, generationResult);
            
            return strBuilder.ToString();
        }

        /// A conflict stops generation, which on its own just means the registry is missing and every
        /// neuro type reports as unregistered. Unity doesn't show a diagnostic reported during the
        /// generation step (see Execute), so the reason gets written into the source instead - same
        /// trick the exception fallback uses.
        static string CreateFatalSource(List<string> messages)
        {
            var strBuilder = new StringBuilder(512);
            strBuilder.AppendLine("using NeuroTagConflict_SeeGeneratedFile_NeuroTypesRegister_InYourIDEForDetails;");
            strBuilder.AppendLine("/*");
            strBuilder.AppendLine("Neuro code generation stopped: a tag is used twice.");
            strBuilder.AppendLine("Fix the conflict(s) below and the rest of the neuro errors will go away with it.");
            strBuilder.AppendLine();
            foreach (var message in messages)
            {
                strBuilder.AppendLine(message);
            }
            strBuilder.AppendLine("*/");
            return strBuilder.ToString();
        }

        /// Which tag numbers this assembly has already spent, so the next one can be picked by reading
        /// rather than by guessing and waiting for the conflict error.
        void AppendTagMap(Compilation compilation, StringBuilder strBuilder, GenerationResult generationResult)
        {
            var tagsByRootClass = generationResult.TagsByRootClass;
            var globalTypeIds = generationResult.GlobalTypeIds;
            var hasRoots = tagsByRootClass != null && tagsByRootClass.Count > 0;
            var hasGlobals = globalTypeIds != null && globalTypeIds.Count > 0;
            if (!hasRoots && !hasGlobals)
            {
                return;
            }
            strBuilder.Append("/* Neuro tag map - ").AppendLine(compilation.Assembly.Name);
            strBuilder.AppendLine("   This assembly only. Types in other assemblies are not visible from here,");
            strBuilder.AppendLine("   so a global type id free here can still be taken elsewhere.");
            if (hasRoots)
            {
                foreach (var rootNameAndTags in tagsByRootClass.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    strBuilder.AppendLine();
                    strBuilder.Append("   [Neuro(#)] under ").Append(rootNameAndTags.Key).Append(": ");
                    AppendTagSummary(strBuilder, rootNameAndTags.Value, "tags");
                }
            }
            if (hasGlobals)
            {
                strBuilder.AppendLine();
                strBuilder.Append("   [NeuroGlobalType(#)]: ");
                AppendTagSummary(strBuilder, globalTypeIds, "global type ids");
            }
            strBuilder.AppendLine("*/");
        }

        static void AppendTagSummary(StringBuilder strBuilder, List<NeuroTagReport.Entry> entries, string noun)
        {
            var sorted = NeuroTagReport.Sorted(entries);
            strBuilder.Append("used ").Append(NeuroTagReport.UsedRanges(sorted));
            strBuilder.Append(" | next free ").Append(NeuroTagReport.NextFree(sorted));
            strBuilder.AppendLine();
            foreach (var entry in sorted)
            {
                strBuilder.Append("     ").Append(entry.Tag).Append(" = ");
                strBuilder.AppendLine(entry.IsReserved ? "[reserved]" : entry.Name);
            }
            if (sorted.Count == 0)
            {
                strBuilder.Append("     (no ").Append(noun).AppendLine(" yet)");
            }
        }

        void AppendRegistry(Compilation compilation, StringBuilder strBuilder, GenerationResult generationResult)
        {
            strBuilder.AppendLine(@"using _NeuroSyncNS = Ninjadini.Neuro.Sync;");
            strBuilder.AppendLine(@"using _NeuroSyncTypes = Ninjadini.Neuro.Sync.NeuroSyncTypes;");
            
            var uniqueClassName = "NeuroCodeGen_" + Regex.Replace(compilation.Assembly.Name, @"\W", "_");
            strBuilder.Append(@"[assembly:Ninjadini.Neuro.NeuroAssemblyAttribute(typeof(");
            strBuilder.Append(uniqueClassName);
        strBuilder.Append(@"), ""RegisterTypes"")]
public static class ");
            strBuilder.Append(uniqueClassName);
            strBuilder.Append(@"
{
    static bool registered;
    public static void RegisterTypes()
    {
        if (registered) return;
        registered = true;
        // Generated ");
            var timeNow = DateTime.UtcNow;
            strBuilder.Append(timeNow.ToString());
            strBuilder.Append(".");
            strBuilder.Append(timeNow.Millisecond);
            strBuilder.Append(". ");
            strBuilder.Append(timeNow.Ticks);
            strBuilder.Append(". CodeGen DLL creation date: ");
            strBuilder.Append(GetCodeGenDllDate());
            strBuilder.AppendLine();
            
            if (generationResult.RegistryHooks != null)
            {
                foreach (var hook in generationResult.RegistryHooks)
                {
                    //        new {typeName}().Register();
                    strBuilder.Append("        new ");
                    strBuilder.Append(hook);
                    strBuilder.AppendLine("().Register();");
                }
            }
            foreach (var classToGenerate in generationResult.Classes)
            {
                //        if(NeuroSyncTypes.IsEmpty<{typeName}>()) NeuroSyncTypes.Register<{typeName}>({typeName}.Sync);
                //OR
                //        if(NeuroSyncTypes.IsEmpty<{typeName}>()) NeuroSyncTypes.RegisterSubClass<{baseTypeName}, {typeName}>({typeTag}, {typeName}.Sync);
                
                strBuilder.Append("        if(_NeuroSyncTypes.IsEmpty<");
                AppendFullClassName(strBuilder, classToGenerate);
                strBuilder.Append(">())\n         _NeuroSyncTypes.");
                if (string.IsNullOrEmpty(classToGenerate.BaseClassName))
                {
                    strBuilder.Append("Register<");
                    AppendFullClassName(strBuilder, classToGenerate);
                    strBuilder.Append(">(");
                }
                else
                {
                    strBuilder.Append("RegisterSubClass<");
                    if (classToGenerate.RootClassName != classToGenerate.BaseClassName)
                    {
                        strBuilder.Append(classToGenerate.RootClassName);
                        strBuilder.Append(", ");
                    }
                    strBuilder.Append(classToGenerate.BaseClassName);
                    strBuilder.Append(", ");
                    AppendFullClassName(strBuilder, classToGenerate);
                    strBuilder.Append(">(");
                    strBuilder.Append(classToGenerate.Tag);
                    strBuilder.Append(", ");
                }
                if (classToGenerate.HasPrivateFields)
                {
                    AppendFullClassName(strBuilder, classToGenerate);
                    strBuilder.Append(".Sync");
                }
                else
                {
                    strBuilder.Append("(_NeuroSyncNS.INeuroSync neuro, ref ");
                    AppendFullClassName(strBuilder, classToGenerate);
                    strBuilder.AppendLine(" value) => {");
                    AppendConstructor(strBuilder, classToGenerate);
                    AppendFields(strBuilder, classToGenerate);
                    strBuilder.Append("         }");
                }
                
                if (classToGenerate.GlobalTypeId > 0)
                {
                    strBuilder.Append(", globalTypeId:");
                    strBuilder.Append(classToGenerate.GlobalTypeId);
                }
                strBuilder.AppendLine(");");
            }

            foreach (var referencableType in generationResult.ReferencableTypes)
            {
                // NeuroSyncTypes.Register<Reference<{typeName}>>(FieldSizeType.VarInt, Reference<{typeName}>.Sync);
                
                strBuilder.Append("        if(_NeuroSyncTypes.IsEmpty<Ninjadini.Neuro.Reference<");
                strBuilder.Append(referencableType);
                strBuilder.Append(">>())\n         _NeuroSyncTypes.Register<Ninjadini.Neuro.Reference<");
                strBuilder.Append(referencableType);
                strBuilder.Append(">>(_NeuroSyncNS.FieldSizeType.VarInt, Ninjadini.Neuro.Reference<");
                strBuilder.Append(referencableType);
                strBuilder.AppendLine(">.Sync);");
            }
            foreach (var enumType in generationResult.EnumTypes)
            {
                strBuilder.Append("        if(_NeuroSyncNS.NeuroSyncEnumTypes<");
                strBuilder.Append(enumType);
                strBuilder.Append(">.IsEmpty())\n         _NeuroSyncNS.NeuroSyncEnumTypes<");
                strBuilder.Append(enumType);
                strBuilder.Append(">.Register((e) => (int)e, (i) => (");
                strBuilder.Append(enumType);
                strBuilder.AppendLine(")i);");
            }
            strBuilder.AppendLine(@"    }
}");
            /*
            strBuilder.Append(@"
public static class NeuroTypesRegister
{
    public static void Register()
    {        
        ");
            strBuilder.Append(uniqueClassName);
            strBuilder.AppendLine(@".RegisterTypes();
    }
}");
*/
        }

        string GetCodeGenDllDate()
        {
            // Assembly.Location is empty when the analyzer was loaded from memory rather than a file,
            // and the shadow copied file may no longer exist by the time we get here.
            try
            {
                var location = GetType().Assembly.Location;
                return string.IsNullOrEmpty(location) ? "unknown" : new System.IO.FileInfo(location).LastWriteTime.ToString();
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        void AppendPartialClasses(StringBuilder strBuilder, GenerationResult generationResult)
        {
            foreach (var classToGenerate in generationResult.Classes)
            {
                if (!classToGenerate.HasPrivateFields)
                {
                    continue;
                }
                //namespace {nameSpace} {
                var closingCount = 0;
                if (!string.IsNullOrEmpty(classToGenerate.NameSpace))
                {
                    closingCount++;
                    strBuilder.Append("namespace ");
                    strBuilder.Append(classToGenerate.NameSpace);
                    strBuilder.AppendLine(" {");
                }
                //public partial class {typeName} {

                var dotIndex = 0;
                do
                {
                    closingCount++;
                    var nextDot = classToGenerate.Name.IndexOf(".", dotIndex, StringComparison.Ordinal);
                    strBuilder.Append("public partial class ");
                    if (nextDot >= 0)
                    {
                        strBuilder.Append(classToGenerate.Name.Substring(dotIndex, nextDot - dotIndex));
                        dotIndex = nextDot + 1;
                    }
                    else
                    {
                        strBuilder.Append(classToGenerate.Name.Substring(dotIndex));
                        dotIndex = 0;
                    }
                    strBuilder.AppendLine(" {");
                } while (dotIndex > 0);
                
                //    internal static void Sync(INeuroSync neuro, ref UberTestClass value) {
                strBuilder.Append("    internal static void Sync(_NeuroSyncNS.INeuroSync neuro, ref ");
                strBuilder.Append(classToGenerate.Name);
                strBuilder.AppendLine(" value) {");

                AppendConstructor(strBuilder, classToGenerate);
                AppendFields(strBuilder, classToGenerate);
                
                while (closingCount > 0)
                {
                    strBuilder.Append("}");
                    closingCount--;
                }
                strBuilder.AppendLine("}");
            }
        }

        void AppendConstructor(StringBuilder strBuilder, ClassToGenerate classToGenerate)
        {
            if (!classToGenerate.IsStructOrAbstract)
            {
                //     value ??= new {typeName}();
                // OR
                //     value ??= neuro.GetPooled<{typeName}>() ?? new {typeName}();
                strBuilder.Append("           value ??= ");
                if (classToGenerate.IsPoolable)
                {
                    strBuilder.Append("neuro.GetPooled<");
                    AppendFullClassName(strBuilder, classToGenerate);
                    strBuilder.Append(">() ?? ");
                }
                strBuilder.Append("new ");
                AppendFullClassName(strBuilder, classToGenerate);
                strBuilder.AppendLine("();");
            }
        }

        void AppendFields(StringBuilder strBuilder, ClassToGenerate classToGenerate)
        {
            foreach (var field in classToGenerate.Fields.OrderBy(f => f.Tag))
            {
                strBuilder.Append("           ");
                //neuro.SyncEnum({tag}, nameof(value.{fieldName}), ref value.{fieldName}, {defaultValue});
                // OR
                // neuro.Sync({tag}, nameof(value.{fieldName}), ref value.{fieldName}, {defaultValue});
                // OR
                // neuro.Sync({tag}, nameof(value.{fieldName}), ref value.{fieldName});
                
                if (field.IsEnum)
                {
                    strBuilder.Append("neuro.SyncEnum(");
                }
                else
                {
                    strBuilder.Append("neuro.Sync(");
                }
                strBuilder.Append(field.Tag);
                strBuilder.Append(", nameof(value.");
                strBuilder.Append(field.Name);
                strBuilder.Append(field.IsReadonly ? "), value." : "), ref value.");
                strBuilder.Append(field.Name);
                if (field.DefaultValue != null)
                {
                    strBuilder.Append(", ");
                    if (field.IsEnum)
                    {
                        strBuilder.Append("(int) ");
                    }
                    strBuilder.Append(field.DefaultValue);
                }
                strBuilder.AppendLine(");");
            }
        }

        static void AppendFullClassName(StringBuilder stringBuilder, ClassToGenerate classToGenerate)
        {
            if (!string.IsNullOrEmpty(classToGenerate.NameSpace))
            {
                stringBuilder.Append(classToGenerate.NameSpace);
                stringBuilder.Append(".");
            }
            stringBuilder.Append(classToGenerate.Name);
        }
    }
}
