using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Ninjadini.Neuro.Editor
{
    internal class AssetAddressFieldElement : VisualElement
    {
        ObjectInspector.Data data;
        ObjectField objectField;
        Button clearBtn;
        string drawnAddress;
        
        internal AssetAddressFieldElement(ObjectInspector.Data data_)
        {
            data = data_;
            objectField = new ObjectField(data.name);
            var type = AssetAddressEditorUtils.TryGetAssetType(data.MemberInfo);
            if (type != null)
            {
                objectField.objectType = type;
            }
            objectField.RegisterValueChangedCallback(OnObjFieldValueChanged);
            Add(objectField);
            schedule.Execute(OnUpdate).Every(ObjectInspectorFields.RefreshRate);
            OnUpdate();
        }

        void OnObjFieldValueChanged(ChangeEvent<Object> evt)
        {
            var obj = evt.newValue;
            var result = new AssetAddress();
            if (obj)
            {
                if (!AssetAddressEditorUtils.PrepObjectLinkable(obj))
                {
                    return;
                }
                drawnAddress = AssetAddressEditorUtils.GetAddress(obj);
            }
            else
            {
                drawnAddress = null;
            }
            UpdateValidityDisplay(obj);
            result.Address = drawnAddress;
            data.SetValue(result);
        }

        void OnUpdate()
        {
            var valueObj = data.getter();
            var value = valueObj != null ? (AssetAddress)valueObj : default;
            if (drawnAddress != value.Address)
            {
                drawnAddress = value.Address;
                var obj = AssetAddressEditorUtils.LoadObjectFromAddress(value.Address);
                objectField.SetValueWithoutNotify(obj);
                UpdateValidityDisplay(obj);
            }
        }

        void UpdateValidityDisplay(Object obj)
        {
            if (!string.IsNullOrEmpty(drawnAddress) && !obj)
            {
                objectField.labelElement.style.color = Color.red;
                objectField.label = data.name + " (Invalid)";
                if (clearBtn == null)
                {
                    clearBtn = new Button(() =>
                    {
                        objectField.SetValueWithoutNotify(null);
                        data.SetValue(new AssetAddress());
                    });
                    clearBtn.style.position = Position.Absolute;
                    clearBtn.style.right = 20;
                    clearBtn.text = "Clear";
                    Add(clearBtn);
                }
                else if (clearBtn.parent == null)
                {
                    Add(clearBtn);
                }
            }
            else
            {
                if (clearBtn != null && clearBtn.parent != null)
                {
                    clearBtn.RemoveFromHierarchy();
                }
                objectField.label = data.name;
                objectField.labelElement.style.color = new StyleColor(StyleKeyword.Null);
            }
        }
    }
}