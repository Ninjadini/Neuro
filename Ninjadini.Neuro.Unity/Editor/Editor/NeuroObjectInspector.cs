using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroObjectInspector : ObjectInspector, ObjectInspector.IController
    {
        public bool AllowIdChange;
        public readonly NeuroReferences References;
        public readonly NeuroEditorHistory _history;

        public Action AnyValueChanged;

        ICustomNeuroObjectInspectorController neuroController;
        NeuroBytesWriter writer;
        NeuroBytesReader reader;

        object drawnObj;
        uint idBefore;

        static readonly Type NeuroAttribute = typeof(NeuroAttribute);

        public object DrawnObj => drawnObj;

        public NeuroObjectInspector(NeuroReferences references, NeuroEditorHistory history = null)
        {
            References = references;
            _history = history;
        }

        public override void Draw(Data drawData)
        {
            neuroController = SharedNeuroController;
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
                try
                {
                    writer.WriteObject((object)drawnObj);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }

            base.Draw(drawData);
            
            if (fieldsParent.childCount == 0)
            {
                NeuroUiUtils.AddLabel(fieldsParent, "No fields to draw.\nIf this object is a custom neuro object without [Neuro] attributes, you can register the fields via: "+nameof(NeuroCustomEditorFieldRegistry));
            }
        }

        string IController.GetDisplayName(Data data) => neuroController.GetDisplayName(data);

        bool IController.ShouldDrawField(FieldInfo fieldInfo, object holderObject)
        {
            var isNeuroField = fieldInfo.IsDefined(NeuroAttribute) || NeuroCustomEditorFieldRegistry.IsNameCustomField(holderObject.GetType(), fieldInfo.Name);
            if (isNeuroField)
            {
                return neuroController.ShouldDrawField(fieldInfo, holderObject);
            }
            return neuroController.ShouldDrawNonNeuroField(fieldInfo, holderObject);
        }

        bool IController.ShouldDrawProperty(PropertyInfo propertyInfo, object holderObject)
        {
            var isNeuroField = NeuroCustomEditorFieldRegistry.IsNameCustomField(holderObject.GetType(), propertyInfo.Name);
            if (isNeuroField)
            {
                return neuroController.ShouldDrawProperty(propertyInfo, holderObject);
            }
            return neuroController.ShouldDrawNonNeuroProperty(propertyInfo, holderObject);
        }

        VisualElement IController.CreateCustomHeader(Data data, object value)
        {
            var custom = neuroController?.CreateCustomHeader(data, value);
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
        
        VisualElement IController.CreateCustomFieldHeader(Data data) => neuroController.CreateCustomFieldHeader(data);

        VisualElement IController.CreateCustomDrawer(Data data)
        {
            var custom = neuroController?.CreateCustomDrawer(data);
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
        bool IController.CanEdit(Type type, object value) => neuroController?.CanEdit(type, value) ?? true;
        bool IController.CanSetToNull(Type type, object value)
        {
            if (value == drawnObj)
            {
                return false;
            }
            return neuroController?.CanSetToNull(type, value) ?? true;
        }

        bool IController.CanCreateObject(Type type) => neuroController?.CanCreateObject(type) ?? true;

        Type[] IController.GetPossibleCreationTypesOf(Type type)
        {
            return neuroController.GetPossibleCreationTypesOf(type) ?? GetPossibleCreationTypesOf(type);
        }

        void IController.SwitchObjectType(object originalObject, Type newType, ref object newObject)
        {
            if (originalObject != null)
            {
                try
                {
                    neuroController.SwitchObjectType(originalObject, newType, ref newObject);
                    if (newObject == null)
                    {
                        newObject = SwitchObjectTypeWhileKeepingValues(originalObject, newType, References);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        void IController.ApplyStyle(Data data, VisualElement element)
        {
            neuroController.ApplyStyle(data, element);
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
                try
                {
                    writer.WriteObject(drawnObj);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
                idBefore = idNow;
            }

            AnyValueChanged?.Invoke();
            neuroController.OnValueChanged(holderObject);
        }
        
        bool IController.ShouldAddFoldOut(Data data, object value) => value != drawnObj && neuroController.ShouldAddFoldOut(data, value);
        
        bool IController.ShouldAutoExpandFoldout(Type type) => neuroController.ShouldAutoExpandFoldout(type);
        
        void IController.CreateObject(Type type, VisualElement fromElement, Action<object> resultCallback)
        {
            neuroController.CreateObject(type, fromElement, resultCallback);
        }

        NeuroEditorHistory IController.History => _history;

        public static void ShowRefIdChangedError(uint idBefore, uint idAfter)
        {
            EditorUtility.DisplayDialog("", $"RefId was changed from {idBefore} to {idAfter} but this is not allowed (yet).", "OK");
        }

        public static Type[] GetPossibleCreationTypesOf(Type type)
        {
            if (type == typeof(object))
            {
                return NeuroGlobalTypes.GetAllRootTypes().ToArray();
            }
            var typeIsClass = type.IsClass;
            var result = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                where !domainAssembly.IsDynamic && domainAssembly.IsDefined(typeof(NeuroAssemblyAttribute))
                from assemblyType in domainAssembly.GetExportedTypes()
                where assemblyType.IsClass
                      && !assemblyType.IsAbstract
                      && !assemblyType.IsInterface
                      && (assemblyType == type || (typeIsClass ? assemblyType.IsSubclassOf(type) : type.IsAssignableFrom(assemblyType)))
                      && !assemblyType.IsDefined(typeof(HideInInspector))
                      && NeuroSyncTypes.CheckIfTypeRegisteredUsingReflection(assemblyType)
                select assemblyType).ToList();
            if (typeIsClass && !type.IsAbstract && !type.IsInterface && !result.Contains(type))
            {
                result.Insert(0, type);
            }
            return result.ToArray();
        }

        public static object SwitchObjectTypeWhileKeepingValues(object originalObject, Type newType, NeuroReferences references = null)
        {
            var newObj = Activator.CreateInstance(newType);
            if (NeuroReferences.GetRootReferencable(originalObject.GetType()) != NeuroReferences.GetRootReferencable(newType))
            {
                return newObj;
            }
            var json = new NeuroJsonWriter().WriteObject(originalObject, references, NeuroJsonWriter.Options.TagValuesOnly);
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
            new NeuroJsonReader().ReadObject(json, newObj.GetType(), ref newObj);
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

        static ICustomNeuroObjectInspectorController _sharedNeuroController;
        public static ICustomNeuroObjectInspectorController SharedNeuroController
        {
            get
            {
                if(_sharedNeuroController == null)
                {
                    var allControllers = NeuroEditorUtils.CreateFromScannableTypes<ICustomNeuroObjectInspectorController>();
                    _sharedNeuroController = allControllers.OrderByDescending(c => c.Priority).FirstOrDefault();
                    if (_sharedNeuroController == null)
                    {
                        _sharedNeuroController = new BasicNeuroController();
                    }
                }
                return _sharedNeuroController;
            }
        }
        
        class BasicNeuroController : ICustomNeuroObjectInspectorController
        {
            
        }
    }
}