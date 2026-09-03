using System;

namespace Ninjadini.Neuro.Sync
{
    internal static class NeuroSyncErrors
    {
        /// Top level read/write only works with 'child' types - a class or a struct with [Neuro] fields.
        /// Primitives (int, string, float, DateTime, ...) and collections have no header of their own so
        /// there is nothing to read them back with. Silently writing them would produce unreadable data.
        internal static Exception NotAStandaloneType(Type type, string action)
        {
            return new Exception(
                $"Can not {action} `{type}` on its own - it is not a neuro object (a class or struct with [Neuro] fields)." +
                $"\nPut it in a field of a neuro object and {action} that object instead, e.g:" +
                $"\n[Neuro(1)] public {type.Name} Value;");
        }

        internal static Exception UnexpectedSubType(Type requestedType, Type actualType)
        {
            return new Exception(
                $"The data is of type `{actualType}` which is not a `{requestedType}`." +
                $"\nRead it as the base type they share (or as `{actualType}`) instead.");
        }

        internal static Exception NotANeuroObjectHeader(Type type, uint sizeType)
        {
            return new Exception(
                $"The data does not start with a neuro object header (got field size type {sizeType}), can not read it as `{type}`." +
                "\nIt may have been written by `WriteGlobalTyped()` - that needs `ReadGlobalTyped()` to read it back.");
        }

        internal static Exception NotGlobalTypedData(string inputName, string readCall)
        {
            return new Exception(
                $"The {inputName} do not carry a global type id, so `ReadGlobalTyped()` can not work out the type." +
                $"\nOnly `WriteGlobalTyped()` output can be read this way - for `Write()` / `WriteObject()` output use `{readCall}`.");
        }

        internal static Exception UnknownSubTypeTag(Type rootType, uint tag)
        {
            return new Exception(
                $"Sub type tag `{tag}` of base type `{rootType}` is not registered." +
                "\nThe data was likely written by a newer version that has a sub class this build doesn't know about.");
        }
    }
}
