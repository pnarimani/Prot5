using System;
using System.Collections.Generic;
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
        public static GameManager Instance { get; private set; }

        [Header("Zone Definitions (order 0-4)")]
        public ZoneDefinition[] zoneDefinitions; // must be 5, assigned in inspector

        // --- Runtime ---
        public GameState State { get; private set; }
        public GamePhase Phase { get; private set; }
        public CausalityLog Log { get; private set; }
        public RandomProvider Rng { get; private set; }
        public SimulationContext LastSimContext { get; private set; }
        public TelemetryLogger Telemetry { get; private set; }

        // --- Events (UI subscribes to these) ---
        public event Action OnPhaseChanged;
        public event Action OnStateChanged;
        public event Action<SimulationContext> OnDaySimulated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

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

            // Deduct pre-simulation costs
            DaySimulator.DeductPreSimulationCosts(State, Log);

            // Run simulation
            LastSimContext = DaySimulator.SimulateDay(State, Log, Rng);

            // Telemetry snapshot
            Telemetry.RecordDayEnd(State, Log);

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

        // --- Law Enactment ---

        public bool IsLawUnlocked(LawId lawId)
        {
            switch (lawId)
            {
                case LawId.L1_StrictRations: return true;
                case LawId.L2_DilutedWater: return State.water < 100; // or water deficit occurred
                case LawId.L3_ExtendedShifts: return State.currentDay >= 5;
                case LawId.L4_MandatoryGuardService: return State.unrest > 40;
                case LawId.L5_EmergencyShelters: return State.AnyZoneLost;
                case LawId.L6_PublicExecutions: return State.unrest > 60;
                case LawId.L7_FaithProcessions: return State.morale < 40;
                case LawId.L8_FoodConfiscation: return State.food < 100;
                case LawId.L9_MedicalTriage: return State.medicine < 20;
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

            // Check specific costs
            switch (lawId)
            {
                case LawId.L4_MandatoryGuardService:
                    if (State.healthyWorkers < 10) return false;
                    break;
                case LawId.L7_FaithProcessions:
                    if (State.materials < 10) return false;
                    break;
            }
            return true;
        }

        public void EnactLaw(LawId lawId)
        {
            if (!CanEnactLaw(lawId)) return;

            State.enactedLaws.Add(lawId);
            State.daysSinceLastLaw = 0;
            State.daysSinceLastLawEnacted = 0;

            // Apply immediate on-enact effects
            switch (lawId)
            {
                case LawId.L1_StrictRations:
                    State.morale -= 10;
                    break;
                case LawId.L2_DilutedWater:
                    State.morale -= 5;
                    break;
                case LawId.L3_ExtendedShifts:
                    State.morale -= 15;
                    break;
                case LawId.L4_MandatoryGuardService:
                    State.healthyWorkers -= 10;
                    State.guards += 10;
                    State.morale -= 10;
                    PopulationManager.ValidateWorkerAllocations(State);
                    break;
                case LawId.L5_EmergencyShelters:
                    State.unrest += 10;
                    break;
                case LawId.L6_PublicExecutions:
                    State.unrest -= 25;
                    State.morale -= 20;
                    // L6 priority: Healthy first
                    PopulationManager.ApplyDeathsHealthyFirst(State, 5, Log, "Public Executions (L6) enactment");
                    PopulationManager.RecomputeZonePopulationsAfterDeaths(State);
                    break;
                case LawId.L7_FaithProcessions:
                    State.materials -= 10;
                    State.morale += 15;
                    State.unrest += 5;
                    break;
                case LawId.L8_FoodConfiscation:
                    State.food += 100;
                    State.unrest += 20;
                    State.morale -= 20;
                    break;
                case LawId.L9_MedicalTriage:
                    // No immediate effect
                    break;
                case LawId.L10_Curfew:
                    // No immediate effect
                    break;
                case LawId.L11_AbandonOuterRing:
                    // Force Outer Farms lost
                    var farms = State.OuterFarms;
                    farms.isLost = true;
                    farms.currentIntegrity = 0;
                    // Apply natural on-loss shock
                    State.unrest += farms.definition.onLossUnrest; // +15
                    State.sickness += farms.definition.onLossSickness; // +10
                    State.morale += farms.definition.onLossMorale; // -10
                    // L11 extra
                    State.unrest += 15;
                    // Force population inward
                    PopulationManager.ForcePopulationInward(State, farms, Log);
                    break;
                case LawId.L12_MartialLaw:
                    // No immediate effect (ongoing caps applied during sim)
                    break;
            }

            State.morale = Mathf.Clamp(State.morale, 0, 100);
            State.unrest = Mathf.Clamp(State.unrest, 0, 100);
            State.sickness = Mathf.Clamp(State.sickness, 0, 100);

            Telemetry.RecordLawEnacted(lawId, State.currentDay);
            OnStateChanged?.Invoke();
        }

        // --- Emergency Orders ---

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

        public void IssueOrder(EmergencyOrderId orderId, int quarantineZone = -1)
        {
            if (!CanIssueOrder(orderId)) return;
            State.todayEmergencyOrder = orderId;
            if (orderId == EmergencyOrderId.O5_QuarantineDistrict)
            {
                State.quarantineZoneIndex = quarantineZone;
            }
            OnStateChanged?.Invoke();
        }

        public void CancelOrder()
        {
            State.todayEmergencyOrder = null;
            State.quarantineZoneIndex = -1;
            OnStateChanged?.Invoke();
        }

        // --- Missions ---

        public bool CanStartMission(MissionId missionId)
        {
            if (Phase != GamePhase.PlayerTurn) return false;
            if (State.activeMission != null) return false;
            if (State.healthyWorkers < 10) return false;
            return true;
        }

        public void StartMission(MissionId missionId)
        {
            if (!CanStartMission(missionId)) return;

            bool fuelInsufficient = (missionId == MissionId.M2_NightRaid && State.fuel < 40);
            State.activeMission = new ActiveMission(missionId, State.currentDay, 10, fuelInsufficient);

            // Validate worker allocations (10 workers now unavailable)
            PopulationManager.ValidateWorkerAllocations(State);
            OnStateChanged?.Invoke();
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

            State.morale = Mathf.Clamp(State.morale, 0, 100);
            State.unrest = Mathf.Clamp(State.unrest, 0, 100);
            State.sickness = Mathf.Clamp(State.sickness, 0, 100);

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
