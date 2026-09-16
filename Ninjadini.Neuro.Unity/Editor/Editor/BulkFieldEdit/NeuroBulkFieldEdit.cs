using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ninjadini.Neuro.Editor
{
    /// One number field, changed the same way on every item of a table - "every creep's health, times 1.2".
    /// Reached by right clicking the field in the editor; see <see cref="NeuroBulkFieldEditMenu"/> for the menu
    /// and <see cref="NeuroFieldPath"/> for how the clicked field is found again on the other items.
    ///
    /// The walk is <see cref="NeuroEditVisitor"/> over each item, so what counts as a number is whatever Neuro
    /// serializes as one, at the place the inspector draws it. That matters for a type like Toolkit's `fp`: it
    /// is visited as an `fp`, so the arithmetic happens in fixed point and lands back quantised the same way
    /// saving the file would quantise it, rather than being pulled apart into the raw integer underneath.
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public static class NeuroBulkFieldEdit
    {
        public enum Operation
        {
            Multiply,
            Add,
            Set
        }

        /// Teaches the bulk edit about a number type of your own - the pair is the same conversion the field's
        /// custom drawer already does to show it in a float box. Register it from an
        /// `INeuroCustomTypesRegistryHook`, next to the type's `NeuroSyncTypes.Register`.
        ///
        /// The value goes through `double` on the way in and out, so a type finer grained than a double loses
        /// the difference - the same loss as typing the number into the field by hand.
        public static void Register<T>(Func<T, double> toNumber, Func<double, T> fromNumber)
        {
            if (toNumber == null) throw new ArgumentNullException(nameof(toNumber));
            if (fromNumber == null) throw new ArgumentNullException(nameof(fromNumber));
            Number<T>.ToNumber = toNumber;
            Number<T>.FromNumber = fromNumber;
            Converters[typeof(T)] = new Converter<T>(toNumber, fromNumber);
        }

        public static bool IsNumber(Type type) => type != null && Converters.ContainsKey(type);

        /// The number a drawn value holds, for previewing what an operation would do to it.
        public static bool TryGetNumber(Type type, object value, out double number)
        {
            if (value != null && type != null && Converters.TryGetValue(type, out var converter))
            {
                number = converter.To(value);
                return true;
            }
            number = 0;
            return false;
        }

        /// What that number would land as once the type has had its say - an int rounds, an `fp` quantises.
        public static object ToValue(Type type, double number)
        {
            return type != null && Converters.TryGetValue(type, out var converter) ? converter.From(number) : null;
        }

        static readonly Dictionary<Type, Converter> Converters = new Dictionary<Type, Converter>();

        /// The same conversion as <see cref="Number{T}"/>, reachable without knowing T at compile time.
        abstract class Converter
        {
            public abstract double To(object value);
            public abstract object From(double number);
        }

        class Converter<T> : Converter
        {
            readonly Func<T, double> toNumber;
            readonly Func<double, T> fromNumber;

            public Converter(Func<T, double> toNumber, Func<double, T> fromNumber)
            {
                this.toNumber = toNumber;
                this.fromNumber = fromNumber;
            }

            public override double To(object value) => toNumber((T)value);
            public override object From(double number) => fromNumber(number);
        }

#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        static class Number<T>
        {
            internal static Func<T, double> ToNumber;
            internal static Func<double, T> FromNumber;
        }

        static NeuroBulkFieldEdit()
        {
            Register<int>(v => v, d => (int)ClampRound(d, int.MinValue, int.MaxValue));
            Register<uint>(v => v, d => (uint)ClampRound(d, uint.MinValue, uint.MaxValue));
            Register<long>(v => v, d => (long)ClampRound(d, long.MinValue, long.MaxValue));
            Register<ulong>(v => v, d => (ulong)ClampRound(d, ulong.MinValue, ulong.MaxValue));
            Register<short>(v => v, d => (short)ClampRound(d, short.MinValue, short.MaxValue));
            Register<ushort>(v => v, d => (ushort)ClampRound(d, ushort.MinValue, ushort.MaxValue));
            Register<byte>(v => v, d => (byte)ClampRound(d, byte.MinValue, byte.MaxValue));
            Register<sbyte>(v => v, d => (sbyte)ClampRound(d, sbyte.MinValue, sbyte.MaxValue));
            Register<float>(v => v, d => (float)d);
            Register<double>(v => v, d => d);
        }

        /// An integer takes the nearest whole number rather than the truncation a plain cast would do - x1.5 on
        /// a 5 is a 8, not a 7 - and stops at the ends of its range instead of wrapping around to a negative.
        static double ClampRound(double value, double min, double max)
        {
            if (double.IsNaN(value))
            {
                return 0;
            }
            return Math.Max(min, Math.Min(max, Math.Round(value, MidpointRounding.AwayFromZero)));
        }

        public static double Calculate(double current, Operation operation, double operand)
        {
            switch (operation)
            {
                case Operation.Multiply: return current * operand;
                case Operation.Add: return current + operand;
                case Operation.Set: return operand;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }

        public struct Result
        {
            /// How many individual numbers were changed. More than one per item when the path runs through a list.
            public int Fields;
            public List<IReferencable> Items;

            public int ItemCount => Items?.Count ?? 0;
        }

        /// Runs `operation` on `path` in every item of `rootType`'s table.
        ///
        /// With `dryRun` nothing is written and nothing is saved - it just counts what a real run would touch,
        /// which is what the confirmation is built from. A real run saves every item it changed and records one
        /// undo entry covering all of them.
        public static Result Apply(NeuroEditorDataProvider provider, Type rootType, NeuroFieldPath path,
            Operation operation, double operand, bool dryRun, EditorWindow window = null)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (rootType == null) throw new ArgumentNullException(nameof(rootType));
            if (path == null) throw new ArgumentNullException(nameof(path));

            var result = new Result() { Items = new List<IReferencable>() };
            // ToArray because visiting deserializes the lazily loaded items, which writes to the very table we
            // would otherwise still be enumerating.
            var items = provider.References.GetTable(rootType).SelectAll().ToArray();
            var changedFiles = dryRun ? null : new List<NeuroDataFile>();
            var visitor = new BulkEditVisitor(path, operation, operand, dryRun);
            var walk = new NeuroEditVisitor();
            foreach (var item in items)
            {
                var dataFile = provider.Find(item);
                if (dataFile == null)
                {
                    // No file to save into - an item that only exists in memory is left alone rather than
                    // changed where the change could not be kept. Skipped in a dry run too, so the count the
                    // user is shown is the count they will get.
                    continue;
                }
                if (!dryRun)
                {
                    // The "before" of the undo entry. Most of these have never been drawn, so nothing else has
                    // taken a snapshot of them.
                    NeuroEditorUndoRedos.Snapshot(dataFile);
                }
                visitor.Reset();
                try
                {
                    walk.Visit(item, visitor, true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Neuro ~ bulk edit could not walk {rootType.Name} " +
                                   $"{NeuroEditorUtils.DisplayRefId(item.RefId)}: {e.Message}\n{e}");
                    continue;
                }
                if (visitor.Changes == 0)
                {
                    continue;
                }
                result.Fields += visitor.Changes;
                result.Items.Add(item);
                if (!dryRun)
                {
                    provider.SaveData(dataFile);
                    changedFiles.Add(dataFile);
                }
            }
            if (!dryRun && changedFiles.Count > 0)
            {
                NeuroEditorUndoRedos.RecordChanges(changedFiles, DescribeOperation(operation, operand), window);
            }
            return result;
        }

        public static string DescribeOperation(Operation operation, double operand)
        {
            switch (operation)
            {
                case Operation.Multiply: return $"× {operand:0.####}";
                case Operation.Add: return operand < 0 ? $"− {Math.Abs(operand):0.####}" : $"+ {operand:0.####}";
                case Operation.Set: return $"= {operand:0.####}";
                default: return operation.ToString();
            }
        }

        /// Finds the one field wherever it turns up in an item and changes it. The visitor hands out a writable
        /// ref all the way down, so a number inside a struct inside a list lands back where it came from -
        /// which reflection over boxed struct copies would not.
        class BulkEditVisitor : NeuroEditVisitor.IInterface
        {
            readonly NeuroFieldPath path;
            readonly Operation operation;
            readonly double operand;
            readonly bool dryRun;
            readonly List<NeuroFieldPath.Step> stack = new List<NeuroFieldPath.Step>();

            public int Changes;

            public BulkEditVisitor(NeuroFieldPath path, Operation operation, double operand, bool dryRun)
            {
                this.path = path;
                this.operation = operation;
                this.operand = operand;
                this.dryRun = dryRun;
            }

            public void Reset()
            {
                Changes = 0;
                stack.Clear();
            }

            void NeuroEditVisitor.IInterface.BeginVisit<T>(ref T obj, string name, int? listIndex)
            {
                stack.Add(new NeuroFieldPath.Step(name, listIndex ?? -1));
                if (Number<T>.ToNumber == null || !path.Matches(stack))
                {
                    return;
                }
                var newValue = Calculate(Number<T>.ToNumber(obj), operation, operand);
                if (!dryRun)
                {
                    obj = Number<T>.FromNumber(newValue);
                }
                Changes++;
            }

            void NeuroEditVisitor.IInterface.EndVisit()
            {
                stack.RemoveAt(stack.Count - 1);
            }

            void NeuroEditVisitor.IInterface.VisitRef<T>(ref Reference<T> reference)
            {
            }
        }
    }
}
