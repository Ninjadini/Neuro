using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninjadini.Neuro.Sync;
using Ninjadini.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroObjectInspector : ObjectInspector, ObjectInspector.IController
    {
        public bool AllowIdChange;
        public readonly NeuroReferences References;

        public Action AnyValueChanged;

        IController subController;
        NeuroBytesWriter writer;
        NeuroBytesReader reader;

        object drawnObj;
        uint idBefore;

        static readonly Type NeuroAttribute = typeof(NeuroAttribute);

        public object DrawnObj => drawnObj;

        public NeuroObjectInspector(NeuroReferences references)
        {
            References = references;
        }

        public override void Draw(Data drawData)
        {
            subController = drawData.Controller;
            drawData.Controller = this;
            if (reader == null)
            {
                writer = new NeuroBytesWriter();
                reader = new NeuroBytesReader();
            }

            drawnObj = drawData.getter();
            if (drawnObj is IReferencable referencable)
            {
                idBefore = referencable.RefId;
                writer.Write((object)drawnObj);
            }

            base.Draw(drawData);
            
            if (fieldsParent.childCount == 0)
            {
                NeuroUiUtils.AddLabel(fieldsParent, "No fields to draw.\nIf this object is a custom neuro object without [Neuro] attributes, you can register the fields via: "+nameof(NeuroCustomEditorFieldRegistry));
            }
        }

        bool IController.ShouldDrawField(FieldInfo fieldInfo, object holderObject)
        {
            var isNeuroField = fieldInfo.IsDefined(NeuroAttribute) || NeuroCustomEditorFieldRegistry.IsNameCustomField(holderObject.GetType(), fieldInfo.Name);
            return isNeuroField &&
                   (subController?.ShouldDrawField(fieldInfo, holderObject) ?? true);
        }

        bool IController.ShouldDrawProperty(PropertyInfo propertyInfo, object holderObject)
        {
            return NeuroCustomEditorFieldRegistry.IsNameCustomField(holderObject.GetType(), propertyInfo.Name) && (subController?.ShouldDrawProperty(propertyInfo, holderObject) ?? true);
        }

        VisualElement IController.CreateCustomHeader(Data data, object value)
        {
            var custom = subController?.CreateCustomHeader(data, value);
            if (custom != null)
            {
                return custom;
            }
            foreach (var customProvider in CustomProviders)
            {
                custom = customProvider.CreateCustomHeader(this, data, value);
                if (custom != null)
                {
                    return custom;
                }
            }
            return null;
        }

        VisualElement IController.CreateCustomDrawer(Data data)
        {
            var custom = subController?.CreateCustomDrawer(data);
            if (custom != null)
            {
                return custom;
            }
            foreach (var customProvider in CustomProviders)
            {
                custom = customProvider.CreateCustomDrawer(this, data);
                if (custom != null)
                {
                    return custom;
                }
            }
            return null;
        }
        bool IController.CanEdit(Type type, object value) => subController?.CanEdit(type, value) ?? true;
        bool IController.CanSetToNull(Type type, object value) => subController?.CanSetToNull(type, value) ?? true;
        bool IController.CanCreateObject(Type type) => subController?.CanCreateObject(type) ?? true;

        Type[] IController.GetPossibleCreationTypesOf(Type type)
        {
            if (type == typeof(object))
            {
                return NeuroGlobalTypes.GetAllRootTypes().ToArray();
            }
            var typeIsClass = type.IsClass;
            // https://stackoverflow.com/questions/857705/get-all-derived-types-of-a-type
            return (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                where !domainAssembly.IsDynamic && domainAssembly.IsDefined(typeof(NeuroAssemblyAttribute))
                from assemblyType in domainAssembly.GetExportedTypes()
                where assemblyType.IsClass 
                      && (assemblyType == type || (typeIsClass ? assemblyType.IsSubclassOf(type) : type.IsAssignableFrom(assemblyType)))
                      && NeuroSyncTypes.CheckIfTypeRegisteredUsingReflection(assemblyType)
                      && !assemblyType.IsDefined(typeof(HideInInspector))
                select assemblyType).ToArray();
        }

        object IController.SwitchObjectType(object originalObject, Type newType)
        {
            if (originalObject != null)
            {
                try
                {
                    return SwitchObjectTypeWhileKeepingValues(originalObject, newType, References);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return null;
        }

        void IController.OnValueChanged(object holderObject)
        {
            if (drawnObj is IReferencable referencable)
            {
                var idNow = referencable.RefId;
                if (!AllowIdChange && idBefore != idNow)
                {
                    ShowRefIdChangedError(idBefore, idNow);
                    reader.Read(writer.GetCurrentBytesChunk(), ref drawnObj);
                    return;
                }

                writer.Write(drawnObj);
                idBefore = idNow;
            }

            AnyValueChanged?.Invoke();
            subController?.OnValueChanged(holderObject);
        }

        public static void ShowRefIdChangedError(uint idBefore, uint idAfter)
        {
            EditorUtility.DisplayDialog("",
                $"RefId was changed from {idBefore} to {idAfter} but this is not allowed (yet).", "OK");
        }

        public static object SwitchObjectTypeWhileKeepingValues(object originalObject, Type newType, NeuroReferences references = null)
        {
            var newObj = Activator.CreateInstance(newType);
            if (NeuroReferences.GetRootReferencable(originalObject.GetType()) != NeuroReferences.GetRootReferencable(newType))
            {
                return newObj;
            }
            var json = new NeuroJsonWriter().Write(originalObject, references, NeuroJsonWriter.Options.TagValuesOnly);
            var jsonVisitor = new NeuroJsonTokenizer();
            var nodes = jsonVisitor.Visit(json);
            var currentParent = nodes.Array[0].Parent;
            var subTypeNode = nodes.Array.FirstOrDefault(node =>
                node.Parent == currentParent &&
                NeuroJsonTokenizer.StringRange.Equals(node.Key, json, NeuroJsonWriter.FieldName_ClassTag));
            var typeId = NeuroGlobalTypes.GetSubTypeTag(newType);
            if (subTypeNode.Type != NeuroJsonTokenizer.NodeType.Unknown)
            {
                json = json.Substring(0, subTypeNode.Value.Start) 
                       + typeId
                       +json.Substring(subTypeNode.Value.End);
            }
            new NeuroJsonReader().Read(json, ref newObj);
            return newObj;
        }


        static ICustomNeuroEditorProvider[] _customProviders;
        public static IReadOnlyList<ICustomNeuroEditorProvider> CustomProviders
        {
            get
            {
                if (_customProviders == null)
                {
                    _customProviders = NeuroEditorUtils.CreateFromScannableTypes<ICustomNeuroEditorProvider>()
                        .OrderByDescending(c => c.Priority)
                        .ToArray();
                }
                return _customProviders;
            }
        }
    }
}