using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// Narrows which subtypes the Neuro Editor offers when picking or creating a value of a polymorphic type -
    /// the subtype dropdown beside the field and the "create" popup for a null field or a new list element.
    /// Subclass it, decide per subtype in <see cref="Include"/>, and put it on the base class or interface the
    /// fields are declared as; every field of that type picks it up.
    /// </summary>
    /// <remarks>
    /// Only a convenience, never a rule: the popup's footer says how many are hidden and a <b>Show all</b>
    /// button lists the rest for that one popup, and a stored value of a hidden subtype is still listed as the
    /// current one. Several attributes on one type must all accept a subtype. If a filter would leave nothing to
    /// pick, it is ignored. It is read off the declared type itself and its base classes, not off interfaces a
    /// class implements.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class HideDebugCommandsAttribute : NeuroSubtypeFilterAttribute
    /// {
    ///     public override bool Include(Type subtype) => !subtype.Name.StartsWith("Debug");
    /// }
    ///
    /// [HideDebugCommands]
    /// [Neuro(0)] public interface ICommand { }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public abstract class NeuroSubtypeFilterAttribute : Attribute
    {
        /// <summary>Return true to list <paramref name="subtype"/> in the picker.</summary>
        public abstract bool Include(Type subtype);
    }
}
