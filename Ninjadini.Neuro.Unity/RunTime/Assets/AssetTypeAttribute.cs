using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Tells the Neuro Editor which asset type an <see cref="AssetAddress"/> field should offer.
    /// Optional - without it the picker shows every asset.
    /// </summary>
    /// <remarks>
    /// Editor only; it does not restrict what you load at runtime.
    /// </remarks>
    /// <example>
    /// <code>
    /// [AssetType(typeof(UnityEngine.Sprite))]
    /// [Neuro(1)] public AssetAddress Icon;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class AssetTypeAttribute : Attribute
    {
        /// <summary>The asset type, when you can reference it at compile time.</summary>
        public Type Type;

        /// <summary>Full name of the asset type, resolved by the editor. See <see cref="AssetTypeAttribute(string)"/>.</summary>
        public string TypeString;

        public AssetTypeAttribute(Type type)
        {
            Type = type;
        }

        /// <param name="typeStr">
        /// Full type name, e.g. "UnityEngine.U2D.SpriteAtlas". Use this when the assembly defining the type
        /// isn't referenced by yours - an optional package, or an editor only type.
        /// </param>
        public AssetTypeAttribute(string typeStr)
        {
            TypeString = typeStr;
        }
    }
}
