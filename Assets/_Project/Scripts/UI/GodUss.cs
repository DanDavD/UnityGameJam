using GameJam.Core;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The USS class of each god ("god--zeus"...). Match.uss keys nation colors and icons on it,
    /// so views only switch a class and never hold colors or sprites themselves.
    /// </summary>
    public static class GodUss
    {
        private static readonly string[] All =
        {
            ClassOf(God.Zeus),
            ClassOf(God.Thor),
            ClassOf(God.Ra),
            ClassOf(God.Morrigan),
        };

        public static string ClassOf(God god) =>
            god switch
            {
                God.Zeus => "god--zeus",
                God.Thor => "god--thor",
                God.Ra => "god--ra",
                _ => "god--morrigan",
            };

        /// <summary>Leaves the element with only this god's class, or none for null.</summary>
        public static void Apply(VisualElement element, God? god)
        {
            foreach (string godClass in All)
                element.RemoveFromClassList(godClass);
            if (god.HasValue)
                element.AddToClassList(ClassOf(god.Value));
        }
    }
}
