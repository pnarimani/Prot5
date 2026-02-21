using SiegeSurvival.Core;
using SiegeSurvival.Data;
using SiegeSurvival.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Panels
{
    public class MidBottomInfoPanel : MonoBehaviour
    {
        const string EventLogPrefabResourcePath = "UI/EventLogPopup";
        const string VictoryPopupPrefabResourcePath = "UI/VictoryPopup";
        const string DefeatPopupPrefabResourcePath = "UI/DefeatPopup";

        [SerializeField] EventLogPanel eventLogPopupPrefab;
        [SerializeField] GameObject victoryPopupPrefab;
        [SerializeField] GameObject defeatPopupPrefab;

        ProgressBarWidget _siege;
        TextMeshProUGUI _siegeText;
        Button _eventLogButton;
        GameObject _endgamePopup;
        Button _endgameRestartButton;
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
            _gm.OnPhaseChanged += OnPhaseChanged;
            Canvas.ForceUpdateCanvases();
            Refresh();
        }

        void OnDestroy()
        {
            if (_eventLogButton != null)
                _eventLogButton.onClick.RemoveListener(OnEventLogClicked);
            if (_endgameRestartButton != null)
                _endgameRestartButton.onClick.RemoveListener(OnEndgameRestartClicked);

            if (_gm != null)
            {
                _gm.OnStateChanged -= Refresh;
                _gm.OnPhaseChanged -= OnPhaseChanged;
            }
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
            var prefab = eventLogPopupPrefab != null
                ? eventLogPopupPrefab
                : Resources.Load<EventLogPanel>(EventLogPrefabResourcePath);

            if (prefab == null)
            {
                Debug.LogError(
                    $"MidBottomInfoPanel: missing Event Log popup prefab at Resources/{EventLogPrefabResourcePath}");
                return;
            }

            var eventLogPanel = Instantiate(prefab, transform.root);
            eventLogPanel.showLatestEntryOnly = false;
            var popupRoot = eventLogPanel.popupRoot != null ? eventLogPanel.popupRoot : eventLogPanel.gameObject;
            if (popupRoot.TryGetComponent<RectTransform>(out var rt))
                StretchToParent(rt);
        }

        static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void OnPhaseChanged()
        {
            if (_gm == null)
                return;

            if (_gm.Phase == GamePhase.PlayerTurn)
            {
                DestroyEndgamePopup();
                return;
            }

            if (_endgamePopup != null)
                return;

            if (_gm.Phase == GamePhase.GameOver)
            {
                SpawnEndgamePopup(isVictory: false);
            }
            else if (_gm.Phase == GamePhase.Victory)
            {
                SpawnEndgamePopup(isVictory: true);
            }
        }

        void SpawnEndgamePopup(bool isVictory)
        {
            var prefab = GetEndgamePopupPrefab(isVictory);
            if (prefab == null)
            {
                var missingPath = isVictory ? VictoryPopupPrefabResourcePath : DefeatPopupPrefabResourcePath;
                Debug.LogError($"MidBottomInfoPanel: missing endgame popup prefab at Resources/{missingPath}");
                return;
            }

            _endgamePopup = Instantiate(prefab, transform.root);
            if (_endgamePopup.TryGetComponent<RectTransform>(out var rt))
                StretchToParent(rt);

            ConfigureEndgamePopup(isVictory);
        }

        GameObject GetEndgamePopupPrefab(bool isVictory)
        {
            if (isVictory)
                return victoryPopupPrefab != null ? victoryPopupPrefab : Resources.Load<GameObject>(VictoryPopupPrefabResourcePath);

            return defeatPopupPrefab != null ? defeatPopupPrefab : Resources.Load<GameObject>(DefeatPopupPrefabResourcePath);
        }

        void ConfigureEndgamePopup(bool isVictory)
        {
            var state = _gm?.State;
            if (_endgamePopup == null || state == null)
                return;

            var title = _endgamePopup.transform.Find("Window/Title")?.GetComponent<TextMeshProUGUI>();
            var message = _endgamePopup.transform.Find("Window/ScrollView/Viewport/Content/#LogText")?.GetComponent<TextMeshProUGUI>();
            var button = _endgamePopup.transform.Find("#CloseButton")?.GetComponent<Button>();
            var buttonText = _endgamePopup.transform.Find("#CloseButton/Anim/#Text")?.GetComponent<TextMeshProUGUI>();

            if (title != null)
                title.text = isVictory
                    ? $"VICTORY - Day {Mathf.Max(1, state.currentDay - 1)}"
                    : $"DEFEAT - Day {state.currentDay}";

            if (message != null)
                message.text = isVictory
                    ? "You held the city for 40 days. The siege is over."
                    : $"Cause of Defeat: {BuildLossMessage(state.gameOverReason)}";

            if (buttonText != null)
                buttonText.text = "Restart Run";

            if (button == null)
                return;

            _endgameRestartButton = button;
            _endgameRestartButton.onClick.RemoveListener(OnEndgameRestartClicked);
            _endgameRestartButton.onClick.AddListener(OnEndgameRestartClicked);
        }

        void DestroyEndgamePopup()
        {
            if (_endgameRestartButton != null)
                _endgameRestartButton.onClick.RemoveListener(OnEndgameRestartClicked);

            _endgameRestartButton = null;

            if (_endgamePopup == null)
                return;

            Destroy(_endgamePopup);
            _endgamePopup = null;
        }

        void OnEndgameRestartClicked()
        {
            _gm?.StartNewRun();
            DestroyEndgamePopup();
        }

        static string BuildLossMessage(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return "Unknown";

            return reason switch
            {
                "Breach" => "The Keep was breached.",
                "Council Revolt" => "Unrest sparked a council revolt.",
                "Total Collapse" => "Food and water ran out for too long.",
                _ => reason
            };
        }
    }
}
