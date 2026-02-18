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
        }

        public void Init(JobSlot slot)
        {
            _gm = GameManager.Instance;
            _slot = slot;
            _addBtn.onClick.AddListener(() => _gm.AllocateWorkers(slot, 5));
            _removeBtn.onClick.AddListener(() => _gm.AllocateWorkers(slot, -5));
        }

        public void RefreshJobRow(GameState s, string label, string projection)
        {
            var assigned = s.workerAllocation[_slot];
            _titleTxt.text = label;
            _currentTxt.text = assigned.ToString();
            _projectionTxt.text = projection;

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
    }
}