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

        /// <summary>
        /// Shows the icon that leads a Destiny name: the favored god's chip on a god's face, the
        /// sacred mark on good fortune, the cursed mark on the bad omen.
        /// </summary>
        public static void ApplyDestinyIcon(VisualElement godChip, VisualElement omenIcon, int face)
        {
            God? god = Pantheon.GodOfFace(face);
            Apply(godChip, god);
            godChip.style.display = god.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
            bool good = face == Destiny.GoodFortuneFace;
            bool omen = Destiny.IsBadOmen(face);
            omenIcon.EnableInClassList("omen-icon--sacred", good);
            omenIcon.EnableInClassList("omen-icon--cursed", omen);
            omenIcon.style.display = good || omen ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
