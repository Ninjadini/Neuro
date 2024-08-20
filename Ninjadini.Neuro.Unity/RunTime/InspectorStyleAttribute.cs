using System;
using UnityEngine;

namespace Ninjadini.Neuro
{
    public class InspectorStyleAttribute : Attribute
    {
        public int? Space;
        public Color? BackgroundColor;
        public bool? Horizontal;
        
        public InspectorStyleAttribute(int? space = null, Color? backgroundColor = default, bool? horizontal = default)
        {
            Space = space;
            BackgroundColor = backgroundColor;
            Horizontal = horizontal;
        }
    }
}