using SiegeSurvival.Core;
using SiegeSurvival.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival
{
    public class WorkerAllocationWidget : MonoBehaviour
    {
        Button _addBtn;
        Button _removeBtn;
        TextMeshProUGUI _currentTxt;
        TextMeshProUGUI _descTxt;
        TextMeshProUGUI _titleTxt;
        TextMeshProUGUI _warningTxt;
        TextMeshProUGUI _projectionTxt;
        TooltipMaker _tooltipMaker;
        JobSlot _slot;
        GameManager _gm;

        void Awake()
        {
            _addBtn = this.FindChildRecursive<Button>("#Add");
            _removeBtn = this.FindChildRecursive<Button>("#Remove");
            _currentTxt = this.FindChildRecursive<TextMeshProUGUI>("#CurrentCount");
            _titleTxt = this.FindChildRecursive<TextMeshProUGUI>("#Title");
            _descTxt = this.FindChildRecursive<TextMeshProUGUI>("#Desc");
            _warningTxt = this.FindChildRecursive<TextMeshProUGUI>("#Warning");
            _projectionTxt = this.FindChildRecursive<TextMeshProUGUI>("#Projection");
            if (!TryGetComponent<TooltipMaker>(out _tooltipMaker))
                _tooltipMaker = gameObject.AddComponent<TooltipMaker>();
        }

        public void Init(JobSlot slot)
        {
            _gm = GameManager.Instance;
            _slot = slot;
            _addBtn.onClick.AddListener(() => _gm.AllocateWorkers(slot, 5));
            _removeBtn.onClick.AddListener(() => _gm.AllocateWorkers(slot, -5));
            _tooltipMaker.SetTooltip(GetTooltipText(slot, null, null));
        }

        public void RefreshJobRow(GameState s, string label, string projection)
        {
            var assigned = s.workerAllocation[_slot];
            _titleTxt.text = label;
            _currentTxt.text = assigned.ToString();
            _projectionTxt.text = projection;
            _tooltipMaker.SetTooltip(GetTooltipText(_slot, projection, s));

            _addBtn.interactable = _gm.CanAllocateWorkers(_slot, 5);
            _removeBtn.interactable = _gm.CanAllocateWorkers(_slot, -5);

            var warn = "";
            if (_slot == JobSlot.Repairs && s.materials <= 0 && assigned > 0)
                warn = "No materials!";
            else if (_slot == JobSlot.ClinicStaff && s.medicine <= 0 && assigned > 0)
                warn = "No medicine!";
            _warningTxt.text = string.Empty;
            _warningTxt.gameObject.SetActive(!string.IsNullOrEmpty(warn));
        }

        static string GetTooltipText(JobSlot slot, string projection, GameState state)
        {
            string projectionLine = string.IsNullOrWhiteSpace(projection) ? string.Empty : $"\nCurrent projection: {projection}";
            return slot switch
            {
                JobSlot.FoodProduction => "Food Production\nEvery 5 workers produce 10 base Food/day before morale, unrest, fuel, law, and zone modifiers." + projectionLine,
                JobSlot.WaterDrawing => "Water Drawing\nEvery 5 workers produce 12 base Water/day. Output is reduced when wells are damaged." + projectionLine,
                JobSlot.MaterialsCrafting => "Materials Crafting\nEvery 5 workers produce 8 base Materials/day (zone and law modifiers apply)." + projectionLine,
                JobSlot.Repairs => "Repairs\nEvery 5 workers repair 8 base integrity/day and consume 4 Materials/day. Repairs stall without materials." + projectionLine,
                JobSlot.Sanitation => "Sanitation\nEvery 5 workers reduce Sickness by 5/day. Strong counter to overcrowding and fuel-deficit disease pressure." + projectionLine,
                JobSlot.ClinicStaff => "Clinic Staff\nEvery 5 workers reduce Sickness by 8/day and consume Medicine. Without medicine, clinic output is 0." + projectionLine,
                JobSlot.FuelScavenging => "Fuel Scavenging\nEvery 5 workers produce 15 base Fuel/day. At Siege Intensity 4+, this job has a daily ambush death risk." + projectionLine + BuildFuelRiskLine(state),
                _ => "Allocate workers in groups of 5." + projectionLine
            };
        }

        static string BuildFuelRiskLine(GameState state)
        {
            if (state == null)
                return string.Empty;

            return state.siegeIntensity >= 4
                ? "\nRisk active: 20% chance of 2 deaths/day while fuel scavenging."
                : "\nRisk inactive: ambush risk starts at Siege Intensity 4.";
        }
    }
}
