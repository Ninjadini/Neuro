using System;
using System.Linq;
using System.Reflection;
using Ninjadini.Neuro.Sync;
using Ninjadini.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public class NeuroEditorNavElement : VisualElement
    {
        SearchablePopupField<string> typeDropdown;
        NeuroReferencablesDropdownField itemDropdown;
        Type[] allTypes;
        Type selectedType;
        NeuroDataFile selectedItem;
        NeuroEditorItemElement itemEditor;
        NeuroItemDebugDisplay debugDisplay;
        NeuroEditorDataProvider dataProvider;

        NeuroEditorClassSettingsElement classSettingsElement;
        NeuroEditorHistory historyElement;

        VisualElement reloadPanel;

        Button leftBtn;
        Button rightBtn;

        Button deleteBtn;
        Button addBtn;
        Button cloneBtn;

        public NeuroEditorNavElement(NeuroEditorDataProvider dataProvider_)
        {
            dataProvider = dataProvider_ ?? throw new ArgumentNullException(nameof(dataProvider_));

            style.flexGrow = 1;
            style.bottom = 0;

            var topBar = NeuroUiUtils.AddHorizontal(this);
            topBar.style.flexShrink = 0f;
            typeDropdown = new SearchablePopupField<string>();
            typeDropdown.style.flexGrow = 1f;
            typeDropdown.style.flexShrink = 1f;
            typeDropdown.BeforePopupShown = OnBeforeTypesPopupShown;
            typeDropdown.RegisterValueChangedCallback(OnTypeDropDownChanged);
            topBar.Add(typeDropdown);

            classSettingsElement = new NeuroEditorClassSettingsElement();
            Add(classSettingsElement);
            topBar.Add(classSettingsElement.ToggleButton);
            classSettingsElement.ToggleButton.clicked += () => classSettingsElement.Toggle(selectedType);
            
            NeuroUiUtils.AddButton(topBar, "⎇ Settings", () =>
            {
                SettingsService.OpenProjectSettings(NeuroUnityEditorSettings.SETTINGS_MENU_PATH);
            }).tooltip = "Open Project Settings > Neuro";
            
            NeuroUiUtils.AddButton(topBar, "↻ Reload", () =>
            {
                //EditorUtility.RequestScriptReload();
                dataProvider_.Reload();
                OnUpdate();
            });

            var secondBar = NeuroUiUtils.AddHorizontal(this);
            secondBar.style.flexShrink = 0f;
            itemDropdown = new NeuroReferencablesDropdownField(dataProvider.References);
            itemDropdown.style.flexGrow = 1f;
            itemDropdown.style.overflow = Overflow.Hidden;
            itemDropdown.RegisterValueChangedCallback(OnItemDropDownChanged);
            secondBar.Add(itemDropdown);

            leftBtn = NeuroUiUtils.AddButton(secondBar, "←", () => OnDirectionalBtnClicked(-1));
            leftBtn.style.minWidth = 40;
            rightBtn = NeuroUiUtils.AddButton(secondBar, "→", () => OnDirectionalBtnClicked(1));
            rightBtn.style.minWidth = 40;
            
            addBtn = NeuroUiUtils.AddButton(secondBar, "＋ Add", OnAddBtnClicked);
            addBtn.style.minWidth = 60;

            var thirdBar = NeuroUiUtils.AddHorizontal(this);
            thirdBar.style.flexShrink = 0f;
            
            historyElement = new NeuroEditorHistory(dataProvider_, (item) =>
            {
                selectedItem = item;
                UpdateSelectedItem();
            }, () => selectedItem);
            historyElement.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            historyElement.style.flexGrow = 1f;
            historyElement.style.flexShrink = 1f;
            thirdBar.Add(historyElement);
            thirdBar.Add(new VisualElement()
            {
                style =
                {
                    flexGrow = 1f,
                }
            });
            deleteBtn = NeuroUiUtils.AddButton(thirdBar, "✕ Delete", OnDeleteBtnClicked);
            deleteBtn.style.width = 73;
            deleteBtn.style.flexShrink = 1f;
            
            cloneBtn = NeuroUiUtils.AddButton(thirdBar, "❏ Clone", OnCloneBtnClicked);
            cloneBtn.style.width = 73;
            cloneBtn.style.flexShrink = 1f;

            reloadPanel = NeuroUiUtils.AddHorizontal(this);
            reloadPanel.style.alignItems = Align.Center;
            reloadPanel.style.justifyContent = Justify.Center;
            reloadPanel.style.backgroundColor = new Color(0.5f, 0.1f, 0.05f, 1f);
            reloadPanel.style.display = DisplayStyle.None;
            NeuroUiUtils.AddLabel(reloadPanel, "File changes or problems detected");
            NeuroUiUtils.AddButton(reloadPanel, "Reload", () =>
            {
                //EditorUtility.RequestScriptReload();
                dataProvider_.Reload();
                OnUpdate();
            });

            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            Add(scrollView);
            itemEditor = new NeuroEditorItemElement();
            itemEditor.AnyValueChanged = ItemValueChanged;
            itemEditor.style.flexShrink = 1f;
            scrollView.Add(itemEditor);

            debugDisplay = new NeuroItemDebugDisplay(dataProvider.References, () => selectedItem?.Value, SetValueFromDebug);
            debugDisplay.style.bottom = 0;
            debugDisplay.style.flexShrink = 0.01f;
            Add(debugDisplay);

            NeuroSyncTypes.TryRegisterAllAssemblies();
            allTypes = NeuroGlobalTypes.GetAllRootTypes()
                .Where(t => typeof(IReferencable).IsAssignableFrom(t))
                .OrderBy(NeuroUnityEditorSettings.GetTypeDropDownName)
                .ToArray();
            typeDropdown.choices = allTypes.Select(NeuroUnityEditorSettings.GetTypeDropDownName).ToList();
            
            schedule.Execute(OnUpdate).Every(ObjectInspectorFields.RefreshRate);
            schedule.Execute(DelayedInit);
        }

        internal void SetHistoryData(NeuroEditorHistory.HistoryData historyData)
        {
            historyElement.SetHistoryData(historyData);
        }

        void OnBeforeTypesPopupShown()
        {
            typeDropdown.choices = allTypes.Select(NeuroUnityEditorSettings.GetTypeDropDownName).ToList();
        }

        void DelayedInit()
        {
            if (selectedType == null)
            {
                var firstType = typeDropdown.choices.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstType))
                {
                    typeDropdown.SetValueWithoutNotify(firstType);
                    OnTypeDropDownChanged(ChangeEvent<string>.GetPooled(null, firstType));
                }
            }
        }

        public Type SelectedType => selectedType;
        public uint SelectedItemId => selectedItem?.Value?.RefId ?? 0;

        public void SetSelectedItem(Type type, uint refId)
        {
            var index = Array.IndexOf(allTypes, type);
            if (index < 0)
            {
                return;
            }
            historyElement.AddCurrentToHistory();
            if (refId != 0)
            {
                selectedType = type;
                typeDropdown.SetValueWithoutNotify(typeDropdown.choices[index]);
                itemDropdown.SetValue(selectedType, refId);
                UpdateAddBtn();
            }
            else
            {
                typeDropdown.value = typeDropdown.choices[index];
            }
        }

        void ItemValueChanged()
        {
            itemDropdown.SetValue(selectedType, selectedItem.Value.RefId, false);
            debugDisplay.Refresh();
        }

        void SetValueFromDebug(object obj)
        {
            var newObj = obj as IReferencable;
            if (newObj != null)
            {
                if (selectedItem.Value.RefId != newObj.RefId)
                {
                    NeuroObjectInspector.ShowRefIdChangedError(selectedItem.Value.RefId, newObj.RefId);
                    debugDisplay.Refresh();
                    return;
                }
                selectedItem.Value = newObj;
                dataProvider.SaveData(newObj);
                itemEditor.Draw(dataProvider, selectedType, selectedItem);
                debugDisplay.Refresh();
            }
        }

        void OnTypeDropDownChanged(ChangeEvent<string> evt)
        {
            var index = typeDropdown.choices.IndexOf(evt.newValue);
            var type = allTypes[index];
            if (selectedType != type)
            {
                selectedType = type;
                itemDropdown.SetValue(selectedType, 0, false);
                var firstItem = itemDropdown.choices.FirstOrDefault();
                itemDropdown.value = firstItem;
                if (firstItem == 0)
                {
                    OnItemDropDownChanged(ChangeEvent<uint>.GetPooled(0, 0));
                }
                classSettingsElement.UpdateClassSettingPanel(selectedType);
                UpdateAddBtn();
            }
        }

        void OnItemDropDownChanged(ChangeEvent<uint> evt)
        {
            var id = evt.newValue;
            var item = id != 0 ? dataProvider.Find(selectedType, id) : null;
            historyElement.AddCurrentToHistory();
            selectedItem = item;
            UpdateSelectedItem();
        }

        void SetSelectedItem(NeuroDataFile item)
        {
            historyElement.AddCurrentToHistory();
            selectedItem = item;
            UpdateSelectedItem();
        }

        void UpdateSelectedItem()
        {
            if (selectedItem?.Value != null)
            {
                var type = NeuroReferences.GetRootReferencable(selectedItem.Value.GetType());
                if (selectedType != type)
                {
                    selectedType = type;
                    typeDropdown.SetValueWithoutNotify(NeuroUnityEditorSettings.GetTypeDropDownName(type));
                }
                itemDropdown.SetValue(selectedType, selectedItem.Value.RefId, false);
                itemEditor.style.display = DisplayStyle.Flex;
                itemEditor.Draw(dataProvider, selectedType, selectedItem);
                debugDisplay.Refresh();
                deleteBtn.SetEnabled(true);
                
                NeuroEditorHistory.AddToSharedRecentHistory(selectedItem);
            }
            else
            {
                selectedItem = null;
                itemDropdown.SetValue(selectedType, 0, false);
                itemEditor.style.display = DisplayStyle.None;
                deleteBtn.SetEnabled(false);
            }
            itemDropdown.SetEnabled(!typeof(ISingletonReferencable).IsAssignableFrom(selectedType));
            historyElement.UpdateBackForwardBtns();
            UpdateDirectionalBtns();
            UpdateAddBtn();
        }

        void OnAddBtnClicked()
        {
            foreach (var linkedReferenceAttribute in selectedType.GetCustomAttributes<LinkedReferenceAttribute>())
            {
                if (!linkedReferenceAttribute.Optional && linkedReferenceAttribute.To != null && typeof(IReferencable).IsAssignableFrom(linkedReferenceAttribute.To))
                {
                    var toName = linkedReferenceAttribute.To.Name;
                    if (EditorUtility.DisplayDialog($"Can't add {selectedType.Name}",
                            $"It has a required linked reference to\n{linkedReferenceAttribute.To.Name}.\nYou can only create new items via the linked type.",
                            $"Go to {toName}", "Cancel"))
                    {
                        SetSelectedItem(NeuroReferences.GetRootReferencable(linkedReferenceAttribute.To), 0);
                    }
                    return;
                }
            }
            
            var obj = Activator.CreateInstance(selectedType) as IReferencable;
            var item = dataProvider.Add(obj);
            SetSelectedItem(item);
        }

        void OnCloneBtnClicked()
        {
            var src = selectedItem?.Value;
            if (src != null)
            {
                var bytes = new NeuroBytesWriter().WriteGlobalType(src);
                var obj = (IReferencable)new NeuroBytesReader().ReadGlobalTyped(bytes.ToArray());
                obj.RefName = src.RefName + " - Clone";
                var item = dataProvider.Add(obj);
                SetSelectedItem(item);
            }
        }

        void OnUpdate()
        {
            var value = selectedItem?.Value;
            if (value != null)
            {
                var item = dataProvider.Find(value.GetType(), selectedItem.Value.RefId);
                if (selectedItem != item)
                {
                    selectedItem = item;
                    UpdateSelectedItem();
                }
            }
            if (dataProvider.HasPendingFileChanges || dataProvider.HadProblemsLoading)
            {
                reloadPanel.style.display = DisplayStyle.Flex;
                itemEditor.SetEnabled(false);
            }
            else
            {
                reloadPanel.style.display = DisplayStyle.None;
                itemEditor.SetEnabled(true);
            }
        }


        void OnDeleteBtnClicked()
        {
            var message = $"Delete {selectedItem.Value?.RefId}-{selectedItem.Value?.RefName}?\n\n@ {selectedItem.FilePath}";
            if (EditorUtility.DisplayDialog("", message, "Delete", "Cancel"))
            {
                var index = itemDropdown.index;
                dataProvider.Delete(selectedItem);
                if (index > 0)
                {
                    index--;
                }
                else if (index < itemDropdown.choices.Count - 2)
                {
                    index++;
                }
                else
                {
                    index = -1;
                }
                var id = index >= 0 ? itemDropdown.choices[index] : 0;
                selectedItem = id != 0 ? dataProvider.Find(selectedType, id) : null;
                UpdateSelectedItem();
            }
        }

        void OnDirectionalBtnClicked(int dir)
        {
            var index = itemDropdown.index + dir;
            var choices = itemDropdown.choices;
            if (index >= 0 && index < choices.Count)
            {
                itemDropdown.SetValue(selectedType, choices[index]);
            }
            else
            {
                UpdateDirectionalBtns();
            }
        }

        void UpdateDirectionalBtns()
        {
            var index = itemDropdown.index;
            leftBtn.SetEnabled(index > 0);
            rightBtn.SetEnabled(index < itemDropdown.choices.Count - 1);
        }

        void UpdateAddBtn()
        {
            var canCreate = !(selectedItem?.Value is ISingletonReferencable);
            addBtn.SetEnabled(canCreate);
            cloneBtn.SetEnabled(canCreate);
        }
    }
}