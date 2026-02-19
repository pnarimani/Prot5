using SiegeSurvival.Core;
using SiegeSurvival.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Panels
{
    public class MidBottomInfoPanel : MonoBehaviour
    {
        const string EventLogPrefabResourcePath = "UI/EventLogPopup";

        [SerializeField] EventLogPanel eventLogPopupPrefab;

        ProgressBarWidget _siege;
        TextMeshProUGUI _siegeText;
        Button _eventLogButton;
        EventLogPanel _eventLogPanel;
        GameManager _gm;

        bool _inited;

        void Awake()
        {
            _siege = FindBar("#Siege");
            _siegeText = this.FindChildRecursive<TextMeshProUGUI>("#SiegeText");
            _eventLogButton = this.FindChildRecursive<Button>("#EventLogButton");
            if (_eventLogButton != null)
                _eventLogButton.onClick.AddListener(OnEventLogClicked);
        }

        void Start()
        {
            _gm = GameManager.Instance;
            if (_gm == null)
                return;

            _gm.OnStateChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_eventLogButton != null)
                _eventLogButton.onClick.RemoveListener(OnEventLogClicked);

            if (_gm != null)
                _gm.OnStateChanged -= Refresh;
        }

        void Refresh()
        {
            var s = _gm?.State;
            if (s == null)
                return;

            _siege.SetValue(Mathf.Clamp01(s.siegeIntensity / 6f), _inited);
            _siegeText.text = $"Siege Level: {s.siegeIntensity}/6";
            _inited = true;
        }

        ProgressBarWidget FindBar(string name)
            => this.FindChildRecursive<Transform>(name)?.GetComponentInChildren<ProgressBarWidget>();

        void OnEventLogClicked()
        {
            var popup = GetOrCreatePopup();
            popup?.OpenPopup();
        }

        EventLogPanel GetOrCreatePopup()
        {
            if (_eventLogPanel != null)
                return _eventLogPanel;

            var prefab = eventLogPopupPrefab != null
                ? eventLogPopupPrefab
                : Resources.Load<EventLogPanel>(EventLogPrefabResourcePath);

            if (prefab == null)
            {
                Debug.LogError($"MidBottomInfoPanel: missing Event Log popup prefab at Resources/{EventLogPrefabResourcePath}");
                return null;
            }

            _eventLogPanel = Instantiate(prefab, transform.root);
            var popupRoot = _eventLogPanel.popupRoot != null ? _eventLogPanel.popupRoot : _eventLogPanel.gameObject;
            if (popupRoot.TryGetComponent<RectTransform>(out var rt))
                StretchToParent(rt);

            return _eventLogPanel;
        }

        static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
