using System;
using System.Collections.Generic;
using System.Reflection;
using Ninjadini.Neuro.Sync;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroEditorRefLinkItemsElement : VisualElement
    {
        NeuroEditorDataProvider _dataProvider;
        IReferencable _drawnValue;

        Dictionary<Type, List<(Type, LinkedReferenceAttribute)>> links;

        public void Draw(NeuroEditorDataProvider dataProvider, Type type, IReferencable value)
        {
            _dataProvider = dataProvider;
            Clear();
            if (value == null)
            {
                return;
            }

            EnsureLinksCached();
            _drawnValue = value;
            
            style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            style.marginBottom = style.marginLeft = style.marginRight = style.marginTop = 2f;
            style.paddingLeft = style.paddingRight = 3f;
            style.borderBottomLeftRadius = style.borderBottomRightRadius =
                style.borderTopLeftRadius = style.borderBottomRightRadius = 5f;


            if (links.TryGetValue(type, out var data))
            {
                foreach (var pair in data)
                {
                    var mainType = pair.Item1;
                    var attribute = pair.Item2;
                    
                    var otherType = mainType == type ? attribute.To : mainType;
                    
                    var group = NeuroUiUtils.AddHorizontal(this);
                    group.style.paddingRight = 20;
                    
                    var otherName = mainType == type
                        ? (string.IsNullOrEmpty(attribute.ToName) ? attribute.To.Name : attribute.ToName)
                        : ((string.IsNullOrEmpty(attribute.FromName) ? mainType.Name : attribute.FromName));
                    
                    NeuroUiUtils.AddLabel(group, otherName).style.top = 2;
                    if (IsValidLinkType(mainType) && IsValidLinkType(attribute.To))
                    {
                        var exists = _dataProvider.References.Get(otherType, _drawnValue.RefId) != null;
                        var direction = otherType == mainType ? "<" : ">";
                        var btn = NeuroUiUtils.AddButton(group, exists ? direction: "+", () => LinkClicked(otherType));
                        btn.style.width = 40;
                    }
                    else
                    {
                        NeuroUiUtils.AddLabel(group,"[invalid]").style.top = 2;;
                    }
                }
            }
        }

        void EnsureLinksCached()
        {
            if (links != null)
            {
                return;
            }
            links = new Dictionary<Type, List<(Type, LinkedReferenceAttribute)>>();
            foreach (var type in NeuroGlobalTypes.GetAllRootTypes())
            {
                foreach (var linkedReferenceAttribute in type.GetCustomAttributes<LinkedReferenceAttribute>())
                {
                    if (linkedReferenceAttribute.To != null)
                    {
                        AddType(type, linkedReferenceAttribute.To, linkedReferenceAttribute);
                    }
                }
            }
        }

        void AddType(Type mainType, Type linkedType, LinkedReferenceAttribute attribute)
        {
            if (mainType == linkedType)
            {
                return;
            }
            if (!links.TryGetValue(mainType, out var data))
            {
                data = new List<(Type, LinkedReferenceAttribute)>();
                links.Add(mainType, data);
            }
            data.Add((mainType, attribute));
            if (!links.TryGetValue(linkedType, out var linkedData))
            {
                linkedData = new List<(Type, LinkedReferenceAttribute)>();
                links.Add(linkedType, linkedData);
            }
            linkedData.Add((mainType, attribute));
        }

        bool IsValidLinkType(Type type)
        {
            return !typeof(ISingletonReferencable).IsAssignableFrom(type) && typeof(IReferencable).IsAssignableFrom(type) && (type.BaseType == typeof(Referencable) || type.BaseType == null || !typeof(IReferencable).IsAssignableFrom(type.BaseType));
        }

        void LinkClicked(Type otherType)
        {
            var refId = _drawnValue.RefId;
            if(_dataProvider.References.Get(otherType, _drawnValue.RefId) == null)
            {
                var obj = Activator.CreateInstance(otherType) as IReferencable;
                _dataProvider.Add(obj, refId);
            }
            var loopParent = parent;
            while (loopParent != null)
            {
                if (loopParent is NeuroEditorNavElement navElement)
                {
                    navElement.SetSelectedItem(otherType, refId);
                    break;
                }
                loopParent = loopParent.parent;
            }
        }
    }
}