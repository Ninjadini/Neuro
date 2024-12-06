using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    internal class NeuroEditorClassSettingsElement : VisualElement, ObjectInspector.IController
    { 
        ObjectInspector classSettingPanel;
        Type type;
        NeuroEditorTypeItemSetting setting;
        
        public readonly Button ToggleButton;
        
        public NeuroEditorClassSettingsElement()
        {
            ToggleButton = new Button();
            ToggleButton.text = "▼";
            ToggleButton.style.width = 30;
            ToggleButton.tooltip = "Type settings";
            style.flexShrink = 0f;
        }
        
        public void Toggle(Type selectedType)
        {
            if (classSettingPanel != null)
            {
                classSettingPanel.RemoveFromHierarchy();
                classSettingPanel = null;
                ToggleButton.text = "▼";
            }
            else
            {
                classSettingPanel = new ObjectInspector();
                classSettingPanel.style.paddingLeft = style.paddingRight = 10;
                classSettingPanel.style.paddingBottom = 10;
                UpdateClassSettingPanel(selectedType);
                ToggleButton.text = "▲";
                Add(classSettingPanel);
            }
        }

        public void UpdateClassSettingPanel(Type selectedType)
        {
            type = selectedType;
            if (classSettingPanel != null)
            {
                setting = NeuroUnityEditorSettings.Get().FindTypeSetting(selectedType);
                if (setting == null)
                {
                    setting = new NeuroEditorTypeItemSetting();
                    setting.SetDefaults(selectedType);
                }
                classSettingPanel.Draw(typeof(NeuroEditorTypeItemSetting), setting, controller: this);
            }
        }

        bool ObjectInspector.IController.ShouldDrawField(FieldInfo fieldInfo, object holderObject)
        {
            return fieldInfo.Name != "Type";
        }

        void ObjectInspector.IController.OnValueChanged(object holderObject)
        {
            var settings = NeuroUnityEditorSettings.Get();
            var currentSetting = settings.FindTypeSetting(type);
            if(currentSetting != null)
            {
                if (currentSetting != setting)
                {
                    currentSetting.CopyFrom(setting);
                }
            }
            else
            {
                settings.ClassSettings.Add(setting);
            }
            settings.Save();
        }
    }
}