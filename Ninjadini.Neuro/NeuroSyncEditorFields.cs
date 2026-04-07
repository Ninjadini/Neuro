using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Runtime-callable registry for declaring fields/properties that should be drawn in the Neuro editor
    /// for types that don't have [Neuro] attributes (e.g. third-party structs registered via a custom
    /// INeuroCustomTypesRegistryHook). The actual editor-side wiring is plugged in by the editor assembly
    /// via <see cref="SetEditorHook"/>; runtime calls made before the hook is set are queued and replayed.
    ///
    /// AddField/AddProperty are marked [Conditional("UNITY_EDITOR")], so calls to them are stripped from
    /// player builds at compile time — there is no runtime cost outside the editor.
    /// </summary>
    public static class NeuroSyncEditorFields
    {
#if UNITY_EDITOR
        public delegate void EditorRegisterDelegate(Type type, string memberName, bool isProperty);
        static List<(Type type, string name, bool isProperty)> _pending;
        static EditorRegisterDelegate _hook;
#endif

        [Conditional("UNITY_EDITOR")]
        public static void AddField(Type type, string fieldName) => Add(type, fieldName, false);

        [Conditional("UNITY_EDITOR")]
        public static void AddProperty(Type type, string propertyName) => Add(type, propertyName, true);

        static void Add(Type type, string name, bool isProperty)
        {
#if UNITY_EDITOR
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (_hook != null)
            {
                _hook(type, name, isProperty);
                return;
            }
            (_pending ??= new List<(Type, string, bool)>()).Add((type, name, isProperty));
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called by the editor assembly to install the actual registration sink.
        /// Any entries added before this call are flushed to the hook immediately.
        /// </summary>
        public static void SetEditorHook(EditorRegisterDelegate hook)
        {
            _hook = hook;
            if (hook == null || _pending == null) return;
            foreach (var entry in _pending)
            {
                hook(entry.type, entry.name, entry.isProperty);
            }
            _pending = null;
        }
#endif
    }
}
