using System;
using System.Collections.Generic;
using System.Text;
using SiegeSurvival.Data;
using SiegeSurvival.Data.Runtime;
using SiegeSurvival.Systems;
using SiegeSurvival.Telemetry;
using UnityEngine;

namespace SiegeSurvival.Core
{
    /// <summary>
    /// Central game controller. Manages state machine, initialization, and player actions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance => FindFirstObjectByType<GameManager>();

        [Header("Zone Definitions (order 0-4)")]
        public ZoneDefinition[] zoneDefinitions; // must be 5, assigned in inspector

        // --- Runtime ---
        public GameState State { get; private set; }
        public GamePhase Phase { get; private set; }
        public CausalityLog Log { get; private set; }
        public RandomProvider Rng { get; private set; }
        public SimulationContext LastSimContext { get; private set; }
        public TelemetryLogger Telemetry { get; private set; }
        public IReadOnlyList<string> DayReports => _dayReports;

        // --- Events (UI subscribes to these) ---
        public event Action OnPhaseChanged;
        public event Action OnStateChanged;
        public event Action<SimulationContext> OnDaySimulated;
        public event Action OnScheduledActionChanged;

        readonly List<string> _dayReports = new();

        private void Start()
        {
            StartNewRun();
        }

        // ==================== Game Loop ====================

        public void StartNewRun()
        {
            Rng = new RandomProvider();
            Log = new CausalityLog();
            Telemetry = new TelemetryLogger();
            _dayReports.Clear();

            State = new GameState();
            InitializeRun();
            Phase = GamePhase.PlayerTurn;
            OnPhaseChanged?.Invoke();
            OnStateChanged?.Invoke();
        }

        private void InitializeRun()
        {
            // 1. Initialize zones from definitions
            State.zones = new ZoneState[5];
            for (int i = 0; i < 5; i++)
            {
                var def = zoneDefinitions[i];
                int integrity = def.baseIntegrity;
                if (def.hasRandomIntegrity)
                    integrity = Rng.Range(def.integrityRangeMin, def.integrityRangeMax + 1);
                State.zones[i] = new ZoneState(def, integrity);
            }

            // 2. Pick pressure profile
            State.activeProfile = (PressureProfileId)Rng.Range(0, 4);
            ApplyPressureProfile();

            // 3. Schedule 2 unique early incidents on Days 3-6
            ScheduleEarlyIncidents();

            // 4. Initialize zone populations (outer-to-inner)
            PopulationManager.InitializeZonePopulations(State);

            // 5. Telemetry init
            Telemetry.RecordSetup(State, Rng.Seed);
        }

        private void ApplyPressureProfile()
        {
            switch (State.activeProfile)
            {
                case PressureProfileId.P1_DiseaseWave:
                    State.sickness += 10;
                    State.medicine -= 10;
                    State.profileFoodConsumptionMult = 0.98f;
                    break;
                case PressureProfileId.P2_SupplySpoilage:
                    State.food -= 60;
                    State.unrest += 5;
                    State.materials += 10;
                    break;
                case PressureProfileId.P3_SabotagedWells:
                    State.wellsDamaged = true;
                    State.morale += 10;
                    State.unrest -= 10;
                    break;
                case PressureProfileId.P4_HeavyBombardment:
                    State.siegeIntensity = 2;
                    State.zones[0].currentIntegrity = 65; // Override Outer Farms
                    State.food += 40;
                    break;
            }
        }

        private void ScheduleEarlyIncidents()
        {
            var allIncidents = new List<EarlyIncidentId>
            {
                EarlyIncidentId.MinorFire,
                EarlyIncidentId.FeverCluster,
                EarlyIncidentId.FoodTheft,
                EarlyIncidentId.GuardDesertion
            };
            Rng.Shuffle(allIncidents);

            var days = new List<int> { 3, 4, 5, 6 };
            Rng.Shuffle(days);

            State.scheduledIncidents.Clear();
            State.scheduledIncidents.Add(new ScheduledIncident(allIncidents[0], days[0]));
            State.scheduledIncidents.Add(new ScheduledIncident(allIncidents[1], days[1]));
        }

        // ==================== End Day ====================

        public void EndDay()
        {
            if (Phase != GamePhase.PlayerTurn) return;

            Phase = GamePhase.Simulating;
            OnPhaseChanged?.Invoke();

            var scheduledActionSummary = GetScheduledActionSummary();

            // Execute scheduled player action
            ExecuteScheduledAction();

            // Deduct pre-simulation costs
            DaySimulator.DeductPreSimulationCosts(State, Log);

            // Run simulation
            LastSimContext = DaySimulator.SimulateDay(State, Log, Rng);

            // Telemetry snapshot
            Telemetry.RecordDayEnd(State, Log);
            _dayReports.Add(BuildDayReportEntry(LastSimContext, scheduledActionSummary));

            // Determine next phase
            if (State.isGameOver)
            {
                Phase = GamePhase.GameOver;
            }
            else if (State.isVictory)
            {
                Phase = GamePhase.Victory;
            }
            else
            {
                Phase = GamePhase.ShowReport;
            }

            OnDaySimulated?.Invoke(LastSimContext);
            OnPhaseChanged?.Invoke();
            OnStateChanged?.Invoke();
        }

        string GetScheduledActionSummary()
        {
            if (State.scheduledLaw.HasValue)
                return $"Law enacted: {State.scheduledLaw.Value}";

            if (State.scheduledOrder.HasValue)
            {
                if (State.scheduledOrder.Value == EmergencyOrderId.O5_QuarantineDistrict &&
                    State.scheduledQuarantineZone >= 0 &&
                    State.scheduledQuarantineZone < State.zones.Length)
                {
                    string zoneName = State.zones[State.scheduledQuarantineZone].definition.zoneName;
                    return $"Emergency order: {State.scheduledOrder.Value} ({zoneName})";
                }

                return $"Emergency order: {State.scheduledOrder.Value}";
            }

            if (State.scheduledMission.HasValue)
                return $"Mission started: {State.scheduledMission.Value}";

            return "No scheduled player action";
        }

        string BuildDayReportEntry(SimulationContext ctx, string scheduledActionSummary)
        {
            int day = State.currentDay - 1;

            var sb = new StringBuilder();
            sb.AppendLine($"=========== DAY {day} ==============");
            sb.AppendLine("<b>Player Actions:</b>");
            sb.AppendLine($"  • {scheduledActionSummary}");
            sb.AppendLine("<b>Simulation:</b>");

            int foodNet = State.food - ctx.foodStart;
            int waterNet = State.water - ctx.waterStart;
            int fuelNet = State.fuel - ctx.fuelStart;
            sb.AppendLine($"  Resources: Food {foodNet:+0;-0;0}, Water {waterNet:+0;-0;0}, Fuel {fuelNet:+0;-0;0}");

            int moraleNet = State.morale - ctx.moraleStart;
            int unrestNet = State.unrest - ctx.unrestStart;
            int sickNet = State.sickness - ctx.sicknessStart;
            sb.AppendLine($"  Meters: Morale {moraleNet:+0;-0;0}, Unrest {unrestNet:+0;-0;0}, Sickness {sickNet:+0;-0;0}");

            bool hasDetails = false;
            foreach (var entry in Log.Entries)
            {
                if (entry.category == CausalityCategory.General)
                    continue;

                hasDetails = true;
                sb.AppendLine($"  [{entry.category}] {entry.description}");
            }

            if (!hasDetails)
                sb.AppendLine("  (no logged events)");

            return sb.ToString();
        }

        public void ContinueFromReport()
        {
            if (Phase != GamePhase.ShowReport) return;
            Phase = GamePhase.PlayerTurn;
            OnPhaseChanged?.Invoke();
            OnStateChanged?.Invoke();
        }

        // ==================== Player Actions ====================

        // --- Worker Allocation ---

        public bool CanAllocateWorkers(JobSlot slot, int delta)
        {
            if (slot == JobSlot.GuardDuty) return false;
            int current = State.workerAllocation[slot];
            int newVal = current + delta;
            if (newVal < 0) return false;
            if (newVal % 5 != 0) return false;
            if (delta > 0 && State.IdleWorkers < delta) return false;
            return true;
        }

        public void AllocateWorkers(JobSlot slot, int delta)
        {
            if (!CanAllocateWorkers(slot, delta)) return;
            State.workerAllocation[slot] += delta;
            OnStateChanged?.Invoke();
        }

        // ==================== Scheduled Action System ====================

        public bool HasScheduledAction =>
            State.scheduledLaw.HasValue ||
            State.scheduledOrder.HasValue ||
            State.scheduledMission.HasValue;

        /// <summary>Clears any currently scheduled action and notifies listeners.</summary>
        public void ClearScheduledAction()
        {
            ClearScheduledActionInternal();
            OnScheduledActionChanged?.Invoke();
            OnStateChanged?.Invoke();
        }

        private void ClearScheduledActionInternal()
        {
            State.scheduledLaw = null;
            State.scheduledOrder = null;
            State.scheduledMission = null;
            State.scheduledQuarantineZone = -1;
        }

        private void ExecuteScheduledAction()
        {
            if (State.scheduledLaw.HasValue)
                ExecuteLaw(State.scheduledLaw.Value);
            else if (State.scheduledOrder.HasValue)
                ExecuteOrder(State.scheduledOrder.Value, State.scheduledQuarantineZone);
            else if (State.scheduledMission.HasValue)
                ExecuteMission(State.scheduledMission.Value);

            ClearScheduledActionInternal();
        }

        // --- Law Scheduling & Execution ---

        public bool IsLawUnlocked(LawId lawId)
        {
            switch (lawId)
            {
                case LawId.L01_StrictRations: return true;
                case LawId.L02_DilutedWater: return State.water < 100;
                case LawId.L03_ExtendedShifts: return State.currentDay >= 5;
                case LawId.L04_MandatoryGuardService: return State.unrest > 40;
                case LawId.L05_EmergencyShelters: return State.AnyZoneLost;
                case LawId.L06_PublicExecutions: return State.unrest > 60;
                case LawId.L07_FaithProcessions: return State.morale < 40;
                case LawId.L08_FoodConfiscation: return State.food < 100;
                case LawId.L09_MedicalTriage: return State.medicine < 20;
                case LawId.L10_Curfew: return State.unrest > 50;
                case LawId.L11_AbandonOuterRing:
                    return !State.OuterFarms.isLost && State.OuterFarms.currentIntegrity < 40;
                case LawId.L12_MartialLaw: return State.unrest > 75;
                default: return false;
            }
        }

        public bool CanEnactLaw(LawId lawId)
        {
            if (Phase != GamePhase.PlayerTurn) return false;
            if (State.enactedLaws.Contains(lawId)) return false;
            if (State.daysSinceLastLaw < 3) return false;
            if (!IsLawUnlocked(lawId)) return false;

            switch (lawId)
            {
                case LawId.L04_MandatoryGuardService:
                    if (State.healthyWorkers < 10) return false;
                    break;
                case LawId.L07_FaithProcessions:
                    if (State.materials < 10) return false;
                    break;
            }
            return true;
        }

        /// <summary>Schedules a law to be enacted at end of day. Replaces any previous scheduled action.</summary>
        public void EnactLaw(LawId lawId)
        {
            if (!CanEnactLaw(lawId)) return;
            ClearScheduledActionInternal();
            State.scheduledLaw = lawId;
            OnScheduledActionChanged?.Invoke();
            OnStateChanged?.Invoke();
        }

        private void ExecuteLaw(LawId lawId)
        {
            State.enactedLaws.Add(lawId);
            State.daysSinceLastLaw = 0;
            State.daysSinceLastLawEnacted = 0;

            switch (lawId)
            {
                case LawId.L01_StrictRations:
                    State.morale -= 10;
                    break;
                case LawId.L02_DilutedWater:
                    State.morale -= 5;
                    break;
                case LawId.L03_ExtendedShifts:
                    State.morale -= 15;
                    break;
                case LawId.L04_MandatoryGuardService:
                    State.healthyWorkers -= 10;
                    State.guards += 10;
                    State.morale -= 10;
                    PopulationManager.ValidateWorkerAllocations(State);
                    break;
                case LawId.L05_EmergencyShelters:
                    State.unrest += 10;
                    break;
                case LawId.L06_PublicExecutions:
                    State.unrest -= 25;
                    State.morale -= 20;
                    PopulationManager.ApplyDeathsHealthyFirst(State, 5, Log, "Public Executions (L6) enactment");
                    PopulationManager.RecomputeZonePopulationsAfterDeaths(State);
                    break;
                case LawId.L07_FaithProcessions:
                    State.materials -= 10;
                    State.morale += 15;
                    State.unrest += 5;
                    break;
                case LawId.L08_FoodConfiscation:
                    State.food += 100;
                    State.unrest += 20;
                    State.morale -= 20;
                    break;
                case LawId.L09_MedicalTriage:
                    break;
                case LawId.L10_Curfew:
                    break;
                case LawId.L11_AbandonOuterRing:
                    var farms = State.OuterFarms;
                    farms.currentIntegrity = 0;
                    ZoneLossHelper.TryApplyZoneLoss(State, farms, null, Log, "Abandon Outer Ring (L11)");
                    State.unrest += 15; // extra penalty for deliberate abandonment
                    break;
                case LawId.L12_MartialLaw:
                    break;
            }

            State.ClampAllMeters();

            Telemetry.RecordLawEnacted(lawId, State.currentDay);
        }

        // --- Emergency Order Scheduling & Execution ---

        public bool CanIssueOrder(EmergencyOrderId orderId)
        {
            if (Phase != GamePhase.PlayerTurn) return false;
            if (State.todayEmergencyOrder.HasValue) return false;

            switch (orderId)
            {
                case EmergencyOrderId.O1_DivertSuppliesToRepairs:
                    return State.food >= 30 && State.water >= 20;
                case EmergencyOrderId.O2_SoupKitchens:
                    return State.food >= 40;
                case EmergencyOrderId.O3_EmergencyWaterRation:
                    return true;
                case EmergencyOrderId.O4_CrackdownPatrols:
                    return true;
                case EmergencyOrderId.O5_QuarantineDistrict:
                    return true;
                case EmergencyOrderId.O6_InspireThePeople:
                    return State.materials >= 15;
                default: return false;
            }
        }

        /// <summary>Schedules an emergency order for end of day. Replaces any previous scheduled action.</summary>
        public void IssueOrder(EmergencyOrderId orderId, int quarantineZone = -1)
        {
            if (!CanIssueOrder(orderId)) return;
            ClearScheduledActionInternal();
            State.scheduledOrder = orderId;
            State.scheduledQuarantineZone = quarantineZone;
            OnScheduledActionChanged?.Invoke();
            OnStateChanged?.Invoke();
        }

        public void CancelOrder()
        {
            ClearScheduledAction();
        }

        private void ExecuteOrder(EmergencyOrderId orderId, int quarantineZone)
        {
            State.todayEmergencyOrder = orderId;
            if (orderId == EmergencyOrderId.O5_QuarantineDistrict)
                State.quarantineZoneIndex = quarantineZone;
        }

        // --- Mission Scheduling & Execution ---

        public bool CanStartMission(MissionId missionId)
        {
            if (Phase != GamePhase.PlayerTurn) return false;
            if (State.activeMission != null) return false;
            if (State.healthyWorkers < 10) return false;
            return true;
        }

        /// <summary>Schedules a mission to start at end of day. Replaces any previous scheduled action.</summary>
        public void StartMission(MissionId missionId)
        {
            if (!CanStartMission(missionId)) return;
            ClearScheduledActionInternal();
            State.scheduledMission = missionId;
            OnScheduledActionChanged?.Invoke();
            OnStateChanged?.Invoke();
        }

        private void ExecuteMission(MissionId missionId)
        {
            bool fuelInsufficient = (missionId == MissionId.M2_NightRaid && State.fuel < 40);
            State.activeMission = new ActiveMission(missionId, State.currentDay, 10, fuelInsufficient);
            PopulationManager.ValidateWorkerAllocations(State);
        }

        // --- Evacuation ---

        public bool CanEvacuate()
        {
            if (Phase != GamePhase.PlayerTurn) return false;
            if (State.materials < 20) return false;

            // Get active perimeter (must not be Keep)
            var perim = State.GetActivePerimeter();
            if (perim.definition.isKeep) return false;

            // Eligibility: any one of these conditions
            bool allOuterLost = State.OuterFarms.isLost && State.OuterResidential.isLost;
            bool integrityLow = perim.currentIntegrity < 40;
            bool siegeHigh = State.siegeIntensity >= 5;

            return allOuterLost || integrityLow || siegeHigh;
        }

        public void Evacuate()
        {
            if (!CanEvacuate()) return;

            var perim = State.GetActivePerimeter();

            // Evacuation cost
            State.materials -= 20;
            State.sickness += 10;
            State.unrest += 10;
            State.morale -= 15; // Replaces zone's natural morale loss

            // Zone on-loss effects (Unrest, Sickness) but use evacuation morale
            perim.isLost = true;
            perim.currentIntegrity = 0;
            State.unrest += perim.definition.onLossUnrest;
            State.sickness += perim.definition.onLossSickness;
            // morale uses onEvacMorale (already applied as -15 above, don't double-apply)

            // Move population inward
            PopulationManager.ForcePopulationInward(State, perim, Log);

            State.ClampAllMeters();

            Telemetry.RecordZoneLost(perim.definition.zoneName, State.currentDay);
            OnStateChanged?.Invoke();
        }

        // --- Repair Wells ---

        public bool CanRepairWells()
        {
            return Phase == GamePhase.PlayerTurn && State.wellsDamaged && State.materials >= 10;
        }

        public void RepairWells()
        {
            if (!CanRepairWells()) return;
            State.materials -= 10;
            State.wellsDamaged = false;
            OnStateChanged?.Invoke();
        }

        // ==================== Debug ====================

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5) && Phase == GamePhase.PlayerTurn)
            {
                EndDay();
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log($"[GameState] Day:{State.currentDay} Food:{State.food} Water:{State.water} Fuel:{State.fuel} Med:{State.medicine} Mat:{State.materials} " +
                          $"Morale:{State.morale} Unrest:{State.unrest} Sick:{State.sickness} Siege:{State.siegeIntensity} " +
                          $"Pop:{State.TotalPopulation}(H:{State.healthyWorkers} G:{State.guards} S:{State.sick} E:{State.elderly})");
            }
        }
#endif
    }
}
