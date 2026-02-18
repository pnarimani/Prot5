using System.Text;
using SiegeSurvival.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SiegeSurvival.UI.Panels
{
    /// <summary>
    /// Full-screen overlay shown after simulation. Shows all resource/meter deltas with causality.
    /// </summary>
    public class DailyReportPanel : MonoBehaviour
    {
        [Header("Layout")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI reportBody;
        public ScrollRect scrollRect;
        public Button continueButton;

        private GameManager _gm;

        private void Awake()
        {
            if (continueButton)
                continueButton.onClick.AddListener(OnContinue);
        }

        private void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnDaySimulated += OnDaySimulated;
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnDaySimulated -= OnDaySimulated;
        }

        private void OnDaySimulated(SimulationContext ctx)
        {
            BuildReport(ctx);
        }

        private void OnEnable()
        {
            if (_gm != null && _gm.LastSimContext != null)
                BuildReport(_gm.LastSimContext);
        }

        private void BuildReport(SimulationContext ctx)
        {
            var s = _gm.State;
            var log = _gm.Log;
            var sb = new StringBuilder();

            // 1. Day Summary
            if (titleText) titleText.text = $"Day {s.currentDay - 1} Report";

            // 2. Resource Changes
            sb.AppendLine("<b>=== Resource Changes ===</b>");
            AppendResourceRow(sb, "Food", ctx.foodStart, ctx.foodProduced, ctx.foodConsumed, s.food);
            AppendResourceRow(sb, "Water", ctx.waterStart, ctx.waterProduced, ctx.waterConsumed, s.water);
            AppendResourceRow(sb, "Fuel", ctx.fuelStart, ctx.fuelProduced, ctx.fuelConsumed, s.fuel);
            AppendResourceDelta(sb, "Medicine", ctx.medicineStart, s.medicine);
            AppendResourceDelta(sb, "Materials", ctx.materialsStart, s.materials);
            sb.AppendLine();

            // 3. Production Breakdown
            sb.AppendLine("<b>=== Production Breakdown ===</b>");
            foreach (var e in log.GetByCategory(CausalityCategory.Production))
            {
                if (e.multiplier != 0)
                    sb.AppendLine($"  {e.description} (×{e.multiplier:F2})");
                else
                    sb.AppendLine($"  {e.description}");
            }
            sb.AppendLine();

            // 4. Consumption Breakdown
            sb.AppendLine("<b>=== Consumption Breakdown ===</b>");
            foreach (var e in log.GetByCategory(CausalityCategory.Consumption))
                sb.AppendLine($"  {e.description}");
            sb.AppendLine();

            // 5. Morale Breakdown
            sb.AppendLine("<b>=== Morale ===</b>");
            sb.AppendLine($"  Start: {ctx.moraleStart} → End: {s.morale} (Net: {s.morale - ctx.moraleStart:+0;-0;0})");
            foreach (var e in log.GetByCategory(CausalityCategory.Morale))
                sb.AppendLine($"  {e.source}: {(e.value >= 0 ? "+" : "")}{e.value}");
            sb.AppendLine();

            // 6. Unrest Breakdown
            sb.AppendLine("<b>=== Unrest ===</b>");
            sb.AppendLine($"  Start: {ctx.unrestStart} → End: {s.unrest} (Net: {s.unrest - ctx.unrestStart:+0;-0;0})");
            foreach (var e in log.GetByCategory(CausalityCategory.Unrest))
                sb.AppendLine($"  {e.source}: {(e.value >= 0 ? "+" : "")}{e.value}");
            sb.AppendLine();

            // 7. Sickness Breakdown
            sb.AppendLine("<b>=== Sickness ===</b>");
            sb.AppendLine($"  Start: {ctx.sicknessStart} → End: {s.sickness} (Net: {s.sickness - ctx.sicknessStart:+0;-0;0})");
            foreach (var e in log.GetByCategory(CausalityCategory.Sickness))
                sb.AppendLine($"  {e.source}: {(e.value >= 0 ? "+" : "")}{e.value}");
            sb.AppendLine();

            // 8. Siege Damage
            sb.AppendLine("<b>=== Siege Damage ===</b>");
            foreach (var e in log.GetByCategory(CausalityCategory.SiegeDamage))
                sb.AppendLine($"  {e.description}");
            foreach (var e in log.GetByCategory(CausalityCategory.Integrity))
                sb.AppendLine($"  {e.description}");
            sb.AppendLine();

            // 9. Zone status
            sb.AppendLine("<b>=== Zone Status ===</b>");
            foreach (var z in s.zones)
            {
                string status = z.isLost ? "LOST" :
                    (z == s.GetActivePerimeter() ? "PERIMETER" : "Intact");
                sb.AppendLine($"  {z.definition.zoneName}: {z.currentIntegrity}/{z.definition.baseIntegrity} [{status}] Pop: {z.currentPopulation}/{z.effectiveCapacity}");
            }
            sb.AppendLine();

            // 10. Events
            var events = log.GetByCategory(CausalityCategory.Event);
            if (events.Count > 0)
            {
                sb.AppendLine("<b>=== Events Triggered ===</b>");
                foreach (var e in events)
                    sb.AppendLine($"  <color=yellow>{e.description}</color>");
                sb.AppendLine();
            }

            // 11. Mission
            var missions = log.GetByCategory(CausalityCategory.Mission);
            if (missions.Count > 0)
            {
                sb.AppendLine("<b>=== Mission Result ===</b>");
                foreach (var e in missions)
                    sb.AppendLine($"  {e.description}");
                sb.AppendLine();
            }

            // 12. Deaths
            var deaths = log.GetByCategory(CausalityCategory.Death);
            if (deaths.Count > 0)
            {
                sb.AppendLine("<b>=== Deaths ===</b>");
                foreach (var e in deaths)
                    sb.AppendLine($"  {e.description}");
                sb.AppendLine();
            }

            // 13. Overcrowding
            var overcrowding = log.GetByCategory(CausalityCategory.Overcrowding);
            if (overcrowding.Count > 0)
            {
                sb.AppendLine("<b>=== Overcrowding ===</b>");
                foreach (var e in overcrowding)
                    sb.AppendLine($"  {e.description}");
                sb.AppendLine();
            }

            // 14. Population
            sb.AppendLine("<b>=== Population ===</b>");
            sb.AppendLine($"  Total: {s.TotalPopulation} (Healthy: {s.healthyWorkers}, Guards: {s.guards}, Sick: {s.sick}, Elderly: {s.elderly})");

            if (reportBody) reportBody.text = sb.ToString();
            if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
        }

        private void AppendResourceRow(StringBuilder sb, string name, int start, int produced, int consumed, int end)
        {
            int net = end - start;
            string netColor = net >= 0 ? "green" : "red";
            sb.AppendLine($"  {name}: {start} → {end}  (<color={netColor}>{net:+0;-0;0}</color>)  [+{produced} / -{consumed}]");
        }

        private void AppendResourceDelta(StringBuilder sb, string name, int start, int end)
        {
            int net = end - start;
            string netColor = net >= 0 ? "green" : "red";
            sb.AppendLine($"  {name}: {start} → {end}  (<color={netColor}>{net:+0;-0;0}</color>)");
        }

        private void OnContinue()
        {
            _gm?.ContinueFromReport();
        }
    }
}
