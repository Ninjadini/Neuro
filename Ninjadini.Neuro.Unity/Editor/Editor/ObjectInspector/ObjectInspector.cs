using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Ninjadini.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public partial class ObjectInspector : VisualElement
    {
        static readonly BasicController SharedController = new BasicController();
        
        Data data;
        object drawnObj;
        protected SearchablePopupField<string> subtypesDropDown;
        protected Foldout foldout;
        protected Toggle existsToggle;
        protected VisualElement fieldsParent;
        bool? openFoldout;

        public ObjectInspector()
        {
            schedule.Execute(Update).Every(ObjectInspectorFields.RefreshRate);
        }

        public void Draw(Type type, object obj, Action<object> setter = null, IController controller = null, string foldOutKey = null)
        {
            if (controller == null)
            {
                controller = SharedController;
            }
            openFoldout = true;
            Draw(new Data()
            {
                name = name,
                type = type,
                getter = () => obj,
                setter = setter != null ? (v) =>
                {
                    obj = v;
                    setter?.Invoke(v);
                    controller.OnValueChanged(obj);
                } : null,
                Controller = controller,
                path = foldOutKey
            });
        }

        public void Draw(string fieldName, Type type, Func<object> getter, Action<object> setter, IController controller = null, string foldOutKey = null)
        {
            Draw(new Data()
            {
                name = fieldName,
                getter = getter,
                setter = setter,
                type = type,
                Controller = controller,
                path = foldOutKey
            });
        }

        public virtual void Draw(Data drawData)
        {
            drawData.Controller ??= SharedController;
            data = drawData;
            
            if (data.type == null)
            {
                throw new ArgumentNullException(nameof(data.type));
            }
            if (data.getter == null)
            {
                throw new ArgumentNullException(nameof(data.getter));
            }
            
            Clear();
            existsToggle = null;
            foldout = null;
            var obj = data.getter();
            if (data.setter != null && (data.Controller?.ShouldAddFoldOut(data, obj) ?? true))
            {
                foldout = new Foldout();
                if (!string.IsNullOrEmpty(data.path))
                {
                    foldout.viewDataKey = data.path;
                }
                foldout.hierarchy[0].style.marginLeft = 0f;
                foldout.style.flexGrow = 1f;
                foldout.text = data.GetDisplayName();
                openFoldout ??= data.Controller?.ShouldAutoExpandFoldout(data.type) ?? false;
                foldout.SetValueWithoutNotify(openFoldout.Value);
                foldout.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
                {
                    if (evt.currentTarget == evt.target)
                    {
                        openFoldout = evt.newValue;
                        if (evt.newValue)
                        {
                            RedrawFields(data.getter());
                        }
                    }
                });
                if (!data.type.IsValueType)
                {
                    existsToggle = NeuroUiUtils.AddToggle(null, "", false, OnToggleClicked);
                    ObjectInspectorFields.AddToggleToFoldOut(foldout, existsToggle, !string.IsNullOrEmpty(data.name));
                    existsToggle.style.width = 20;
                }
                Add(foldout);
                fieldsParent = foldout.contentContainer;
                if (openFoldout != null && openFoldout.Value)
                {
                    RedrawFields(obj);
                }
            }
            else
            {
                fieldsParent = this;
                RedrawFields(obj);
            }
        }

        void OnToggleClicked(ChangeEvent<bool> evt)
        {
            if (evt.currentTarget != evt.target)
            {
                return;
            }
            if (evt.newValue)
            {
                ActivatorClicked(data.type, existsToggle, delegate(object obj)
                {
                    if (obj != null)
                    {
                        data.setter(obj);
                        data.Controller.OnValueChanged(obj);
                        foldout.value = true;
                    }
                    else
                    {
                        existsToggle.SetValueWithoutNotify(false);
                    }
                });
            }
            else
            {
                ObjectInspectorFields.ShowNullConfirmation(data, existsToggle, delegate(bool confirmed)
                {
                    if (confirmed)
                    {
                        data.setter(null);
                        data.Controller.OnValueChanged(null);
                        foldout.value = false;
                    }
                    else
                    {
                        existsToggle.SetValueWithoutNotify(true);
                    }
                });
            }
        }
        
        public void SetFoldOutOpen(bool value)
        {
            openFoldout = value;
            if (foldout != null)
            {
                foldout.value = value;
            }
        }

        void RedrawFields(object obj)
        {
            fieldsParent.Clear();
            var canEdit = data.Controller.CanEdit(data.type, obj);
            
            drawnObj = obj;
            var type = obj != null ? obj.GetType() : data.type;
            var controller = data.Controller;
            
            if (existsToggle != null)
            {
                existsToggle.SetValueWithoutNotify(obj != null);
                if (obj != null)
                {
                    existsToggle.style.display = canEdit && data.Controller.CanSetToNull(data.type, obj)
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                    existsToggle.SetEnabled(true);
                }
                else
                {
                    existsToggle.style.display = DisplayStyle.Flex;
                    existsToggle.SetEnabled(canEdit && data.Controller.CanCreateObject(data.type));
                }
            }

            DrawPolymorphicSelect(obj);
            
            ObjectInspectorFields.ApplyTooltip(this, data.MemberInfo, obj?.GetType());

            var header = controller?.CreateCustomHeader(data, obj);
            if (header != null)
            {
                fieldsParent.Add(header);
            }
            var customDrawer = controller?.CreateCustomDrawer(data);
            if (customDrawer != null)
            {
                fieldsParent.Add(customDrawer);
                return;
            }
                
            if (obj == null)
            {
                NeuroUiUtils.AddLabel(fieldsParent, $"null ({type.FullName})");
                return;
            }
            var container = fieldsParent;
            var isStruct = type.IsValueType;
            foreach (var fieldInfo in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!(controller?.ShouldDrawField(fieldInfo, obj) ?? false))
                {
                    continue;
                }
                var fieldData = CreateDataForField(data, obj, fieldInfo);
                CreateFieldHeader(fieldData, ref container);
                var element = ObjectInspectorFields.CreateField(fieldData);
                if (element != null)
                {
                    element.userData = fieldInfo;
                    ObjectInspectorFields.ApplyTooltip(element, fieldInfo, fieldInfo.FieldType);
                    container.Add(element);
                }
            }

            container = fieldsParent;
            if (!isStruct)
            {
                foreach (var propInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!(controller?.ShouldDrawProperty(propInfo, obj) ?? false))
                    {
                        continue;
                    }
                    if (!propInfo.CanRead 
                        || propInfo.GetMethod?.GetParameters().Length > 0 
                        || propInfo.IsDefined(typeof(EditorBrowsableAttribute)))
                    {
                        continue;
                    }
                    var fieldData = new Data()
                    {
                        name = propInfo.Name,
                        type = propInfo.PropertyType,
                        getter = () => propInfo.GetValue(obj),
                        setter = propInfo.CanWrite
                            ? (v) =>
                            {
                                propInfo.SetValue(obj, v);
                                data.setter?.Invoke(obj);
                                data.Controller.OnValueChanged(obj);
                            }
                            : null,
                        Controller = data.Controller,
                        MemberInfo = propInfo,
                        path = data.path + ">" + propInfo.Name
                    };
                    CreateFieldHeader(fieldData, ref container);
                    var element = ObjectInspectorFields.CreateField(fieldData);
                    if (element != null)
                    {
                        element.userData = propInfo;
                        ObjectInspectorFields.ApplyTooltip(element, propInfo, propInfo.PropertyType);
                        container.Add(element);
                    }
                }
            }
        }

        public static Data CreateDataForField(Data data, object obj, FieldInfo fieldInfo)
        {
            var type = obj != null ? obj.GetType() : data.type;
            return new Data()
            {
                name = fieldInfo.Name,
                type = fieldInfo.FieldType,
                getter = () => fieldInfo.GetValue(obj),
                setter = (v) =>
                {
                    fieldInfo.SetValue(obj, v);
                    if (type.IsValueType)
                    {
                        data.setter?.Invoke(obj);
                    }
                    data.Controller.OnValueChanged(obj);
                },
                Controller = data.Controller,
                MemberInfo = fieldInfo,
                path = data.path + ">" + fieldInfo.Name
            };
        }

        void CreateFieldHeader(Data data, ref VisualElement container)
        {
            if (data.MemberInfo != null && data.MemberInfo.IsDefined(typeof(HeaderAttribute), true))
            {
                var header = data.MemberInfo.GetCustomAttribute<HeaderAttribute>();
                if (header.header.StartsWith(">"))
                {
                    var headerStr = header.header.Substring(1);
                    if (headerStr.Length == 0)
                    {
                        container = fieldsParent;
                    }
                    else
                    {
                        var foldOut = new Foldout()
                        {
                            text = $"<b>{header.header.Substring(1)}</b>",
                        };
                        var foldoutHeader = foldOut.hierarchy[0];
                        foldoutHeader.style.paddingTop = foldoutHeader.style.paddingBottom = 2;
                        foldoutHeader.style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);
                        foldOut.contentContainer.style.marginLeft = 4;
                        fieldsParent.Add(foldOut);
                        container = foldOut.contentContainer;
                    }
                }
                else
                {
                    var lbl = new Label(header.header);
                    lbl.style.paddingTop = 12;
                    lbl.style.paddingLeft = 4;
                    lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    container.Add(lbl);
                }
            }
            var result = data.Controller?.CreateCustomFieldHeader(data);
            if (result != null)
            {
                container.Add(result);
            }
        }

        void DrawPolymorphicSelect(object obj)
        {
            var type = obj?.GetType() ?? data.type;
            if (data.setter == null
                || data.Controller == null 
                || !data.Controller.CanEdit(data.type, obj)
                || !data.Controller.CanCreateObject(data.type))
            {
                if (type != data.type)
                {
                    EnsureSubTypesDropdown();
                    subtypesDropDown.SetEnabled(false);
                    subtypesDropDown.value = type.Name;
                }
                else
                {
                    NeuroUiUtils.SetDisplay(subtypesDropDown, false);
                }
                return;
            }
            var allClasses = data.Controller?.GetPossibleCreationTypesOf(data.type) ?? FindAllTypesAssignableTo(data.type);
            if (allClasses.Length == 0 || (allClasses.Length == 1 && allClasses[0] == obj?.GetType()))
            {
                NeuroUiUtils.SetDisplay(subtypesDropDown, false);
                return;
            }
            EnsureSubTypesDropdown();
            subtypesDropDown.choices = allClasses.Select(GetClassName).ToList();
            subtypesDropDown.SetValueWithoutNotify(GetClassName(type) ?? "null");
        }
        
        static string GetClassName(Type type)
        {
            return type?.DeclaringType == null ? type?.Name : $"{type.DeclaringType.Name}.{type.Name}";
        }

        void EnsureSubTypesDropdown()
        {
            if (subtypesDropDown == null)
            {
                subtypesDropDown = new SearchablePopupField<string>();
                subtypesDropDown.RegisterValueChangedCallback(OnSubtypeDropDownChanged);
            }

            if (foldout != null)
            {
                ObjectInspectorFields.AddToggleToFoldOut(foldout, subtypesDropDown, !string.IsNullOrEmpty(data.name), 20);
            }
            else
            {
                fieldsParent.Add(subtypesDropDown);
            }
            NeuroUiUtils.SetDisplay(subtypesDropDown, true);
        }

        void OnSubtypeDropDownChanged(ChangeEvent<string> evt)
        {
            var allClasses = data.Controller?.GetPossibleCreationTypesOf(data.type) ?? FindAllTypesAssignableTo(data.type);
            var newType = allClasses.FirstOrDefault(t => t.Name == evt.newValue);
            object newObj = null;
            data.Controller?.SwitchObjectType(data.getter(), newType, ref newObj);
            if (newObj != null)
            {
                data.setter(newObj);
            }
            else
            {
                CreateInstance(newType, o =>
                {
                    if (o != null)
                    {
                        data.setter(o);
                    }
                });
            }
        }

        public void Update()
        {
            if (data.getter != null)
            {
                var newObj = data.getter();
                if (newObj != drawnObj)
                {
                    if (!data.type.IsValueType || (newObj != null) != (drawnObj != null))
                    {
                        RedrawFields(newObj);
                    }
                    else
                    {
                        existsToggle?.SetValueWithoutNotify(newObj != null);
                    }
                    if (foldout != null && foldout.value && newObj == null)
                    {
                        foldout.value = false;
                    }
                }
            }
        }

        public void ForceRedraw()
        {
            var newObj = data.getter();
            if (newObj != null)
            {
                RedrawFields(newObj);
            }
            else
            {
                existsToggle?.SetValueWithoutNotify(false);
            }
            if (foldout != null && foldout.value && newObj == null)
            {
                foldout.value = false;
            }
        }

        void ActivatorClicked(Type type, VisualElement fromElement, Action<object> createdCallback)
        {
            if (data.Controller != null)
            {
                data.Controller.CreateObject(type, fromElement, delegate(object obj)
                {
                    if (obj != null)
                    {
                        createdCallback(obj);
                    }
                    else
                    {
                        ShowCreateInstanceWindow(type, fromElement, createdCallback);
                    }
                });
            }
            else
            {
                ShowCreateInstanceWindow(type, fromElement, createdCallback);
            }
        }
        
        
        static Type[] FindAllTypesAssignableTo(Type type)
        {
            var typeIsClass = type.IsClass;
            // https://stackoverflow.com/questions/857705/get-all-derived-types-of-a-type
            return (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                where !domainAssembly.IsDynamic
                from assemblyType in domainAssembly.GetExportedTypes()
                where assemblyType.IsClass 
                      && (assemblyType == type || (typeIsClass ? assemblyType.IsSubclassOf(type) : type.IsAssignableFrom(assemblyType)))
                select assemblyType).ToArray();
        }

        void ShowCreateInstanceWindow(Type type, VisualElement fromElement, Action<object> createdCallback)
        {
            var allClasses = data.Controller?.GetPossibleCreationTypesOf(type) ?? FindAllTypesAssignableTo(type);
            if (allClasses.Length == 1)
            {
                CreateInstance(allClasses[0], createdCallback);
            }
            else
            {
                var rect = fromElement.worldBound;
                rect.y -= rect.height;

                var width = Math.Max(320, (int)rect.width);
                var window = new SearchablePopupField<Type>.SearchListPopupWindow(width,
                    allClasses.ToList(),
                    (t) => t.Name,
                    (t) =>
                    {
                        if (t != null)
                        {
                            CreateInstance(t, createdCallback);
                        }
                        else
                        {
                            createdCallback(null);
                        }
                    },
                    () => createdCallback(null)
                    );
                UnityEditor.PopupWindow.Show(rect, window);
            }
        }

        void CreateInstance(Type type, Action<object> createdCallback)
        {
            object result = null;
            try
            {
                result = Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("", e.Message + "\n" + e.StackTrace, "OK");
                Debug.LogException(e);
            }
            createdCallback(result);
        }
    }
}