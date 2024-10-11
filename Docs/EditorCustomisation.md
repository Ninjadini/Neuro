# Editor Customisation

### Custom reference drop down display
This lets you customise what the item looks in the references drop down list - to give more info than just default ref id and ref name.
```
// Text customisation
public class MyOtherReferencableObject : Referencable, INeuroRefDropDownCustomizable
{
    [Neuro(1)] public string Name;
    
    string INeuroRefDropDownCustomizable.GetRefDropdownText(NeuroReferences references) => RefId + " : "+ RefName + " -- "+Name;
}

// icon display customisation
public class MyOtherReferencableObject : Referencable, INeuroRefDropDownIconCustomizable
{
    [Neuro(1)] public AssetAddress Icon;

    string INeuroRefDropDownCustomizable.GetRefDropdownText(NeuroReferences references) => null; // no custom text in this example.

    AssetAddress INeuroRefDropDownIconCustomizable.RefDropdownIcon => Icon;
}
```

### Basic editor customisation
```
[DisplayName("Test > AnotherReferencableObject")] // < The type name used in editor dropdown list
[ToolTip("Tooltip for this class... Shows up when you mouse over any of the elements of this clas (but not if you are in play mode)")] // You can also use [Description()] 
public class AnotherReferencableObject : Referencable
{
    [ToolTip("Tooltip for this particular field")]
    [Neuro(1)] public string Name;
    
    
    [Header("A header")] // just like in unity
    [Neuro(2)] public string Value1;
    [Neuro(3)] public string Value2;
    
    [Header("> A header with fold out")] // < if you start the header text with "> ", it'll do a fold out
    [Neuro(2)] public string Value1;
    [Neuro(3)] public string Value2;
}
```

### Full editor customisation
Say you want to show the 3 values in one line without the name labels.
And it will say an error message if any of the values have lower than 1 value

> [!TIP]
> If you want to do validation it's actually better to use INeuroContentValidator

> [!CAUTION]
> API MAY CHANGE FOR THIS
```
public struct FloatABC
{
    [Neuro(1)] public float a;
    [Neuro(2)] public float b;
    [Neuro(3)] public float c;
}
    
public class MyGameNeuroEditorProvider : ICustomNeuroEditorProvider
{
    VisualElement ICustomNeuroEditorProvider.CreateCustomDrawer(NeuroObjectInspector inspector, ObjectInspector.Data data)
    {
        if (data.type == typeof(FloatABC))
        {
            // Return the drawer if it's the type you want to customise
            return new FloatABCFieldElement(inspector, data);
        }
        // idea is that you can have other type checks here
        return null;
    }
    
    class FloatABCFieldElement : ObjectInspector
    {
        HelpBox helpBox;
        
        internal FloatABCFieldElement(NeuroObjectInspector inspector, ObjectInspector.Data data)
        {
            var value = data.getter();
            
            style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            // Draw a
            var fieldInfo = typeof(FloatABC).GetField(nameof(FloatABC.a));
            var fieldDrawerData = CreateDataForField(data, value, fieldInfo);
            fieldDrawerData.name = data.name; // it would normally print a, but here we just want the parent field's name
            var fieldElement = ObjectInspectorFields.CreateField(fieldDrawerData);
            Add(fieldElement);
            
            // draw b
            fieldInfo = typeof(FloatABC).GetField(nameof(FloatABC.b));
            fieldDrawerData = CreateDataForField(data, value, fieldInfo);
            fieldDrawerData.name = ""; // it would normally print b, but here we just want 3 fields with no naming
            fieldElement = ObjectInspectorFields.CreateField(fieldDrawerData);
            Add(fieldElement);
            
            // draw c
            fieldInfo = typeof(FloatABC).GetField(nameof(FloatABC.c));
            fieldDrawerData = CreateDataForField(data, value, fieldInfo);
            fieldDrawerData.name = ""; // it would normally print c, but here we just want 3 fields with no naming
            fieldElement = ObjectInspectorFields.CreateField(fieldDrawerData);
            Add(fieldElement);

            helpBox = new HelpBox();
            helpBox.messageType = HelpBoxMessageType.Error;
            Add(helpBox);

            schedule.Execute(() => OnUpdate(data)).Every(10);
        }

        void OnUpdate(ObjectInspector.Data data)
        {
            var value = (FloatABC)data.getter();
            if (value.a < 1 || value.b < 1 || value.c < 1)
            {
                helpBox.text = "All values must be 1 or above";
                helpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                helpBox.style.display = DisplayStyle.None;
            }
            // This is just an example of updating the UI
            // Actual validation stuff is better done via INeuroContentValidator<T> see later section for more info
        }
    }
}
```