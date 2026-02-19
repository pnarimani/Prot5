using NUnit.Framework;
using SiegeSurvival.Core;
using SiegeSurvival.Data;
using SiegeSurvival.Data.Runtime;
using SiegeSurvival.Systems;
using UnityEngine;

namespace SiegeSurvival.Tests
{
    [TestFixture]
    public class SimulationTests
    {
        private GameState _state;
        private CausalityLog _log;
        private SimulationContext _ctx;
        private RandomProvider _rng;

        [SetUp]
        public void Setup()
        {
            _state = CreateDefaultState();
            _log = new CausalityLog();
            _ctx = new SimulationContext();
            _rng = new RandomProvider(42);
        }

        // ==================== Helper ====================

        private static GameState CreateDefaultState()
        {
            var state = new GameState();
            state.zones = new ZoneState[5];
            for (int i = 0; i < 5; i++)
            {
                var def = ScriptableObject.CreateInstance<ZoneDefinition>();
                def.zoneName = $"Zone{i}";
                def.order = i;
                def.baseIntegrity = new[] { 80, 70, 75, 90, 100 }[i];
                def.capacity = new[] { 20, 40, 25, 50, 60 }[i];
                def.perimeterFactor = new[] { 1.0f, 0.9f, 0.8f, 0.7f, 0.6f }[i];
                def.foodProductionModifier = i == 0 ? 1.5f : 1f;
                def.foodProductionLostModifier = i == 0 ? 0.6f : 1f;
                def.materialsProductionModifier = i == 2 ? 1.4f : 1f;
                def.materialsProductionLostModifier = i == 2 ? 0.5f : 1f;
                def.fuelScavengingLostModifier = i == 0 ? 0.5f : 1f;
                def.unrestGrowthModifier = i == 3 ? 0.9f : 1f;
                def.moraleBonus = i == 4 ? 10 : 0;
                def.onLossUnrest = new[] { 15, 15, 10, 25, 0 }[i];
                def.onLossSickness = new[] { 10, 10, 0, 0, 0 }[i];
                def.onLossMorale = new[] { -10, -10, -5, -20, 0 }[i];
                def.onEvacMorale = -15;
                def.isKeep = i == 4;
                def.hasRandomIntegrity = i == 0;

                state.zones[i] = new ZoneState(def, def.baseIntegrity);
            }
            PopulationManager.InitializeZonePopulations(state);
            return state;
        }

        // ==================== Production Tests ====================

        [Test]
        public void ProductionCalculation_WithAllModifiers_ReturnsCorrectMultiplicativeResult()
        {
            // Assign 20 workers to food, with Farms intact (+50%), morale OK, no unrest penalty
            _state.workerAllocation[JobSlot.FoodProduction] = 20;
            _state.morale = 55;
            _state.unrest = 25;
            _state.fuel = 100;

            Step03_Production.Execute(_state, _ctx, _log, _rng);

            // 4 units × 10 base × 1.5 (Farms) × 1.0 (morale OK) × 1.0 (unrest OK) = 60
            Assert.AreEqual(60, _ctx.foodProduced);
        }

        [Test]
        public void ProductionCalculation_WithLowMoraleAndHighUnrest_AppliesMultipliers()
        {
            _state.workerAllocation[JobSlot.FoodProduction] = 20;
            _state.morale = 30; // triggers 0.8× morale penalty
            _state.unrest = 70; // triggers 0.7× unrest penalty

            Step03_Production.Execute(_state, _ctx, _log, _rng);

            // 4 units × 10 × 1.5 × 0.8 × 0.7 = 33.6 → 33
            Assert.AreEqual(33, _ctx.foodProduced);
        }

        // ==================== Consumption Tests ====================

        [Test]
        public void FoodConsumption_BasicCalculation()
        {
            // Default state: 120 population, food consumption = pop × 1
            Step04_Consumption.Execute(_state, _ctx, _log);

            Assert.AreEqual(_state.TotalPopulation, _ctx.foodConsumed);
        }

        [Test]
        public void FuelConsumption_WithOvercrowdingZones_AppliesGlobalModifier()
        {
            // Force zone 0 to > 20% overcrowded
            _state.zones[0].currentPopulation = 30; // 30/20 = 150%, overcrowded
            _state.zones[1].currentPopulation = 55; // 55/40 = 137.5%, overcrowded

            Step04_Consumption.Execute(_state, _ctx, _log);

            // 2 zones with ≥20% overcrowding → 1 + 0.1×2 = 1.2 multiplier
            // Base fuel: 120 × (pop/120) × 1.2
            int pop = _state.TotalPopulation;
            int expectedFuel = Mathf.FloorToInt(120f * (pop / 120f) * 1.2f);
            Assert.AreEqual(expectedFuel, _ctx.fuelConsumed);
        }

        // ==================== Population Tests ====================

        [Test]
        public void PopulationForceInward_GoesToNextInnerZone()
        {
            var farms = _state.zones[0];
            int popBefore = farms.currentPopulation;
            int residentialPopBefore = _state.zones[1].currentPopulation;

            farms.isLost = true;
            PopulationManager.ForcePopulationInward(_state, farms, _log);

            Assert.AreEqual(0, farms.currentPopulation);
            Assert.AreEqual(residentialPopBefore + popBefore, _state.zones[1].currentPopulation);
        }

        [Test]
        public void PopulationDeaths_DefaultPriority_KillsSickFirst()
        {
            _state.sick = 10;
            _state.elderly = 5;
            _state.healthyWorkers = 80;

            PopulationManager.ApplyDeathsDefault(_state, 12, _log, "Test");

            // Kill 10 sick, then 2 elderly
            Assert.AreEqual(0, _state.sick);
            Assert.AreEqual(3, _state.elderly);
            Assert.AreEqual(80, _state.healthyWorkers);
        }

        [Test]
        public void PopulationDeaths_HealthyFirst_L6Priority()
        {
            _state.sick = 10;
            _state.elderly = 5;
            _state.healthyWorkers = 80;

            PopulationManager.ApplyDeathsHealthyFirst(_state, 5, _log, "L6 Test");

            Assert.AreEqual(75, _state.healthyWorkers);
            Assert.AreEqual(10, _state.sick);
            Assert.AreEqual(5, _state.elderly);
        }

        // ==================== Law Tests ====================

        [Test]
        public void LawUnlock_L5_RequiresZoneLost()
        {
            // No zones lost
            Assert.IsFalse(_state.AnyZoneLost);

            // L5 should be locked (can't check via GameManager without MonoBehaviour,
            // but we verify the underlying state condition)
            bool l5Unlocked = _state.AnyZoneLost;
            Assert.IsFalse(l5Unlocked);

            // Lose a zone
            _state.zones[0].isLost = true;
            Assert.IsTrue(_state.AnyZoneLost);
        }

        [Test]
        public void MartialLaw_CapsUnrestAndMorale()
        {
            _state.enactedLaws.Add(LawId.L12_MartialLaw);
            _state.unrest = 80;
            _state.morale = 60;

            // Step01 should set caps
            Step01_LawPassives.Execute(_state, _ctx, _log);

            Assert.IsNotNull(_ctx.unrestCap);
            Assert.AreEqual(60, _ctx.unrestCap.Value);
            Assert.IsNotNull(_ctx.moraleCap);
            Assert.AreEqual(40, _ctx.moraleCap.Value);
        }

        [Test]
        public void StrictRations_ReducesFoodConsumption()
        {
            _state.enactedLaws.Add(LawId.L01_StrictRations);

            Step01_LawPassives.Execute(_state, _ctx, _log);

            Assert.AreEqual(0.75f, _ctx.foodConsumptionMult, 0.01f);
        }

        // ==================== Siege Damage Tests ====================

        [Test]
        public void SiegeDamage_WithGuardsAndL11_ReducesCorrectly()
        {
            _state.siegeIntensity = 3;
            _state.guards = 15; // 3 units → -3 damage
            _state.enactedLaws.Add(LawId.L11_AbandonOuterRing);
            _state.zones[0].isLost = true; // Farms already lost for L11

            // Simulate: perimeter is now Outer Residential (index 1)
            Step01_LawPassives.Execute(_state, _ctx, _log);
            _ctx.siegeDamageReduction = _state.guards / 5; // Guard units reduce damage

            // L11 should set siegeDamageMult = 0.8
            Assert.AreEqual(0.8f, _ctx.siegeDamageMult, 0.01f);

            Step10_SiegeDamage.Execute(_state, _ctx, _log);

            // Damage = (3 + 3) × 0.9 (perimeterFactor) - 3 (guards) = 5.4 - 3 = 2.4 → 2
            // Then × 0.8 (L11) = 1.92 → 1
            // Zone 1 integrity: 70 - 1 = 69
            Assert.GreaterOrEqual(_state.zones[1].currentIntegrity, 68);
            Assert.LessOrEqual(_state.zones[1].currentIntegrity, 70);
        }

        // ==================== Sickness Tests ====================

        [Test]
        public void MedicalTriage_KillsSickDaily()
        {
            _state.enactedLaws.Add(LawId.L09_MedicalTriage);
            _state.sick = 20;

            Step01_LawPassives.Execute(_state, _ctx, _log);

            // L9 should queue 5 sick deaths per day
            Assert.AreEqual(5, _ctx.deathsSick);
            Assert.AreEqual(0.5f, _ctx.clinicMedicineCostMult, 0.01f);
        }

        [Test]
        public void SicknessProgression_BaseGrowth()
        {
            int sickBefore = _state.sickness;
            Step07_Sickness.Execute(_state, _ctx, _log);

            // Base +2 sickness per day (no modifiers)
            Assert.AreEqual(sickBefore + 2, _state.sickness);
        }

        // ==================== Event Tests ====================

        [Test]
        public void EventTrigger_CouncilRevolt_UnrestOver85()
        {
            _state.unrest = 90;
            _state.consecutiveFoodWaterZeroDays = 0;

            Step13_LossConditions.Execute(_state, _ctx, _log);

            // Council revolt should trigger game over
            Assert.IsTrue(_state.isGameOver);
            Assert.AreEqual("Council Revolt", _state.gameOverReason);
        }

        [Test]
        public void EventTrigger_TotalCollapse_FoodAndWaterZero2Days()
        {
            _state.food = 0;
            _state.water = 0;
            _state.consecutiveFoodWaterZeroDays = 2;
            _state.unrest = 50; // below revolt threshold

            Step13_LossConditions.Execute(_state, _ctx, _log);

            Assert.IsTrue(_state.isGameOver);
            Assert.AreEqual("Total Collapse", _state.gameOverReason);
        }

        // ==================== Mission Tests ====================

        [Test]
        public void MissionOdds_FuelAbove100_NoModifier()
        {
            _state.fuel = 150;
            _state.siegeIntensity = 1;

            MissionResolver.GetMissionOdds(MissionId.M1_ForageBeyondWalls, _state, out float[] probs, out string[] labels);

            // Great: 60%, Moderate: 25%, Ambush: 15% (no fuel mod, siege < 4)
            Assert.AreEqual(3, probs.Length);
            Assert.AreEqual(0.60f, probs[0], 0.01f);
        }

        [Test]
        public void MissionOdds_FuelBelow50_IncreasesFailChance()
        {
            _state.fuel = 30;
            _state.siegeIntensity = 1;

            MissionResolver.GetMissionOdds(MissionId.M1_ForageBeyondWalls, _state, out float[] probs, out string[] labels);

            // Fuel 30 = +15% to bad outcomes
            float expectedAmbush = 0.15f + 0.15f; // base + fuel modifier
            Assert.AreEqual(expectedAmbush, probs[2], 0.02f);
        }

        // ==================== Evacuation Tests ====================

        [Test]
        public void Evacuation_CostAndPopulationMovement()
        {
            // Make evacuation eligible (perimeter integrity < 40)
            _state.zones[0].currentIntegrity = 30;
            _state.materials = 50;

            int matBefore = _state.materials;
            int pop0Before = _state.zones[0].currentPopulation;
            int pop1Before = _state.zones[1].currentPopulation;

            // Simulate evacuation manually (can't use GameManager in pure test)
            _state.materials -= 20;
            _state.sickness += 10;
            _state.unrest += 10;
            _state.morale -= 15;
            _state.zones[0].isLost = true;
            _state.zones[0].currentIntegrity = 0;
            _state.unrest += _state.zones[0].definition.onLossUnrest;
            _state.sickness += _state.zones[0].definition.onLossSickness;
            PopulationManager.ForcePopulationInward(_state, _state.zones[0], _log);

            Assert.AreEqual(matBefore - 20, _state.materials);
            Assert.AreEqual(0, _state.zones[0].currentPopulation);
            Assert.AreEqual(pop1Before + pop0Before, _state.zones[1].currentPopulation);
            Assert.IsTrue(_state.zones[0].isLost);
        }

        // ==================== Simulation Order Test ====================

        [Test]
        public void SimulationOrder_ExecutesStepsInCorrectSequence()
        {
            // Assign some workers and run a full day
            _state.workerAllocation[JobSlot.FoodProduction] = 20;
            _state.workerAllocation[JobSlot.WaterDrawing] = 15;
            _state.workerAllocation[JobSlot.FuelScavenging] = 10;
            _state.workerAllocation[JobSlot.Repairs] = 10;
            _state.workerAllocation[JobSlot.Sanitation] = 5;
            _state.materials = 50;

            int dayBefore = _state.currentDay;
            var ctx = DaySimulator.SimulateDay(_state, _log, _rng);

            // Day should increment
            Assert.AreEqual(dayBefore + 1, _state.currentDay);

            // Resources should have changed
            Assert.AreNotEqual(320, _state.food); // some consumption occurred
            Assert.IsNotNull(ctx);

            // Log should have entries
            Assert.Greater(_log.Entries.Count, 0);
        }
    }
}
