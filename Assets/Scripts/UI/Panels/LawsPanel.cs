using System;
using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Shows all 12 laws with unlock status, enacted status, cooldown timer, and enact buttons.
    /// </summary>
    public class LawsPanel : MonoBehaviour
    {
        [Header("Header")]
        public TextMeshProUGUI headerText;

        [Header("Law Entries (create 12 children)")]
        public Transform lawContainer;
        public GameObject lawEntryPrefab;

        private GameManager _gm;
        private LawEntryUI[] _entries;

        private static readonly LawDisplayData[] _lawData = new LawDisplayData[]
        {
            new("Strict Rations", "Always", "Morale -10", "Food consumption -25%, Unrest +5/day"),
            new("Diluted Water", "Water < 100 or deficit", "Morale -5", "Water consumption -20%, Sickness +5/day"),
            new("Extended Shifts", "Day ≥ 5", "Morale -15", "All production +25%, Sickness +8/day"),
            new("Mandatory Guard Service", "Unrest > 40", "10 Workers → Guards, Morale -10", "Food +15/day consumption"),
            new("Emergency Shelters", "Any zone lost", "Unrest +10", "Inner District +30 cap, Sickness +10/day"),
            new("Public Executions", "Unrest > 60", "Unrest -25, Morale -20, 5 deaths", "(none)"),
            new("Faith Processions", "Morale < 40", "Materials -10, Morale +15, Unrest +5", "(none)"),
            new("Food Confiscation", "Food < 100", "Food +100, Unrest +20, Morale -20", "(none)"),
            new("Medical Triage", "Medicine < 20", "(none)", "Clinic cost -50%, 5 Sick die/day"),
            new("Curfew", "Unrest > 50", "(none)", "Unrest -10/day, All production -20%"),
            new("Abandon Outer Ring", "Farms Integrity < 40", "Farms Lost, Unrest +15", "Siege damage ×0.8"),
            new("Martial Law", "Unrest > 75", "(none)", "Unrest capped 60, Morale capped 40"),
        };

        private void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;
            BuildEntries();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        private void BuildEntries()
        {
            var lawIds = (LawId[])Enum.GetValues(typeof(LawId));
            _entries = new LawEntryUI[lawIds.Length];

            for (int i = 0; i < lawIds.Length; i++)
            {
                GameObject go;
                if (lawEntryPrefab != null && lawContainer != null)
                {
                    go = Instantiate(lawEntryPrefab, lawContainer);
                }
                else
                {
                    go = new GameObject($"Law_{lawIds[i]}");
                    if (lawContainer != null) go.transform.SetParent(lawContainer, false);
                }

                var entry = go.GetComponent<LawEntryUI>();
                if (entry == null) entry = go.AddComponent<LawEntryUI>();
                _entries[i] = entry;

                int idx = i;
                LawId lid = lawIds[i];
                entry.Initialize(_lawData[i], () => _gm.EnactLaw(lid));
            }
        }

        private void Refresh()
        {
            var s = _gm.State;
            if (s == null) return;

            int cooldownRemaining = Mathf.Max(0, 3 - s.daysSinceLastLaw);
            if (headerText)
                headerText.text = cooldownRemaining > 0
                    ? $"Laws — Cooldown: {cooldownRemaining} days"
                    : "Laws — Ready";

            var lawIds = (LawId[])Enum.GetValues(typeof(LawId));
            for (int i = 0; i < lawIds.Length && i < _entries.Length; i++)
            {
                if (_entries[i] == null) continue;
                bool enacted = s.enactedLaws.Contains(lawIds[i]);
                bool unlocked = _gm.IsLawUnlocked(lawIds[i]);
                bool canEnact = _gm.CanEnactLaw(lawIds[i]);
                _entries[i].Refresh(enacted, unlocked, canEnact);
            }
        }
    }

    public struct LawDisplayData
    {
        public string name;
        public string requirement;
        public string onEnact;
        public string ongoing;

        public LawDisplayData(string n, string req, string enact, string ong)
        {
            name = n;
            requirement = req;
            onEnact = enact;
            ongoing = ong;
        }
    }

    public class LawEntryUI : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI requirementText;
        public TextMeshProUGUI onEnactText;
        public TextMeshProUGUI ongoingText;
        public TextMeshProUGUI statusText;
        public Button enactButton;
        public TextMeshProUGUI enactButtonText;

        private Action _onEnact;

        public void Initialize(LawDisplayData data, Action onEnact)
        {
            _onEnact = onEnact;

            // Auto-find or create child texts
            EnsureTexts();

            if (nameText) nameText.text = data.name;
            if (requirementText) requirementText.text = $"Requires: {data.requirement}";
            if (onEnactText) onEnactText.text = $"On Enact: {data.onEnact}";
            if (ongoingText) ongoingText.text = $"Ongoing: {data.ongoing}";

            if (enactButton) enactButton.onClick.AddListener(() => _onEnact?.Invoke());
        }

        private void EnsureTexts()
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length >= 5)
            {
                nameText = nameText ? nameText : texts[0];
                requirementText = requirementText ? requirementText : texts[1];
                onEnactText = onEnactText ? onEnactText : texts[2];
                ongoingText = ongoingText ? ongoingText : texts[3];
                statusText = statusText ? statusText : texts[4];
            }
            if (enactButton == null) enactButton = GetComponentInChildren<Button>(true);
            if (enactButton != null && enactButtonText == null)
                enactButtonText = enactButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Refresh(bool enacted, bool unlocked, bool canEnact)
        {
            if (statusText)
            {
                if (enacted)
                {
                    statusText.text = "ENACTED";
                    statusText.color = Color.green;
                }
                else if (!unlocked)
                {
                    statusText.text = "LOCKED";
                    statusText.color = Color.gray;
                }
                else
                {
                    statusText.text = "Available";
                    statusText.color = Color.yellow;
                }
            }

            if (enactButton)
            {
                enactButton.interactable = canEnact;
                enactButton.gameObject.SetActive(!enacted);
            }
            if (enactButtonText)
                enactButtonText.text = "Enact";
        }
    }
}
