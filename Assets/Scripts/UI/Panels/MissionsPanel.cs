using SiegeSurvival.Core;
using SiegeSurvival.Data;
using SiegeSurvival.Systems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Missions panel — 4 mission types, shows adjusted odds, launch buttons.
    /// </summary>
    public class MissionsPanel : MonoBehaviour
    {
        [Header("Header")]
        public TextMeshProUGUI headerText;

        [Header("Mission Active Display")]
        public GameObject missionActiveDisplay;
        public TextMeshProUGUI missionActiveText;

        [Header("Mission Cards")]
        public MissionCardUI m1Card;
        public MissionCardUI m2Card;
        public MissionCardUI m3Card;
        public MissionCardUI m4Card;

        [Header("Mission Selection Container")]
        public GameObject missionSelectionContainer;

        private GameManager _gm;

        private void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;

            SetupCard(m1Card, MissionId.M1_ForageBeyondWalls, "Forage Beyond Walls");
            SetupCard(m2Card, MissionId.M2_NightRaid, "Night Raid on Siege Camp");
            SetupCard(m3Card, MissionId.M3_SearchAbandonedHomes, "Search Abandoned Homes");
            SetupCard(m4Card, MissionId.M4_NegotiateBlackMarketeers, "Negotiate with Black Marketeers");

            Refresh();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        private void SetupCard(MissionCardUI card, MissionId id, string name)
        {
            if (card == null) return;
            card.missionId = id;
            if (card.nameText) card.nameText.text = name;
            if (card.launchButton) card.launchButton.onClick.AddListener(() => _gm.StartMission(id));
        }

        private void Refresh()
        {
            var s = _gm.State;
            if (s == null) return;

            bool mActive = s.activeMission != null;

            if (missionActiveDisplay)
                missionActiveDisplay.SetActive(mActive);
            if (missionActiveText && mActive)
                missionActiveText.text = $"Mission in progress: {s.activeMission.missionId} — resolves end of day";

            if (missionSelectionContainer)
                missionSelectionContainer.SetActive(!mActive);

            if (!mActive)
            {
                if (headerText)
                {
                    bool canLaunch = s.healthyWorkers >= 10;
                    headerText.text = canLaunch
                        ? "Missions — Requires 10 Workers"
                        : "Missions — Not enough workers (need 10 Healthy)";
                }

                RefreshCard(m1Card, s);
                RefreshCard(m2Card, s);
                RefreshCard(m3Card, s);
                RefreshCard(m4Card, s);
            }
        }

        private void RefreshCard(MissionCardUI card, GameState s)
        {
            if (card == null) return;

            bool canStart = _gm.CanStartMission(card.missionId);
            if (card.launchButton) card.launchButton.interactable = canStart;

            // Show adjusted odds
            MissionResolver.GetMissionOdds(card.missionId, s, out float[] probs, out string[] labels);
            if (card.oddsText && probs.Length > 0)
            {
                string oddsStr = "";
                for (int i = 0; i < probs.Length; i++)
                {
                    oddsStr += $"{labels[i]} ({probs[i] * 100:F1}%)";
                    if (i < probs.Length - 1) oddsStr += " | ";
                }
                card.oddsText.text = oddsStr;
            }

            // M2 fuel warning
            if (card.extraInfoText)
            {
                if (card.missionId == MissionId.M2_NightRaid)
                {
                    string warn = $"Fuel cost: 40";
                    if (s.fuel < 40)
                        warn += " (INSUFFICIENT — +20% bad outcome)";
                    card.extraInfoText.text = warn;
                    card.extraInfoText.color = s.fuel < 40 ? Color.red : Color.white;
                    card.extraInfoText.gameObject.SetActive(true);
                }
                else
                {
                    card.extraInfoText.gameObject.SetActive(false);
                }
            }
        }
    }

    [System.Serializable]
    public class MissionCardUI
    {
        public MissionId missionId;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI oddsText;
        public TextMeshProUGUI extraInfoText;
        public Button launchButton;
    }
}
