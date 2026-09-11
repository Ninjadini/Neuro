using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    /// [ContextMenu] methods on the drawn object show when it is right-clicked, the way Unity's inspector shows
    /// them on a component. The nearest object's items are at the top level; every enclosing object that has
    /// items too adds them under a submenu named after its type, so the root's stay reachable from anywhere.
    public partial class ObjectInspector
    {
        static readonly Dictionary<Type, ContextMenuEntry[]> ContextMenuEntriesByType = new Dictionary<Type, ContextMenuEntry[]>();

        ContextualMenuManipulator _contextMenuManipulator;

        class ContextMenuEntry
        {
            public string Path;
            public MethodInfo Method;
            public MethodInfo Validate;
            public int Priority;
        }

        /// Only an object with [ContextMenu] methods takes the right-click, so one without them leaves the
        /// click to its enclosing object - and to any menu the host UI has of its own.
        void UpdateContextMenu(object obj)
        {
            var hasEntries = obj != null && GetContextMenuEntries(obj.GetType()).Length > 0;
            if (hasEntries && _contextMenuManipulator == null)
            {
                _contextMenuManipulator = new ContextualMenuManipulator(OnPopulateContextMenu);
                this.AddManipulator(_contextMenuManipulator);
            }
            else if (!hasEntries && _contextMenuManipulator != null)
            {
                this.RemoveManipulator(_contextMenuManipulator);
                _contextMenuManipulator = null;
            }
        }

        void OnPopulateContextMenu(ContextualMenuPopulateEvent evt)
        {
            string prefix;
            if (evt.target == this)
            {
                prefix = "";
            }
            else if (evt.target is ObjectInspector)
            {
                prefix = null; // decided below, once the object's type is known
            }
            else
            {
                return; // a field's own menu, e.g. a text box's cut / copy / paste
            }
            var obj = data.getter?.Invoke();
            if (obj == null)
            {
                return;
            }
            var entries = GetContextMenuEntries(obj.GetType());
            if (entries.Length == 0)
            {
                return;
            }
            prefix ??= GetClassName(obj.GetType()) + "/";
            if (evt.menu.MenuItems().Count > 0)
            {
                evt.menu.AppendSeparator();
            }
            var canEdit = data.Controller?.CanEdit(data.type, obj) ?? true;
            foreach (var entry in entries)
            {
                var enabled = canEdit && IsContextMenuEntryValid(entry, obj);
                evt.menu.AppendAction(prefix + entry.Path,
                    _ => InvokeContextMenu(entry),
                    enabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
        }

        static bool IsContextMenuEntryValid(ContextMenuEntry entry, object obj)
        {
            if (entry.Validate == null)
            {
                return true;
            }
            try
            {
                return (bool)entry.Validate.Invoke(obj, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e is TargetInvocationException { InnerException: not null } ? e.InnerException : e);
                return false;
            }
        }

        void InvokeContextMenu(ContextMenuEntry entry)
        {
            // Fetched again rather than kept from when the menu opened - a struct is a fresh boxed copy each
            // time, and the one that gets written back must be the one the method ran on.
            var obj = data.getter?.Invoke();
            if (obj == null)
            {
                return;
            }
            try
            {
                entry.Method.Invoke(obj, null);
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException ?? e);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            // Reported even after an exception: the method may have changed part of the object before it threw.
            if (obj.GetType().IsValueType)
            {
                data.SetValue(obj); // the method ran on a copy, so it has to be written back to the field
            }
            else
            {
                data.Controller?.OnValueChanged(obj);
            }
            ForceRedraw();
        }

        /// Instance methods with no parameters, from the type and its base classes - Unity's own rules for
        /// [ContextMenu]. An override shows once, under its most derived declaration. Validate functions
        /// ([ContextMenu("Path", true)], returning bool) grey the item out when they return false.
        static ContextMenuEntry[] GetContextMenuEntries(Type type)
        {
            if (ContextMenuEntriesByType.TryGetValue(type, out var result))
            {
                return result;
            }
            var hierarchy = new List<Type>();
            for (var t = type; t != null && t != typeof(object) && t != typeof(ValueType); t = t.BaseType)
            {
                hierarchy.Add(t);
            }
            var entries = new List<ContextMenuEntry>();
            var validators = new Dictionary<string, MethodInfo>();
            // base first so a base class's items come before the subclass's, as they do in Unity.
            for (var i = hierarchy.Count - 1; i >= 0; i--)
            {
                const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (var method in hierarchy[i].GetMethods(Flags))
                {
                    if (method.GetParameters().Length > 0 || method.ContainsGenericParameters)
                    {
                        continue;
                    }
                    foreach (var attribute in method.GetCustomAttributes<ContextMenu>(false))
                    {
                        if (attribute.validate)
                        {
                            if (method.ReturnType == typeof(bool))
                            {
                                validators[attribute.menuItem] = method;
                            }
                            continue;
                        }
                        var existing = entries.FindIndex(e => e.Path == attribute.menuItem);
                        var entry = new ContextMenuEntry()
                        {
                            Path = attribute.menuItem,
                            Method = method,
                            Priority = attribute.priority
                        };
                        if (existing >= 0)
                        {
                            entries[existing] = entry;
                        }
                        else
                        {
                            entries.Add(entry);
                        }
                    }
                }
            }
            foreach (var entry in entries)
            {
                validators.TryGetValue(entry.Path, out entry.Validate);
            }
            // OrderBy is stable, so equal priorities keep their declaration order.
            result = entries.OrderBy(e => e.Priority).ToArray();
            ContextMenuEntriesByType[type] = result;
            return result;
        }
    }
}
