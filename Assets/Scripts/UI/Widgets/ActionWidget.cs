using System;
using System.Collections.Generic;
using System.Linq;
using FastSpring;
using SiegeSurvival.Core;
using SiegeSurvival.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Widgets
{
    public enum ActionType
    {
        Law,
        Mission,
        EmergencyOrder
    }

    public class ActionWidget : MonoBehaviour
    {
        [SerializeField] private ActionType _actionType;
        [SerializeField] private ActionSelectionPopup _popup;

        Button _button;
        TextMeshProUGUI _buttonText, _title, _desc, _cons;
        RectTransform _actionPanel;
        TransformSpring _panelHolder;

        bool _isOpen;
        GameManager _gm;

        void Awake()
        {
            _button = this.FindChildRecursive<Button>("#ActivationButton");
            _buttonText = this.FindChildRecursive<TextMeshProUGUI>("#Text");
            _title = this.FindChildRecursive<TextMeshProUGUI>("#Title");
            _desc = this.FindChildRecursive<TextMeshProUGUI>("#Desc");
            _cons = this.FindChildRecursive<TextMeshProUGUI>("#Cons");
            _actionPanel = this.FindChildRecursive<RectTransform>("#ChosenActionPanel");
            _panelHolder = this.FindChildRecursive<TransformSpring>("#ChosenActionPanelHolder");

            _panelHolder.SizeDelta.MoveInstantly(Vector2.zero);

            _button.onClick.AddListener(OnButtonClicked);
        }

        void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnScheduledActionChanged += RefreshWidget;
            _gm.OnStateChanged += RefreshWidget;

            _buttonText.text = GetDefaultButtonText();
            RefreshWidget();
        }

        void OnDestroy()
        {
            if (_gm != null)
            {
                _gm.OnScheduledActionChanged -= RefreshWidget;
                _gm.OnStateChanged -= RefreshWidget;
            }
        }

        void OnButtonClicked()
        {
            var popup = Instantiate(_popup, transform.root, false);
            popup.OnEntrySelected += OnEntrySelected;
            popup.ShowEntries(GetEntries());
        }

        void OnEntrySelected(ActionSelectionEntry obj)
        {
            // Set panel content before scheduling (scheduling fires events that call RefreshWidget)
            _title.text = obj.Title;
            _desc.text = obj.Description;
            _cons.text = obj.Consequences;

            // Schedule the action via GameManager
            switch (_actionType)
            {
                case ActionType.Law:
                    _gm.EnactLaw((LawId)Enum.Parse(typeof(LawId), obj.Id));
                    break;
                case ActionType.EmergencyOrder:
                    _gm.IssueOrder((EmergencyOrderId)Enum.Parse(typeof(EmergencyOrderId), obj.Id));
                    break;
                case ActionType.Mission:
                    _gm.StartMission((MissionId)Enum.Parse(typeof(MissionId), obj.Id));
                    break;
            }

            Show();
        }

        void RefreshWidget()
        {
            // Mission in-progress takes priority over scheduling
            if (_actionType == ActionType.Mission && _gm.State.activeMission != null)
            {
                var def = GetMissionDefinition(_gm.State.activeMission.missionId);
                if (def != null)
                {
                    int remaining = _gm.State.activeMission.startDay + def.Duration - _gm.State.currentDay;
                    _buttonText.text = $"{remaining} day{(remaining != 1 ? "s" : "")} left";
                    _button.interactable = false;

                    _title.text = def.displayName;
                    _desc.text = "Mission in progress";
                    _cons.text = def.outcomesDescription;
                    Show();
                }
                return;
            }

            // Reset button to default state
            _button.interactable = true;
            _buttonText.text = GetDefaultButtonText();

            // Show/hide panel based on whether this widget's action type is scheduled
            bool isScheduled = _actionType switch
            {
                ActionType.Law => _gm.State.scheduledLaw.HasValue,
                ActionType.EmergencyOrder => _gm.State.scheduledOrder.HasValue,
                ActionType.Mission => _gm.State.scheduledMission.HasValue,
                _ => false
            };

            if (isScheduled)
                Show();
            else
                Hide();
        }

        string GetDefaultButtonText()
        {
            return _actionType switch
            {
                ActionType.Law => "Law",
                ActionType.Mission => "Mission",
                ActionType.EmergencyOrder => "Emergency Order",
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        ActionSelectionEntry[] GetEntries()
        {
            return _actionType switch
            {
                ActionType.Law => GetLawEntries(),
                ActionType.Mission => GetMissionEntries(),
                ActionType.EmergencyOrder => GetEmergencyOrderEntries(),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        ActionSelectionEntry[] GetLawEntries()
        {
            var entries = new List<ActionSelectionEntry>();
            var lawIds = (LawId[])Enum.GetValues(typeof(LawId));

            foreach (var lawId in lawIds)
            {
                if (!_gm.IsLawUnlocked(lawId) || _gm.State.enactedLaws.Contains(lawId))
                    continue;

                var def = GetLawDefinition(lawId);
                if (def == null)
                {
                    Debug.LogError($"Failed to find law def for {lawId}");
                    continue;
                }

                entries.Add(new ActionSelectionEntry
                {
                    Id = lawId.ToString(),
                    Title = def.displayName,
                    Description = def.description,
                    Consequences = FormatEffects(def.onEnactEffectsDescription, def.ongoingEffectsDescription)
                });
            }

            return entries.ToArray();
        }

        ActionSelectionEntry[] GetMissionEntries()
        {
            var entries = new List<ActionSelectionEntry>();
            var missionIds = (MissionId[])Enum.GetValues(typeof(MissionId));

            foreach (var missionId in missionIds)
            {
                var def = GetMissionDefinition(missionId);
                if (def == null) continue;

                entries.Add(new ActionSelectionEntry
                {
                    Id = missionId.ToString(),
                    Title = def.displayName,
                    Description = def.description,
                    Consequences = def.outcomesDescription
                });
            }

            return entries.ToArray();
        }

        ActionSelectionEntry[] GetEmergencyOrderEntries()
        {
            var entries = new List<ActionSelectionEntry>();
            var orderIds = (EmergencyOrderId[])Enum.GetValues(typeof(EmergencyOrderId));

            foreach (var orderId in orderIds)
            {
                var def = GetEmergencyOrderDefinition(orderId);
                if (def == null) continue;

                entries.Add(new ActionSelectionEntry
                {
                    Id = orderId.ToString(),
                    Title = def.displayName,
                    Description = def.description,
                    Consequences = FormatEffects(def.costDescription, def.effectDescription)
                });
            }

            return entries.ToArray();
        }

        static string FormatEffects(string effect1, string effect2)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(effect1) && effect1 != "(none)")
                parts.Add(effect1);
            if (!string.IsNullOrEmpty(effect2) && effect2 != "(none)")
                parts.Add(effect2);
            return string.Join(" | ", parts);
        }

        static LawDefinition GetLawDefinition(LawId id)
        {
            return Resources.LoadAll<LawDefinition>("Data/Laws")
                .FirstOrDefault(x => x.lawId == id);
        }

        static MissionDefinition GetMissionDefinition(MissionId id)
        {
            return Resources.LoadAll<MissionDefinition>("Data/Missions")
                .FirstOrDefault(x => x.missionId == id);
        }

        static EmergencyOrderDefinition GetEmergencyOrderDefinition(EmergencyOrderId id)
        {
            return Resources.LoadAll<EmergencyOrderDefinition>("Data/EmergencyOrders")
                .FirstOrDefault(x => x.orderId == id);
        }

        public void Show()
        {
            _panelHolder.SizeDelta.Target = _actionPanel.rect.size;
            _isOpen = true;
        }

        public void Hide()
        {
            _panelHolder.SizeDelta.Target = Vector2.zero;
            _isOpen = false;
        }
    }
}
