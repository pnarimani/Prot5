using System.Collections.Generic;
using System.Text;
using SiegeSurvival;
using SiegeSurvival.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Persistent event log showing all actions/events since game start.
    /// Separated by day headers and shown in a popup panel.
    /// </summary>
    public class EventLogPanel : MonoBehaviour
    {
        [Header("Layout")]
        public TextMeshProUGUI logText;
        public ScrollRect scrollRect;
        
        [Header("Popup (optional)")]
        public GameObject popupRoot;
        public Button closeButton;

        private GameManager _gm;
        private readonly List<string> _dayEntries = new();
        private readonly StringBuilder _currentDayActions = new();
        private int _lastRecordedDay;
        
        private void Awake()
        {
            if (popupRoot == null)
                popupRoot = gameObject;
            if (logText == null)
                logText = this.FindChildRecursive<TextMeshProUGUI>("#LogText");
            if (scrollRect == null)
                scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (closeButton == null)
                closeButton = this.FindChildRecursive<Button>("#CloseButton");
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePopup);
        }

        private void Start()
        {
            _gm = GameManager.Instance;
            if (_gm != null)
            {
                _gm.OnStateChanged += OnStateChanged;
                _gm.OnDaySimulated += OnDaySimulated;
                _gm.OnPhaseChanged += OnPhaseChanged;
            }
            _lastRecordedDay = 0;
            ClearLog();
            BackfillFromTelemetry();
            RebuildDisplay();
        }

        private void OnDestroy()
        {
            if (_gm != null)
            {
                _gm.OnStateChanged -= OnStateChanged;
                _gm.OnDaySimulated -= OnDaySimulated;
                _gm.OnPhaseChanged -= OnPhaseChanged;
            }
            if (closeButton != null)
                closeButton.onClick.RemoveListener(ClosePopup);
        }

        public void ClearLog()
        {
            _dayEntries.Clear();
            _currentDayActions.Clear();
            _lastRecordedDay = 0;
            if (logText) logText.text = "";
        }

        /// <summary>
        /// Called by other panels/GameManager when a player action occurs during the turn.
        /// </summary>
        public void LogAction(string action)
        {
            _currentDayActions.AppendLine($"  • {action}");
        }

        private void OnStateChanged()
        {
            // Capture player actions during PlayerTurn phase
            // This is called after worker allocation, law enactment, etc.
            // We rely on explicit LogAction calls from GameManager events
        }

        private void BackfillFromTelemetry()
        {
            var snaps = _gm?.Telemetry?.Data?.dayByDaySnapshots;
            if (snaps == null || snaps.Count == 0)
                return;

            foreach (var snap in snaps)
            {
                if (snap.day <= _lastRecordedDay)
                    continue;

                var sb = new StringBuilder();
                sb.AppendLine($"=========== DAY {snap.day} ==============");
                sb.AppendLine("<b>Simulation:</b>");

                if (snap.events.Count == 0)
                {
                    sb.AppendLine("  (no logged events)");
                }
                else
                {
                    foreach (var evt in snap.events)
                        sb.AppendLine($"  <color=yellow>EVENT: {evt}</color>");
                }

                _dayEntries.Add(sb.ToString());
                _lastRecordedDay = snap.day;
            }
        }

        private void OnDaySimulated(SimulationContext ctx)
        {
            if (_gm == null)
                return;

            // After simulation, gather the day's events from the causality log
            var log = _gm.Log;
            var sb = new StringBuilder();

            int day = _gm.State.currentDay - 1; // simulation incremented the day already
            sb.AppendLine($"=========== DAY {day} ==============");

            // Player actions taken this turn
            if (_currentDayActions.Length > 0)
            {
                sb.AppendLine("<b>Player Actions:</b>");
                sb.Append(_currentDayActions);
            }

            // Simulation events
            sb.AppendLine("<b>Simulation:</b>");

            // Resources summary
            int foodNet = _gm.State.food - ctx.foodStart;
            int waterNet = _gm.State.water - ctx.waterStart;
            int fuelNet = _gm.State.fuel - ctx.fuelStart;
            sb.AppendLine($"  Resources: Food {foodNet:+0;-0;0}, Water {waterNet:+0;-0;0}, Fuel {fuelNet:+0;-0;0}");

            // Meters summary
            int moraleNet = _gm.State.morale - ctx.moraleStart;
            int unrestNet = _gm.State.unrest - ctx.unrestStart;
            int sickNet = _gm.State.sickness - ctx.sicknessStart;
            sb.AppendLine($"  Meters: Morale {moraleNet:+0;-0;0}, Unrest {unrestNet:+0;-0;0}, Sickness {sickNet:+0;-0;0}");

            // Events from causality log
            var events = log.GetByCategory(CausalityCategory.Event);
            foreach (var e in events)
                sb.AppendLine($"  <color=yellow>EVENT: {e.description}</color>");

            // Missions
            var missions = log.GetByCategory(CausalityCategory.Mission);
            foreach (var m in missions)
                sb.AppendLine($"  <color=cyan>MISSION: {m.description}</color>");

            // Deaths
            var deaths = log.GetByCategory(CausalityCategory.Death);
            foreach (var d in deaths)
                sb.AppendLine($"  <color=red>{d.description}</color>");

            // Zone changes
            foreach (var z in log.GetByCategory(CausalityCategory.Integrity))
            {
                if (z.description.Contains("LOST") || z.description.Contains("lost"))
                    sb.AppendLine($"  <color=red>ZONE: {z.description}</color>");
            }

            _dayEntries.Add(sb.ToString());
            _currentDayActions.Clear();
            _lastRecordedDay = day;
        }

        private void OnPhaseChanged()
        {
            if (_gm == null)
                return;

            // Rebuild display when returning to player turn (after report closed)
            if (_gm.Phase == Data.GamePhase.PlayerTurn || _gm.Phase == Data.GamePhase.GameOver || _gm.Phase == Data.GamePhase.Victory)
            {
                RebuildDisplay();
            }
        }

        private void RebuildDisplay()
        {
            if (logText == null) return;

            var sb = new StringBuilder();
            foreach (var entry in _dayEntries)
                sb.AppendLine(entry);

            logText.text = sb.ToString();

            // Scroll to bottom
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void ClosePopup()
        {
            Destroy(gameObject);
        }
    }
}
