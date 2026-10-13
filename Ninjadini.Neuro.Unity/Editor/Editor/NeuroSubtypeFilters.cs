using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ninjadini.Neuro.Editor
{
    /// <summary>
    /// Applies the <see cref="NeuroSubtypeFilterAttribute"/>s on a polymorphic base type to the subtype picker
    /// and the "create" popup: what they list, the "Showing 30 of 56" footer and its Show all button.
    /// </summary>
    public static class NeuroSubtypeFilters
    {
        public const string ShowAllText = "Show all";

        /// <summary>The filter attributes on <paramref name="baseType"/> and its base classes, or null if none.</summary>
        public static NeuroSubtypeFilterAttribute[] Get(Type baseType)
        {
            if (baseType == null)
            {
                return null;
            }
            var result = baseType.GetCustomAttributes<NeuroSubtypeFilterAttribute>(true).ToArray();
            return result.Length > 0 ? result : null;
        }

        /// <summary>
        /// Those of <paramref name="allTypes"/> every filter on <paramref name="baseType"/> accepts, plus
        /// <paramref name="keep"/> (the current value's type) wherever it falls. Null when there is no filter, when
        /// it hides nothing, or when it would leave nothing to pick - the caller then lists everything.
        /// </summary>
        public static List<Type> Visible(Type baseType, IReadOnlyCollection<Type> allTypes, Type keep, out string filterName)
        {
            filterName = null;
            var filters = Get(baseType);
            if (filters == null || allTypes == null)
            {
                return null;
            }
            var result = allTypes.Where(t => t == keep || filters.All(f => f.Include(t))).ToList();
            if (result.Count == allTypes.Count || result.Count == 0 || (result.Count == 1 && result[0] == keep))
            {
                return null;
            }
            filterName = string.Join(" ", filters.Select(f => "[" + NeuroReferenceFilters.TrimAttributeSuffix(f.GetType().Name) + "]"));
            return result;
        }

        public static string FooterText(int shown, int total, string filterName, bool showingAll)
        {
            return showingAll
                ? $"Showing all {total}, ignoring {filterName} · {nameof(NeuroSubtypeFilterAttribute)}"
                : $"Showing {shown} of {total}, filtered by {filterName} · {nameof(NeuroSubtypeFilterAttribute)}";
        }

        /// <summary>
        /// Lists only the visible subtypes in <paramref name="shownList"/> (the list the window was made with, edited
        /// in place) and adds the footer whose Show all button puts the rest back for this popup.
        /// </summary>
        public static void SetupWindow(SearchablePopupField<Type>.SearchListPopupWindow window, List<Type> shownList,
            IReadOnlyCollection<Type> allTypes, List<Type> visible, string filterName)
        {
            shownList.Clear();
            shownList.AddRange(visible);
            window.FooterText = FooterText(visible.Count, allTypes.Count, filterName, false);
            window.FooterButtonText = ShowAllText;
            window.FooterButtonClicked = w =>
            {
                shownList.Clear();
                shownList.AddRange(allTypes);
                w.SetFooterText(FooterText(allTypes.Count, allTypes.Count, filterName, true));
                w.RefreshList();
            };
        }
    }

    /// <summary>
    /// The subtype dropdown beside a polymorphic field. Holds every possible subtype and narrows the list by the
    /// base type's <see cref="NeuroSubtypeFilterAttribute"/>s each time it opens, so Show all lasts one popup.
    /// </summary>
    public class NeuroSubtypePopupField : SearchablePopupField<Type>
    {
        Type baseType;
        Type[] allTypes = Array.Empty<Type>();

        public void SetTypes(Type baseType_, Type[] allTypes_)
        {
            baseType = baseType_;
            allTypes = allTypes_ ?? Array.Empty<Type>();
            choices = allTypes.ToList();
        }

        protected override void SetupWindow(SearchListPopupWindow window)
        {
            var visible = NeuroSubtypeFilters.Visible(baseType, allTypes, value, out var filterName);
            if (visible != null)
            {
                NeuroSubtypeFilters.SetupWindow(window, choices, allTypes, visible, filterName);
            }
            else if (choices.Count != allTypes.Length)
            {
                choices.Clear();
                choices.AddRange(allTypes);
            }
        }
    }
}
