using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Emergency Orders panel — 6 orders, 1 per day max.
    /// </summary>
    public class EmergencyOrdersPanel : MonoBehaviour
    {
        [Header("Header")]
        public TextMeshProUGUI headerText;

        [Header("Order Buttons")]
        public OrderCardUI o1Card;
        public OrderCardUI o2Card;
        public OrderCardUI o3Card;
        public OrderCardUI o4Card;
        public OrderCardUI o5Card;
        public OrderCardUI o6Card;

        [Header("O5 Zone Selector")]
        public GameObject zoneSelectorPanel;
        public Button[] zoneSelectButtons; // 5 buttons

        private GameManager _gm;
        private bool _selectingZone;

        private void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;

            SetupCard(o1Card, EmergencyOrderId.O1_DivertSuppliesToRepairs, "Divert Supplies", "Food -30, Water -20", "Repair +50% today; fixes wells");
            SetupCard(o2Card, EmergencyOrderId.O2_SoupKitchens, "Soup Kitchens", "Food -40", "Unrest -15 today");
            SetupCard(o3Card, EmergencyOrderId.O3_EmergencyWaterRation, "Emergency Water Ration", "(none)", "Water consumption -50%, Sickness +10 today");
            SetupCard(o4Card, EmergencyOrderId.O4_CrackdownPatrols, "Crackdown Patrols", "2 deaths, Morale -10", "Unrest -20 today");
            SetupCard(o5Card, EmergencyOrderId.O5_QuarantineDistrict, "Quarantine District", "(none)", "All production -50%, Sickness -10 today");
            SetupCard(o6Card, EmergencyOrderId.O6_InspireThePeople, "Inspire the People", "Materials -15", "Morale +15 today");

            if (zoneSelectorPanel) zoneSelectorPanel.SetActive(false);
            if (zoneSelectButtons != null)
            {
                for (int i = 0; i < zoneSelectButtons.Length; i++)
                {
                    int idx = i;
                    if (zoneSelectButtons[i] != null)
                        zoneSelectButtons[i].onClick.AddListener(() => OnZoneSelected(idx));
                }
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        private void SetupCard(OrderCardUI card, EmergencyOrderId id, string name, string cost, string effect)
        {
            if (card == null) return;
            card.orderId = id;
            if (card.nameText) card.nameText.text = name;
            if (card.costText) card.costText.text = $"Cost: {cost}";
            if (card.effectText) card.effectText.text = $"Effect: {effect}";
            if (card.issueButton) card.issueButton.onClick.AddListener(() => OnIssue(id));
        }

        private void OnIssue(EmergencyOrderId id)
        {
            if (id == EmergencyOrderId.O5_QuarantineDistrict)
            {
                _selectingZone = true;
                if (zoneSelectorPanel) zoneSelectorPanel.SetActive(true);
                RefreshZoneSelector();
            }
            else
            {
                _gm.IssueOrder(id);
            }
        }

        private void OnZoneSelected(int zoneIndex)
        {
            _selectingZone = false;
            if (zoneSelectorPanel) zoneSelectorPanel.SetActive(false);
            _gm.IssueOrder(EmergencyOrderId.O5_QuarantineDistrict, zoneIndex);
        }

        private void RefreshZoneSelector()
        {
            if (zoneSelectButtons == null || _gm.State == null) return;
            for (int i = 0; i < zoneSelectButtons.Length && i < _gm.State.zones.Length; i++)
            {
                if (zoneSelectButtons[i] == null) continue;
                var zone = _gm.State.zones[i];
                zoneSelectButtons[i].interactable = !zone.isLost;
                var txt = zoneSelectButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = zone.definition.zoneName;
            }
        }

        private void Refresh()
        {
            var s = _gm.State;
            if (s == null) return;

            bool used = s.todayEmergencyOrder.HasValue;
            if (headerText)
                headerText.text = used
                    ? $"Emergency Order — ISSUED: {s.todayEmergencyOrder.Value}"
                    : "Emergency Order — 1 per day";

            RefreshCard(o1Card, s, used);
            RefreshCard(o2Card, s, used);
            RefreshCard(o3Card, s, used);
            RefreshCard(o4Card, s, used);
            RefreshCard(o5Card, s, used);
            RefreshCard(o6Card, s, used);
        }

        private void RefreshCard(OrderCardUI card, GameState s, bool used)
        {
            if (card == null) return;
            bool canIssue = _gm.CanIssueOrder(card.orderId);
            if (card.issueButton) card.issueButton.interactable = canIssue;
        }
    }

    [System.Serializable]
    public class OrderCardUI
    {
        public EmergencyOrderId orderId;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI effectText;
        public Button issueButton;
    }
}
