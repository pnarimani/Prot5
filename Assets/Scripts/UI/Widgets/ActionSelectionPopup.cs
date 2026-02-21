using System;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Widgets
{
    public class ActionSelectionPopup : MonoBehaviour
    {
        [SerializeField] private ActionSelectionEntryView _prefab;
        [SerializeField] private Transform _parent;
        [SerializeField] private Button _close;
        
        public event Action<ActionSelectionEntry> OnEntrySelected;

        void Awake()
        {
            _close.onClick.AddListener(() => Destroy(gameObject));
        }

        public void ShowEntries(ActionSelectionEntry[] entries)
        {
            foreach (var e in entries)
            {
                var row = Instantiate(_prefab, _parent, false);
                row.Title.text = e.Title;
                row.Description.text = e.Description;
                row.Consequences.text = e.Consequences;
                var tooltip = EnsureTooltip(row.SelectButton != null ? row.SelectButton.gameObject : row.gameObject);
                tooltip.SetTooltip(e.Tooltip);
                var eCopy = e;
                row.SelectButton.onClick.AddListener(() =>
                {
                    OnEntrySelected?.Invoke(eCopy);
                    Destroy(gameObject);
                });
            }
        }

        static TooltipMaker EnsureTooltip(GameObject target)
        {
            if (!target.TryGetComponent<TooltipMaker>(out var tooltip))
                tooltip = target.AddComponent<TooltipMaker>();
            return tooltip;
        }
    }
}
