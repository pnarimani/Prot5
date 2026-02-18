using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival
{
    public static class Utils
    {
        public static T FindChildRecursive<T>(this Component component, string name) where T : class
        {
            for (var i = 0; i < component.transform.childCount; i++)
            {
                var child = component.transform.GetChild(i);
                if (child.name == name)
                    if (child.TryGetComponent<T>(out var t))
                        return t;

                var comp = child.FindChildRecursive<T>(name);
                if (comp != null)
                    return comp;
            }

            return null;
        }

        public static T FindChildRecursive<T>(this GameObject go, string name) where T : class
            => go.transform.FindChildRecursive<T>(name);

        public static void RebuildAllLayouts(this Transform tx)
        {
            foreach (var layoutGroup in tx.GetComponentsInChildren<LayoutGroup>())
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
            foreach (var layoutGroup in tx.GetComponentsInParent<LayoutGroup>())
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)tx.root);
        }

        public static async void RebuildAllLayoutsNextFrame(this Transform tx)
        {
            await Awaitable.EndOfFrameAsync();
            tx.RebuildAllLayouts();
        }
    }
}