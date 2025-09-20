using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public partial class ObjectInspectorFields
    {
        public const int RefreshRate = 1;
        
        public static VisualElement CreateField(in ObjectInspector.Data data)
        {
            var customDrawer = data.Controller?.CreateCustomDrawer(data);
            if (customDrawer != null)
            {
                return customDrawer;
            }
            var result = TryPrimitiveTypes(data);
            if (result != null) return result;

            result = TryUnityStructs(data);
            if (result != null)  return result;
            
            result = TryCollectionTypes(data);
            if (result != null)  return result;
            
            result = TryObjectTypes(data);
            if (result != null)  return result;

            return CreateUnsupportedDrawer(data.name, data.type, data.getter);
        }
        
        public static VisualElement CreateFieldWithStandardStyle(in ObjectInspector.Data data)
        {
            return AddStandardStyle(CreateField(data));
        }

        static VisualElement AddStandardStyle(VisualElement element)
        {
            if (element != null)
            {
                var elementStyle = element.style;
                elementStyle.backgroundColor = new Color(0.24f, 0.24f, 0.24f);
                elementStyle.marginLeft = 1;
                elementStyle.paddingLeft = 3;
                elementStyle.marginTop = 0;
                elementStyle.marginBottom = 0;
                elementStyle.paddingRight = elementStyle.paddingTop = elementStyle.paddingBottom = 1;
                elementStyle.borderBottomWidth = elementStyle.borderTopWidth = 2;
                elementStyle.borderLeftWidth = elementStyle.borderRightWidth = 3;
                elementStyle.borderBottomColor = elementStyle.borderLeftColor = elementStyle.borderRightColor = elementStyle.borderTopColor = new Color(0.22f, 0.22f, 0.22f);
            }
            return element;
        }

        public static void ApplyTooltip(VisualElement visualElement, MemberInfo memberInfo, Type objType)
        {
            if (visualElement == null)
            {
                return;
            }
            if (memberInfo != null && memberInfo.IsDefined(typeof(TooltipAttribute), true))
            {
                visualElement.tooltip = $"<b>{memberInfo.DeclaringType?.Name}.{memberInfo.Name}</b>\n{memberInfo.GetCustomAttribute<TooltipAttribute>().tooltip}";
            }
            else if (memberInfo != null && memberInfo.IsDefined(typeof(DescriptionAttribute), true))
            {
                visualElement.tooltip = $"<b>{memberInfo.DeclaringType?.Name}.{memberInfo.Name}</b>\n{memberInfo.GetCustomAttribute<DescriptionAttribute>().Description}";
            }
            else if (objType != null && objType.IsDefined(typeof(TooltipAttribute), true))
            {
                visualElement.tooltip = $"<b>{objType.Name}</b>\n{objType.GetCustomAttributes<TooltipAttribute>().Last().tooltip}";
            }
            else if (objType != null && objType.IsDefined(typeof(DescriptionAttribute), true))
            {
                visualElement.tooltip = $"<b>{objType.Name}</b>\n{objType.GetCustomAttributes<DescriptionAttribute>().Last().Description}";
            }
            else
            {
                visualElement.tooltip = null;
            }
        }

        public static InspectorStyleAttribute GetVisualStyle(MemberInfo memberInfo)
        {
            if (memberInfo != null && memberInfo.IsDefined(typeof(InspectorStyleAttribute), true))
            {
                return memberInfo.GetCustomAttribute<InspectorStyleAttribute>();
            }
            return null;
        }
        
        static VisualElement TryPrimitiveTypes(in ObjectInspector.Data data)
        {
            var type = data.type;
            if (type == typeof(string))
            {
                return CreateStringDrawer(data);
            }
            if (type == typeof(int))
            {
                return CreateDrawer(data, new IntegerField());
            }
            if (type == typeof(uint))
            {
                return CreateDrawer(data, new LongField(), obj => (uint)obj, obj => (uint)obj);
            }
            if (type == typeof(long))
            {
                return CreateDrawer(data, new LongField());
            }
            //if (type == typeof(ulong))
            {
                //return CreateDrawer(name, new LongField(), getter, setter);
            }
            if (type == typeof(float))
            {
                return CreateDrawer(data, new FloatField());
            }
            if (type == typeof(bool))
            {
                return CreateDrawer(data, new Toggle());
            }
            if (type == typeof(double))
            {
                return CreateDrawer(data, new DoubleField());
            }
            if (type.IsEnum)
            {
                if (type.IsDefined(typeof(FlagsAttribute)))
                {
                    return CreateDrawer(data, new EnumFlagsField((Enum)Activator.CreateInstance(type)));
                }
                else
                {
                    return CreateDrawer(data, new EnumField((Enum)Activator.CreateInstance(type)));
                }
            }
            return null;
        }

        static VisualElement TryUnityStructs(in ObjectInspector.Data data)
        {
            var type = data.type;
            if (type == typeof(Vector2))
            {
                return CreateDrawer(data, new Vector2Field());
            }
            if (type == typeof(Vector2Int))
            {
                return CreateDrawer(data, new Vector2IntField());
            }
            if (type == typeof(Vector3))
            {
                return CreateDrawer(data, new Vector3Field());
            }
            if (type == typeof(Vector3Int))
            {
                return CreateDrawer(data, new Vector3IntField());
            }
            if (type == typeof(Vector4))
            {
                return CreateDrawer(data, new Vector4Field());
            }
            if (type == typeof(Bounds))
            {
                return CreateDrawer(data, new BoundsField());
            }
            if (type == typeof(BoundsInt))
            {
                return CreateDrawer(data, new BoundsIntField());
            }
            if (type == typeof(Rect))
            {
                return CreateDrawer(data, new RectField());
            }
            if (type == typeof(RectInt))
            {
                return CreateDrawer(data, new RectIntField());
            }
            if (type == typeof(Hash128))
            {
                return CreateDrawer(data, new Hash128Field());
            }
            return null;
        }

        static VisualElement TryObjectTypes(in ObjectInspector.Data data)
        {
            var type = data.type;
            if (type.IsValueType && !type.IsPrimitive)
            {
                if (Nullable.GetUnderlyingType(type) != null)
                {
                    return CreateNullable(data);
                }
                if (type == typeof(DateTime))
                {
                    return CreateDateTimeDrawer(data);
                }
                if (type == typeof(TimeSpan))
                {
                    return CreateTimeSpanDrawer(data);
                }
                if (type == typeof(Guid))
                {
                    return CreateGuidDrawer(data);
                }
                if (type == typeof(Color) )
                {
                    return CreateDrawer(data, new ColorField(),  (c) => (Color)c, (v) => v);
                }
                if (type == typeof(Color32))
                {
                    return CreateDrawer(data, new ColorField(),  (o) => (Color32)o, (v) => (Color32)v);
                }
                if (type == typeof(System.Drawing.Color))
                {
                    return CreateDrawer<Color>(data, new ColorField(),  
                        (o) =>
                        {
                            var v = (System.Drawing.Color)o;
                            return new Color(v.R, v.G, v.B, v.A);
                        }, 
                        c => System.Drawing.Color.FromArgb((byte)(c.a * 255), (byte)(c.r * 255),
                            (byte)(c.g * 255), (byte)(c.b * 255))
                    );
                }
                var child = new ObjectInspector();
                child.Draw(data);
                return child;
            }
            if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return CreateUnityObjectDrawer(data);
            }
            if (type.IsClass || type.IsInterface)
            {
                var child = new ObjectInspector();
                child.Draw(data);
                return child;
            }
            return null;
        }

        static VisualElement TryCollectionTypes(in ObjectInspector.Data data)
        {
            var type = data.type;
            if (typeof(IList).IsAssignableFrom(type))
            {
                return CreateListDrawer(data);
            }
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return CreateDictionaryDrawer(data);
            }
            return null;
        }
    }
}