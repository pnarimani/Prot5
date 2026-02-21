using System.Collections.Generic;
using System.Text;
using SiegeSurvival.Core;
using UnityEngine;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// "Why Am I Dying?" panel. Shows top 3 current pressures with exact numeric causality.
    /// </summary>
    public class WhyAmIDyingPanel : MonoBehaviour
    {
        [Header("Layout")]
        public TextMeshProUGUI headerText;
        public TextMeshProUGUI pressure1Text;
        public TextMeshProUGUI pressure2Text;
        public TextMeshProUGUI pressure3Text;

        private GameManager _gm;

        private void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            if (_gm == null || _gm.State == null) return;
            var s = _gm.State;
            var ctx = _gm.LastSimContext;

            if (headerText) headerText.text = "WHY AM I DYING?";

            var pressures = new List<(float urgency, string text)>();

            // 1. Resource depletion — for each resource estimate days until zero
            CheckResourceDepletion(pressures, "Food", s.food, ctx);
            CheckResourceDepletion(pressures, "Water", s.water, ctx);
            CheckResourceDepletion(pressures, "Fuel", s.fuel, ctx);
            CheckResourceDepletion(pressures, "Medicine", s.medicine, ctx);
            CheckResourceDepletion(pressures, "Materials", s.materials, ctx);

            // 2. Perimeter breach
            var perim = s.GetActivePerimeter();
            if (perim != null && !perim.definition.isKeep)
            {
                float dailyDamage = EstimateSiegeDamage(s, perim);
                if (dailyDamage > 0)
                {
                    float daysUntilBreach = perim.currentIntegrity / dailyDamage;
                    if (daysUntilBreach <= 5)
                    {
                        float urgency = daysUntilBreach <= 2 ? 100f : 80f - daysUntilBreach * 5;
                        pressures.Add((urgency,
                            $"<color=red>PERIMETER BREACH</color>: {perim.definition.zoneName} at {perim.currentIntegrity} integrity, taking ~{dailyDamage:F0}/day → breach in ~{daysUntilBreach:F1} days"));
                    }
                }
            }

            // 3. Unrest threshold
            if (s.unrest > 60)
            {
                float urgency = 70f + (s.unrest - 60);
                string causes = BuildUnrestCauses(s);
                pressures.Add((urgency,
                    $"<color=red>UNREST</color>: {s.unrest}/85 (revolt at 85). {causes}"));
            }

            // 4. Sickness spiral
            if (s.sickness > 50)
            {
                float urgency = 60f + (s.sickness - 50) * 0.5f;
                pressures.Add((urgency,
                    $"<color=yellow>SICKNESS</color>: {s.sickness}/100, growing ~{EstimateSicknessGrowth(s)}/day"));
            }

            // 5. Morale collapse
            if (s.morale < 35)
            {
                float urgency = 55f + (35 - s.morale);
                pressures.Add((urgency,
                    $"<color=yellow>MORALE COLLAPSE</color>: {s.morale}/100 — low morale reduces production and increases unrest"));
            }

            // 6. Overcrowding
            foreach (var z in s.zones)
            {
                if (!z.isLost && z.OvercrowdingPercent > 20)
                {
                    float urgency = 40f + z.OvercrowdingPercent * 0.3f;
                    int tiers = Mathf.FloorToInt(z.OvercrowdingPercent / 10f);
                    pressures.Add((urgency,
                        $"<color=yellow>OVERCROWDING</color>: {z.definition.zoneName} at {z.OvercrowdingPercent:F0}% over → +{tiers * 2} Unrest, +{tiers * 2} Sickness/day"));
                }
            }

            // Sort by urgency descending, take top 3
            pressures.Sort((a, b) => b.urgency.CompareTo(a.urgency));

            var textFields = new[] { pressure1Text, pressure2Text, pressure3Text };
            for (int i = 0; i < 3; i++)
            {
                if (textFields[i] == null) continue;
                if (i < pressures.Count)
                {
                    textFields[i].text = $"{i + 1}. {pressures[i].text}";
                    textFields[i].gameObject.SetActive(true);
                }
                else
                {
                    textFields[i].text = "";
                    textFields[i].gameObject.SetActive(false);
                }
            }

            // If no pressures
            if (pressures.Count == 0)
            {
                if (pressure1Text)
                {
                    pressure1Text.text = "<color=green>No critical pressures detected. Keep managing carefully.</color>";
                    pressure1Text.gameObject.SetActive(true);
                }
            }
        }

        private void CheckResourceDepletion(List<(float, string)> pressures, string name, int current, SimulationContext ctx)
        {
            if (current <= 0)
            {
                pressures.Add((95f, $"<color=red>{name} DEPLETED</color>: At zero! Immediate penalties active."));
                return;
            }

            // Estimate net burn from last simulation context
            int netBurn = EstimateNetBurn(name, ctx);
            if (netBurn <= 0) return; // net positive or break-even

            float daysLeft = current / (float)netBurn;
            if (daysLeft <= 3)
            {
                pressures.Add((90f - daysLeft * 5, $"<color=red>{name} CRITICAL</color>: {current} remaining, consuming ~{netBurn}/day → runs out in ~{daysLeft:F1} days"));
            }
            else if (daysLeft <= 7)
            {
                pressures.Add((50f - daysLeft, $"<color=yellow>{name} WARNING</color>: {current} remaining, consuming ~{netBurn}/day → runs out in ~{daysLeft:F1} days"));
            }
        }

        private int EstimateNetBurn(string resource, SimulationContext ctx)
        {
            if (ctx == null) return 0;
            switch (resource)
            {
                case "Food": return ctx.foodConsumed - ctx.foodProduced;
                case "Water": return ctx.waterConsumed - ctx.waterProduced;
                case "Fuel": return ctx.fuelConsumed - ctx.fuelProduced;
                case "Medicine": return Mathf.Max(0, ctx.medicineStart - _gm.State.medicine);
                case "Materials": return Mathf.Max(0, ctx.materialsStart - _gm.State.materials);
                default: return 0;
            }
        }

        private float EstimateSiegeDamage(GameState s, Data.Runtime.ZoneState perim)
        {
            float intensity = s.siegeIntensity;
            if (s.nightRaidDebuff != null)
                intensity = Mathf.Max(0, intensity - s.nightRaidDebuff.intensityReduction);
            float baseDmg = (3 + intensity) * perim.definition.perimeterFactor;
            int guardReduction = s.guards / 5;
            float l11Mult = s.enactedLaws.Contains(Data.LawId.L11_AbandonOuterRing) ? 0.8f : 1f;
            return Mathf.Max(0, baseDmg - guardReduction) * l11Mult;
        }

        private string BuildUnrestCauses(GameState s)
        {
            var sb = new StringBuilder("Causes: ");
            if (s.food <= 0) sb.Append("food deficit, ");
            if (s.water <= 0) sb.Append("water deficit, ");
            if (s.fuel <= 0) sb.Append("fuel deficit, ");
            if (s.morale < 50) sb.Append("low morale, ");
            if (s.AnyZoneOvercrowded) sb.Append("overcrowding, ");
            if (s.daysSinceLastLawEnacted >= 3) sb.Append("no law in 3+ days, ");
            if (s.IdlePercent > 10) sb.Append($"idle workers ({s.IdlePercent:F0}%), ");
            return sb.ToString().TrimEnd(',', ' ');
        }

        private int EstimateSicknessGrowth(GameState s)
        {
            const float TreatmentScaling = 0.10f;
            int growth = 2;
            if (s.fuel <= 0) growth += 10;
            foreach (var z in s.zones)
            {
                if (!z.isLost && z.IsOvercrowded)
                    growth += Mathf.FloorToInt(z.OvercrowdingPercent / 10) * 2;
            }
            int totalCapacity = s.workerAllocation[Data.JobSlot.Sanitation] / 5 + s.workerAllocation[Data.JobSlot.ClinicStaff] / 5;
            float treatmentEffect = totalCapacity * TreatmentScaling * s.sickness;
            growth -= Mathf.RoundToInt(treatmentEffect);
            return growth;
        }
    }
}
