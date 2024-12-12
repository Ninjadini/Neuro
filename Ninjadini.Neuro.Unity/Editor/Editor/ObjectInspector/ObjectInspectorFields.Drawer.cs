using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public partial class ObjectInspectorFields
    {
        public const int NameFieldWidth = 120;
        
        public static VisualElement CreateUnsupportedDrawer(string name, Type type, Func<object> getter)
        {
            var element = new VisualElement();
            NeuroUiUtils.AddLabel(element, $"{name} {getter()?.GetType().FullName ?? type.FullName}");
            return element;
        }

        public static VisualElement CreateNullable(ObjectInspector.Data data)
        {
            var value = data.getter();
            var elementType = data.type.GenericTypeArguments[0];
            
            var canEdit = data.Controller.CanEdit(data.type, value);

            var horizontal = new VisualElement();
            horizontal.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            horizontal.AddToClassList(BaseField<TextField>.ussClassName);

            AddFieldNameLabel(horizontal, data.GetDisplayName());

            var existsToggle = NeuroUiUtils.AddToggle(horizontal, "", value != null);
            existsToggle.style.marginTop = 3;
            existsToggle.style.alignSelf = Align.FlexStart;
            existsToggle.SetEnabled(data.setter != null && canEdit);

            var dataCopy = data;
            dataCopy.name = "";
            dataCopy.type = elementType;
            var subElement = CreateField(dataCopy);
            horizontal.Add(subElement);

            var nullType = NeuroUiUtils.AddLabel(horizontal, elementType.Name);
            nullType.AddToClassList(TextField.labelUssClassName);
            nullType.AddToClassList(BaseField<TextField>.labelUssClassName);

            void UpdateCall(bool setNow)
            {
                subElement.style.maxWidth = setNow ? new StyleLength(StyleKeyword.Auto) : new StyleLength(0f);
                subElement.style.flexGrow = setNow ? 1 : 0;
                subElement.visible = setNow;
                nullType.visible = !setNow;
                existsToggle.SetValueWithoutNotify(setNow);
            }

            existsToggle.RegisterValueChangedCallback((evt) =>
            {
                if (evt.currentTarget == evt.target)
                {
                    Action performChange = () =>
                    {
                        var newValue = evt.newValue ? Activator.CreateInstance(elementType) : null;
                        data.SetValue(newValue);
                        UpdateCall(newValue != null);
                        if (subElement is ObjectInspector objectInspector)
                        {
                            objectInspector.Update();
                            objectInspector.SetFoldOutOpen(newValue != null);
                        }
                    };
                    if (!evt.newValue && evt.previousValue)
                    {
                        ShowNullConfirmation(data, existsToggle, delegate(bool confirmed)
                        {
                            if (confirmed)
                            {
                                performChange();
                            }
                            else
                            {
                                existsToggle.SetValueWithoutNotify(true);
                            }
                        });
                    }
                    else
                    {
                        performChange();
                    }
                }
            });
            UpdateCall(value != null);
            horizontal.schedule.Execute(() =>
            {
                var setNow = data.getter() != null;
                if (setNow != existsToggle.value)
                {
                    UpdateCall(setNow);
                }
            }).Every(RefreshRate);
            return horizontal;
        }

        public static Label AddFieldNameLabel(VisualElement parent, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var nameLbl = NeuroUiUtils.AddLabel(parent, name);
                nameLbl.AddToClassList(TextField.labelUssClassName);
                nameLbl.AddToClassList(BaseField<TextField>.labelUssClassName);
                nameLbl.style.minWidth = NameFieldWidth;
                return nameLbl;
            }
            return null;
        }

        public static VisualElement CreateStringDrawer(ObjectInspector.Data data)
        {
            var value = data.getter() as string;
            
            var canEdit = data.Controller.CanEdit(data.type, value);

            var horizontal = new VisualElement();
            horizontal.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            horizontal.AddToClassList(BaseField<TextField>.ussClassName);

            AddFieldNameLabel(horizontal, data.GetDisplayName());

            var existsToggle = NeuroUiUtils.AddToggle(horizontal, "", value != null);
            existsToggle.RegisterValueChangedCallback((evt) =>
            {
                if (evt.currentTarget != evt.target)
                {
                    return;
                }
                Action performChange = () =>
                {
                    var vv = evt.newValue ? "" : null;
                    data.SetValue(vv);
                };
                if (!evt.newValue && evt.previousValue)
                {
                    ShowNullConfirmation(data, existsToggle, delegate(bool confirmed)
                    {
                        if (confirmed)
                        {
                            performChange();
                        }
                        else
                        {
                            existsToggle.SetValueWithoutNotify(true);
                        }
                    });
                }
                else
                {
                    performChange();
                }
            });
            existsToggle.style.marginTop = 3;
            existsToggle.SetEnabled(data.setter != null && canEdit);
            if (data.setter != null && canEdit && (!data.Controller?.CanSetToNull(data.type, value) ?? false))
            {
                existsToggle.RemoveFromHierarchy();
            }

            var field = new TextField();
            field.style.marginRight = 0;
            field.style.flexGrow = 1;
            field.multiline = data.Controller?.ShouldBeMultilineText(data) ?? false;
            field.isDelayed = true;
            field.value = value;
            field.selectAllOnFocus = false;
            field.selectAllOnMouseUp = false;
            field.RegisterValueChangedCallback(delegate(ChangeEvent<string> evt)
            {
                if (evt.currentTarget == evt.target)
                {
                    value = evt.newValue;
                    existsToggle.SetValueWithoutNotify(value != null);
                    data.SetValue(evt.newValue);
                }
            });
            field.SetEnabled(data.setter != null && canEdit);
            horizontal.Add(field);
            horizontal.schedule.Execute(() =>
            {
                var newValue = data.getter() as string;
                if (value != newValue && !NeuroUiUtils.IsFocused(field))
                {
                    value = newValue;
                    field.SetValueWithoutNotify(newValue);
                    existsToggle.SetValueWithoutNotify(newValue != null);
                }
            }).Every(RefreshRate);
            return horizontal;
        }

        public static VisualElement CreateDrawer<T>(ObjectInspector.Data data, BaseField<T> field, Func<object, T> toFieldEditor = null, Func<T, object> fromFieldEditor = null)
        {
            var value = data.getter();

            var canEdit = data.Controller.CanEdit(data.type, value);
            
            field.label = data.GetDisplayName();
            field.style.flexGrow = 1;
            if (field is TextInputBaseField<T> textInputBaseField)
            {
                textInputBaseField.isDelayed = true;
            }
            var vObj = value ?? Activator.CreateInstance<T>();
            field.value = (T)(toFieldEditor != null ? toFieldEditor(vObj) : vObj);
            field.RegisterValueChangedCallback(delegate(ChangeEvent<T> evt)
            {
                if (evt.currentTarget == evt.target)
                {
                    value = fromFieldEditor != null ? fromFieldEditor(evt.newValue) : evt.newValue;
                    data.SetValue(value);
                }
            });
            field.SetEnabled(data.setter != null && canEdit);
            field.schedule.Execute(() =>
            {
                var newValue = data.getter();
                if (value != newValue && !NeuroUiUtils.IsFocused(field))
                {
                    value = newValue;
                    field.SetValueWithoutNotify(newValue != null ? (T)(toFieldEditor != null ? toFieldEditor(newValue) : newValue) : default(T));
                }
            }).Every(RefreshRate);
            return field;
        }

        public static VisualElement CreateDateTimeDrawer(ObjectInspector.Data data)
        {
            var value = (DateTime) (data.getter() ?? new DateTime());
            
            var canEdit = data.Controller.CanEdit(data.type, value);

            var horizontal = new VisualElement();
            horizontal.style.marginLeft = 3f;
            horizontal.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            AddFieldNameLabel(horizontal, data.GetDisplayName());
            
            var fields = new List<IntegerField>();
            
            var yearField = new IntegerField("y");
            yearField.style.marginLeft = 0f;
            yearField.style.minWidth = 50;
            fields.Add(yearField);
            
            var monthField = new IntegerField("m");
            monthField.style.minWidth = 36;
            fields.Add(monthField);
            
            var dayField = new IntegerField("d");
            dayField.style.minWidth = 36;
            fields.Add(dayField);
            
            var hourField = new IntegerField("h");
            hourField.style.minWidth = 36;
            fields.Add(hourField);
            
            var minuteField = new IntegerField("m");
            minuteField.style.minWidth = 36;
            fields.Add(minuteField);
            
            var secondField = new IntegerField("s");
            secondField.style.minWidth = 36;
            fields.Add(secondField);
            
            var kindField = new EnumField(value.Kind);
            
            foreach (var field in fields)
            {
                horizontal.Add(field);
                field.labelElement.style.minWidth = Length.Auto();
                var inputElement = field.Query<VisualElement>("unity-text-input").First();
                if (inputElement != null)
                {
                    inputElement.style.unityTextAlign = TextAnchor.MiddleRight;
                }
                field.style.flexDirection = FlexDirection.RowReverse;
                field.SetEnabled(data.setter != null && canEdit);
                if (data.setter != null)
                {
                    field.RegisterValueChangedCallback(IntValuesUpdated);
                }
            }

            horizontal.Add(kindField);
            kindField.SetEnabled(data.setter != null && canEdit);
            if (data.setter != null)
            {
                kindField.RegisterValueChangedCallback((v) =>
                {
                    UpdateValues();
                });
            }

            void IntValuesUpdated(ChangeEvent<int> evt)
            {
                UpdateValues();
            }

            void UpdateValues()
            {
                value = new DateTime(
                    Math.Clamp(yearField.value, 0, 5000),
                    Math.Clamp(monthField.value, 1, 12),
                    Math.Clamp(dayField.value, 1, 31),
                    Math.Clamp(hourField.value, 0, 23),
                    Math.Clamp(minuteField.value, 0, 59),
                    Math.Clamp(secondField.value, 0, 59),
                    0, (DateTimeKind)kindField.value);
                data.SetValue(value);
                UpdateFields();
            }
            void UpdateFields()
            {
                yearField.SetValueWithoutNotify(value.Year);
                monthField.SetValueWithoutNotify(value.Month);
                dayField.SetValueWithoutNotify(value.Day);
                hourField.SetValueWithoutNotify(value.Hour);
                minuteField.SetValueWithoutNotify(value.Minute);
                secondField.SetValueWithoutNotify(value.Second);
                kindField.SetValueWithoutNotify(value.Kind);
            }
            horizontal.schedule.Execute(() =>
            {
                var newValue = (DateTime) (data.getter() ?? new DateTime());
                if (value != newValue && !fields.Any(NeuroUiUtils.IsFocused))
                {
                    value = newValue;
                    UpdateFields();
                }
            }).Every(RefreshRate);
            UpdateFields();
            return horizontal;
        }


        public static VisualElement CreateTimeSpanDrawer(ObjectInspector.Data data)
        {
            var value = (TimeSpan) (data.getter() ?? TimeSpan.Zero);
            
            var canEdit = data.Controller.CanEdit(data.type, value);

            var horizontal = new VisualElement();
            horizontal.style.marginLeft = 3f;
            horizontal.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            AddFieldNameLabel(horizontal, data.GetDisplayName());
            
            var fields = new List<IntegerField>();
            
            var dayField = new IntegerField("d");
            dayField.style.marginLeft = 0f;
            dayField.style.minWidth = 40;
            fields.Add(dayField);
            
            var hourField = new IntegerField("h");
            hourField.style.minWidth = 40;
            fields.Add(hourField);
            
            var minuteField = new IntegerField("m");
            minuteField.style.minWidth = 40;
            fields.Add(minuteField);
            
            var secondField = new IntegerField("s");
            secondField.style.minWidth = 40;
            fields.Add(secondField);
            
            var msField = new IntegerField("ms");
            msField.style.minWidth = 60;
            fields.Add(msField);
            
            foreach (var field in fields)
            {
                horizontal.Add(field);
                field.labelElement.style.minWidth = Length.Auto();
                var inputElement = field.Query<VisualElement>("unity-text-input").First();
                if (inputElement != null)
                {
                    inputElement.style.unityTextAlign = TextAnchor.MiddleRight;
                }
                field.style.flexDirection = FlexDirection.RowReverse;
                field.SetEnabled(data.setter != null && canEdit);
                if (data.setter != null)
                {
                    field.RegisterValueChangedCallback(IntValuesUpdated);
                }
            }


            void IntValuesUpdated(ChangeEvent<int> evt)
            {
                value = new TimeSpan(
                    Math.Max(0, dayField.value),
                    Math.Clamp(hourField.value, 0, 23),
                    Math.Clamp(minuteField.value, 0, 59),
                    Math.Clamp(secondField.value, 0, 59),
                    Math.Clamp(msField.value, 0, 999));
                data.SetValue(value);
                UpdateFields();
            }
            void UpdateFields()
            {
                dayField.SetValueWithoutNotify((int)value.TotalDays);
                hourField.SetValueWithoutNotify(value.Hours);
                minuteField.SetValueWithoutNotify(value.Minutes);
                secondField.SetValueWithoutNotify(value.Seconds);
                msField.SetValueWithoutNotify(value.Milliseconds);
            }
            horizontal.schedule.Execute(() =>
            {
                var newValue = (TimeSpan) (data.getter() ?? TimeSpan.Zero);
                if (value != newValue && !fields.Any(NeuroUiUtils.IsFocused))
                {
                    value = newValue;
                    UpdateFields();
                }
            }).Every(RefreshRate);
            UpdateFields();
            return horizontal;
        }
        
        public static VisualElement CreateGuidDrawer(ObjectInspector.Data data)
        {
            var value = (Guid) (data.getter() ?? Guid.Empty);
            var canEdit = data.Controller.CanEdit(data.type, value);

            var horizontal = new VisualElement();
            horizontal.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            horizontal.AddToClassList(BaseField<TextField>.ussClassName);

            AddFieldNameLabel(horizontal, data.GetDisplayName());
            
            var field = new TextField();
            field.style.marginRight = 0;
            field.style.flexGrow = 1;
            field.isDelayed = true;
            field.value = value.ToString();
            field.RegisterValueChangedCallback(delegate(ChangeEvent<string> evt)
            {
                if (evt.currentTarget == evt.target)
                {
                    if (Guid.TryParse(evt.newValue, out var newValue))
                    {
                        data.SetValue(newValue);
                    }
                    else
                    {
                        field.SetValueWithoutNotify(value.ToString());
                    }
                }
            });
            field.SetEnabled(data.setter != null && canEdit);
            horizontal.Add(field);
            horizontal.schedule.Execute(() =>
            {
                var newValue = (Guid)data.getter();
                if (value != newValue && !NeuroUiUtils.IsFocused(field))
                {
                    value = newValue;
                    field.SetValueWithoutNotify(newValue.ToString());
                }
            }).Every(RefreshRate);
            return horizontal;
        }

        public static VisualElement CreateUnityObjectDrawer(ObjectInspector.Data data)
        {
            var value = data.getter() as UnityEngine.Object;
            var canEdit = data.Controller.CanEdit(data.type, value);

            var horizontal = new VisualElement();

            var foldout = new Foldout();
            foldout.style.flexGrow = 1f;
            foldout.text = data.GetDisplayName();
            foldout.value = false;
            if (!string.IsNullOrEmpty(data.path))
            {
                foldout.viewDataKey = data.path;
            }
            foldout.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                if (evt.currentTarget == evt.target)
                {
                    foldout.contentContainer.Clear();
                    if (evt.newValue)
                    {
                        var child = new ObjectInspector();
                        var dataCopy = data;
                        dataCopy.name = "";
                        dataCopy.setter = null;
                        child.Draw(dataCopy);
                        foldout.contentContainer.Add(child);
                    }
                }
            });
            horizontal.Add(foldout);

            var field = new ObjectField();
            field.value = value;
            field.objectType = data.type;
            field.allowSceneObjects = true;
            field.RegisterValueChangedCallback(delegate(ChangeEvent<UnityEngine.Object> evt)
            {
                if (evt.currentTarget == evt.target)
                {
                    value = evt.newValue;
                    data.SetValue(value);
                }
            });
            field.SetEnabled(data.setter != null && canEdit);
            AddToggleToFoldOut(foldout, field, true);
            field.style.right = 0;

            horizontal.schedule.Execute(() =>
            {
                var newValue = data.getter() as UnityEngine.Object;
                if (newValue != value)
                {
                    value = newValue;
                    field.SetValueWithoutNotify(newValue);
                }
            }).Every(RefreshRate);
            return horizontal;
        }

        public static VisualElement CreateListDrawer(ObjectInspector.Data data)
        {
            var value = data.getter() as IList;
            var canEdit = data.Controller.CanEdit(data.type, value);
            
            var listView = new ListView();
            listView.reorderable = canEdit;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.showAddRemoveFooter = canEdit;
            listView.showFoldoutHeader = true;
            listView.headerTitle = string.IsNullOrEmpty(data.name) ? data.type.Name : data.GetDisplayName();
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.selectionType = SelectionType.Single;
            listView.itemIndexChanged += delegate(int i, int i1)
            {
                data.SetValue(value);
            };

            var isReadOnly = value != null && data.MemberInfo is FieldInfo { IsInitOnly: true };
            
            var existsToggle = NeuroUiUtils.AddToggle(null, "", value != null);
            if (!isReadOnly && (data.setter == null || !canEdit || data.Controller.CanSetToNull(data.type, value)))
            {
                existsToggle.style.marginLeft = NameFieldWidth + 3;
                existsToggle.style.marginTop = 3;
                existsToggle.style.position = Position.Absolute;
                existsToggle.SetEnabled(data.setter != null && canEdit);
                listView.hierarchy.Add(existsToggle);
            }

            Button addBtn = null;
            if (data.setter != null && canEdit)
            {
                addBtn = NeuroUiUtils.AddButton(null, "+", delegate()
                {
                    if (data.getter() is not IList list || list.Count == 0)
                    {
                        var vv = CreateListType(data, 1);
                        data.SetValue(vv);
                        UpdateCall(true);
                    }
                });
                addBtn.style.height = 18;
                addBtn.style.marginLeft = NameFieldWidth + (existsToggle?.parent != null ? 20 : 0);
                addBtn.style.position = Position.Absolute;
                listView.hierarchy.Add(addBtn);
            }

            void UpdateCall(bool forcedUpdate)
            {
                var v = data.getter() as IList;
                if (forcedUpdate || value != v)
                {
                    value = v;
                    var hasValue = v != null;
                    existsToggle.SetValueWithoutNotify(v != null);
                    listView.itemsSource = value;
                    listView.showAddRemoveFooter = hasValue;
                    listView.showBoundCollectionSize = hasValue;
                    listView.Rebuild();
                }
                if (addBtn != null)
                {
                    addBtn.style.display = v == null || v.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
            existsToggle.RegisterValueChangedCallback((evt) =>
            {
                if (evt.currentTarget == evt.target)
                {
                    Action performChange = () =>
                    {
                        var vv = evt.newValue ? CreateListType(data, 0) : null;
                        data.SetValue(vv);
                        UpdateCall(true);
                    };
                    if (!evt.newValue && evt.previousValue)
                    {
                        ShowNullConfirmation(data, existsToggle, delegate(bool confirmed)
                        {
                            if (confirmed)
                            {
                                performChange();
                            }
                            else
                            {
                                existsToggle.SetValueWithoutNotify(true);
                            }
                        });
                    }
                    else
                    {
                        performChange();
                    }
                }
            });

            var elementType = data.type.HasElementType ? data.type.GetElementType() : data.type.GetGenericArguments()[0];
            listView.makeItem = () => new VisualElement();
            listView.bindItem = (element, index) =>
            {
                var dataCopy = data;
                dataCopy.name = index.ToString();
                dataCopy.type = elementType;
                dataCopy.getter = () => value[index];
                dataCopy.setter = (v) =>
                {
                    value[index] = v;
                    //data.Controller.OnValueChanged(value);
                };
                dataCopy.path = data.path + " > " + index; 
                element.Clear();
                var subElement = CreateField(dataCopy);
                subElement.style.backgroundColor = new StyleColor(new Color(0.26f, 0.26f, 0.26f));
                element.Add(subElement);
            };
            if (data.type.IsArray)
            {
                listView.itemsAdded += indexes =>
                {
                    var newValue = CreateListType(data, value.Count + indexes.Count()) as IList;
                    value.CopyTo((Array)newValue, 0);
                    value = newValue;
                    data.SetValue(newValue);
                };
                listView.itemsRemoved += indexes =>
                {
                    if (indexes.Count() != 1)
                    {
                        throw new System.NotImplementedException();
                    }
                    var index = indexes.First();
                    var newValue = CreateListType(data, value.Count -1) as IList;
                    for (int i = 0, l = value.Count - 1; i < l; i++)
                    {
                        newValue[i] = value[i >= index ? i + 1 : i];
                    }
                    value = newValue;
                    data.SetValue(newValue);
                };
                // TODO ^ this could maybe just use the same system as the list view ?
            }
            else
            {
                listView.SetViewController(new ListViewController());
                listView.viewController.itemsSourceSizeChanged += () =>
                {
                    data.SetValue(value);
                };
            }
            listView.itemsSource = value;
            listView.schedule.Execute(() => UpdateCall(false)).Every(RefreshRate);
            UpdateCall(true);
            return listView;
        }

        static object CreateListType(ObjectInspector.Data data, int count = 0)
        {
            object obj = null;
            if (data.type.IsArray && data.type.GetElementType() != null)
            {
                obj = Array.CreateInstance(data.type.GetElementType(), count);
            }
            else if (data.type.IsGenericType)
            {
                obj = Activator.CreateInstance(data.type);
                if (obj is IList list && list.Count < count)
                {
                    var elementType = data.type.GetGenericArguments().First();
                    for (var i = list.Count; i < count; i++)
                    {
                        list.Add(elementType.IsValueType ? Activator.CreateInstance(elementType) : null);
                    }
                }
            }
            else
            {
                obj ??= Activator.CreateInstance(data.type);
            }
            return obj;
        }

        public static VisualElement CreateDictionaryDrawer(ObjectInspector.Data data)
        {
            var value = data.getter() as IDictionary;
            var canEdit = (value == null || !value.IsReadOnly) && data.Controller.CanEdit(data.type, value);
            var genericArgs = data.type.GetGenericArguments();
            if (genericArgs.Length != 2)
            {
                return new Label("Unsupported Dictionary type. Only `Dictionary<TKey, TValue>` kind of dictionary format is supported.");
            }
            var keyType = genericArgs[0];
            var valueType = genericArgs[1];
            HelpBox hasConflictKeysBox = null;
            
            var keysAndValuesList = new List<(object key, object value)>();
            var listView = new ListView();
            listView.reorderable = canEdit;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.showAddRemoveFooter = canEdit;
            listView.showFoldoutHeader = true;
            listView.headerTitle = string.IsNullOrEmpty(data.name) ? data.type.Name : data.GetDisplayName();
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.selectionType = SelectionType.Single;

            var isReadOnly = value != null && data.MemberInfo is FieldInfo { IsInitOnly: true };
            
            var existsToggle = NeuroUiUtils.AddToggle(null, "", value != null);
            if (!isReadOnly && (data.setter == null || !canEdit || data.Controller.CanSetToNull(data.type, value)))
            {
                existsToggle.style.marginLeft = NameFieldWidth + 3;
                existsToggle.style.marginTop = 3;
                existsToggle.style.position = Position.Absolute;
                existsToggle.SetEnabled(data.setter != null && canEdit);
                listView.hierarchy.Add(existsToggle);
            }

            Button addBtn = null;
            if (data.setter != null && canEdit)
            {
                addBtn = NeuroUiUtils.AddButton(null, "+", delegate()
                {
                    if (data.getter() is not IDictionary dict || dict.Count == 0)
                    {
                        value = (IDictionary)Activator.CreateInstance(data.type);
                        if (keysAndValuesList.Count == 0)
                        {
                            keysAndValuesList.Add(default);
                        }
                        SyncKeysAndValuesFromList(true);
                        UpdateCall(true);
                    }
                });
                addBtn.style.height = 18;
                addBtn.style.marginLeft = NameFieldWidth + (existsToggle?.parent != null ? 20 : 0);
                addBtn.style.position = Position.Absolute;
                listView.hierarchy.Add(addBtn);
            }

            void ShowHasConflictKeys(bool show)
            {
                if (hasConflictKeysBox == null)
                {
                    hasConflictKeysBox = new HelpBox("One or more Dictionary keys shares the same value", HelpBoxMessageType.Error);
                    listView.hierarchy.Add(hasConflictKeysBox);
                }
                hasConflictKeysBox.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void SyncKeysAndValuesToList()
            {
                keysAndValuesList.Clear();
                if (value != null)
                {
                    foreach (var key in value.Keys)
                    {
                        keysAndValuesList.Add((key, value[key]));
                    }
                }
            }

            void SyncKeysAndValuesFromList(bool setValue)
            {
                value.Clear();
                var needsViewRefresh = false;
                var hasAnyConflictingKeys = false;
                for (var index = 0; index < keysAndValuesList.Count; index++)
                {
                    var kv = keysAndValuesList[index];
                    if (kv.key == null)
                    {
                        kv.key = keyType == typeof(string) ? "" : Activator.CreateInstance(keyType);
                        keysAndValuesList[index] = kv;
                        needsViewRefresh = true;
                    }
                    if (kv.value == null && valueType.IsValueType)
                    {
                        kv.value = Activator.CreateInstance(valueType);
                        keysAndValuesList[index] = kv;
                        needsViewRefresh = true;
                    }
                    var hasConflictingKeys = value.Contains(kv.key);
                    if (!hasConflictingKeys)
                    {
                        value.Add(kv.key, kv.value);
                    }
                    hasAnyConflictingKeys |= hasConflictingKeys;
                    var element = listView.GetRootElementForIndex(index);
                    if (element != null)
                    {
                        element.style.backgroundColor =  hasConflictingKeys ? Color.red: default;
                    }
                }
                if (setValue)
                {
                    data.SetValue(value);
                }
                if (needsViewRefresh)
                {
                    listView.RefreshItems();
                }
                ShowHasConflictKeys(hasAnyConflictingKeys);
            }

            void UpdateCall(bool forcedUpdate)
            {
                var v = data.getter() as IDictionary;
                if (forcedUpdate || value != v)
                {
                    value = v;
                    SyncKeysAndValuesToList();
                    var hasValue = v != null;
                    existsToggle.SetValueWithoutNotify(v != null);
                    listView.itemsSource = keysAndValuesList;
                    listView.showAddRemoveFooter = hasValue;
                    listView.showBoundCollectionSize = hasValue;
                    listView.Rebuild();
                }
                if (addBtn != null)
                {
                    addBtn.style.display = v == null || v.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
            existsToggle.RegisterValueChangedCallback((evt) =>
            {
                if (evt.currentTarget == evt.target)
                {
                    Action performChange = () =>
                    {
                        var vv = evt.newValue ? CreateListType(data, 0) : null;
                        data.SetValue(vv);
                        UpdateCall(true);
                    };
                    if (!evt.newValue && evt.previousValue)
                    {
                        ShowNullConfirmation(data, existsToggle, delegate(bool confirmed)
                        {
                            if (confirmed)
                            {
                                performChange();
                            }
                            else
                            {
                                existsToggle.SetValueWithoutNotify(true);
                            }
                        });
                    }
                    else
                    {
                        performChange();
                    }
                }
            });

            listView.makeItem = () => new VisualElement()
            {
                style = { flexDirection = FlexDirection.Row }
            };
            listView.bindItem = (element, index) =>
            {
                element.Clear();
                if (keysAndValuesList[index].key == null)
                {
                    return; // not ready yet.
                }
                var keyData = data;
                keyData.name = "";
                keyData.type = keyType;
                keyData.getter = () => keysAndValuesList[index].key;
                keyData.setter = (v) =>
                {
                    var kv = keysAndValuesList[index];
                    kv.key = v;
                    keysAndValuesList[index] = kv;
                    SyncKeysAndValuesFromList(true);
                };
                keyData.Controller = new DictionaryKeyController(data.Controller);
                keyData.path = data.path + " > " + index;
                var keyElement = CreateField(keyData);
                keyElement.style.width = NameFieldWidth;
                keyElement.style.maxWidth = 200;
                keyElement.style.flexGrow = 01f;
                //keyElement.style.backgroundColor = new StyleColor(new Color(0.26f, 0.26f, 0.26f));
                element.Add(keyElement);
                element.userData = keyElement;
                //
                var valueData = data;
                valueData.name = "";
                valueData.type = valueType;
                valueData.getter = () => keysAndValuesList[index].value;
                valueData.setter = (v) =>
                {
                    var kv = keysAndValuesList[index];
                    kv.value = v;
                    keysAndValuesList[index] = kv;
                    SyncKeysAndValuesFromList(true);
                };
                valueData.path = data.path + " > " + index; 
                var valueElement = CreateField(valueData);
                valueElement.style.flexGrow = 1f;
                valueElement.style.backgroundColor = new StyleColor(new Color(0.26f, 0.26f, 0.26f));
                element.Add(valueElement);
            };
            listView.SetViewController(new ListViewController());
            listView.viewController.itemsSourceSizeChanged += () =>
            {
                SyncKeysAndValuesFromList(true);
            };
            listView.itemIndexChanged += delegate(int i, int i1)
            {
                SyncKeysAndValuesFromList(true);
            };
            SyncKeysAndValuesToList();
            listView.itemsSource = keysAndValuesList;
            listView.schedule.Execute(() => UpdateCall(false)).Every(RefreshRate);
            UpdateCall(true);
            return listView;
        }

        class DictionaryKeyController : ObjectInspector.IController
        {
            ObjectInspector.IController _parent;
            public DictionaryKeyController(ObjectInspector.IController parent)
            {
                _parent = parent;
            }
            
            bool ObjectInspector.IController.ShouldDrawField(FieldInfo fieldInfo, object holderObject) => _parent?.ShouldDrawField(fieldInfo, holderObject) ?? true;
            bool ObjectInspector.IController.ShouldDrawProperty(PropertyInfo propertyInfo, object holderObject) => _parent?.ShouldDrawProperty(propertyInfo, holderObject) ?? false;
            bool ObjectInspector.IController.CanEdit(Type type, object value) => _parent?.CanEdit(type, value) ?? true;
            VisualElement ObjectInspector.IController.CreateCustomDrawer(ObjectInspector.Data data) => _parent?.CreateCustomDrawer(data);
            void ObjectInspector.IController.ApplyStyle(ObjectInspector.Data data, VisualElement element) => _parent?.ApplyStyle(data, element);
            bool ObjectInspector.IController.CanSetToNull(Type type, object value) => false;
        }

        internal static void ShowNullConfirmation(ObjectInspector.Data data, VisualElement visualElement, Action<bool> callback)
        {
            var rect = visualElement.worldBound;
            rect.y -= rect.height;
            UnityEditor.PopupWindow.Show(rect, new ConfirmNullPopupWindow()
            {
                callback = callback
            });
        }

        internal static void AddToggleToFoldOut(Foldout foldout, VisualElement element, bool hasName, int additionalLeftShift = 0)
        {
            element.style.left = (hasName ? NameFieldWidth : 20) + additionalLeftShift;
            element.style.top = 2;
            element.style.position = Position.Absolute;
            foldout.hierarchy.Add(element);
        }
        
        internal class ConfirmNullPopupWindow : PopupWindowContent
        {
            public Action<bool> callback;
            
            public override Vector2 GetWindowSize()
            {
                return new Vector2(100, 24);
            }

            public override void OnGUI(Rect rect)
            {
            }

            void TryCallback(bool setYes)
            {
                var cb = callback;
                callback = null;
                cb?.Invoke(setYes);
            }

            public override void OnOpen()
            {
                var btn = new Button();
                btn.text = "Set to null";
                btn.style.width = GetWindowSize().x - 5;
                btn.style.height = GetWindowSize().y - 2;
                btn.clicked += () =>
                {
                    TryCallback(true);
                    editorWindow.Close();
                };
                editorWindow.rootVisualElement.Add(btn);
            }

            public override void OnClose()
            {
                TryCallback(false);
                base.OnClose();
            }
        }
    }
}