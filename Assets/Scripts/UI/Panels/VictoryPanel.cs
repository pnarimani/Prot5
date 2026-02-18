using SiegeSurvival.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Victory overlay panel — shown when player survives 40 days.
    /// </summary>
    public class VictoryPanel : MonoBehaviour
    {
        [Header("Layout")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI statsText;
        public TextMeshProUGUI telemetryText;
        public Button restartButton;

        private GameManager _gm;

        private void Start()
        {
            _gm = GameManager.Instance;
            if (restartButton) restartButton.onClick.AddListener(OnRestart);
        }

        private void OnEnable()
        {
            if (_gm == null) _gm = GameManager.Instance;
            if (_gm == null || _gm.State == null) return;
            Refresh();
        }

        private void Refresh()
        {
            var s = _gm.State;

            if (titleText)
            {
                titleText.text = "SURVIVED — 40 Days";
                titleText.color = Color.green;
            }

            if (statsText)
            {
                int zonesLost = 0;
                foreach (var z in s.zones) if (z.isLost) zonesLost++;

                statsText.text =
                    $"<b>Final Stats:</b>\n" +
                    $"Population: {s.TotalPopulation} (H:{s.healthyWorkers} G:{s.guards} S:{s.sick} E:{s.elderly})\n" +
                    $"Food: {s.food} | Water: {s.water} | Fuel: {s.fuel} | Medicine: {s.medicine} | Materials: {s.materials}\n" +
                    $"Morale: {s.morale} | Unrest: {s.unrest} | Sickness: {s.sickness}\n" +
                    $"Zones Lost: {zonesLost}/5\n" +
                    $"Laws Enacted: {s.enactedLaws.Count}";
            }

            if (telemetryText && _gm.Telemetry != null)
            {
                var t = _gm.Telemetry;
                telemetryText.text =
                    $"<b>Run Summary:</b>\n" +
                    $"Profile: {s.activeProfile}\n" +
                    $"Average Unrest: {t.AverageUnrest:F1}\n" +
                    $"First Deficit: Day {t.FirstDeficitDay} ({t.FirstDeficitResource})\n" +
                    $"First Zone Lost: Day {t.FirstZoneLostDay} ({t.FirstZoneLostName})";
            }
        }

        private void OnRestart()
        {
            _gm?.StartNewRun();
        }
    }
}
