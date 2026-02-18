# Plan: Siege Survival Prototype — Implementation Plan

## TL;DR

Build a fully playable 40-day siege survival city manager as a single-screen uGUI prototype in Unity 6 (URP 2D). The game is entirely UI-driven (no tilemap). The player allocates workers, enacts laws, issues emergency orders, and launches missions while a deterministic 13-step daily simulation erodes their position. All modifiers stack multiplicatively. Every change is causality-logged. The implementation is split into 6 phases: Data Model → Simulation Engine → Player Actions / Game Loop → UI → Telemetry & Debug → Tuning Validation.

---

## Folder Structure

```
Assets/
  Scripts/
    Core/
      GameManager.cs
      GameState.cs
      DaySimulator.cs
      SimulationContext.cs
      CausalityLog.cs
      PopulationManager.cs
      ModifierCalculator.cs
      RandomProvider.cs
    Data/
      Definitions/
        ZoneDefinition.cs          (ScriptableObject)
        LawDefinition.cs           (ScriptableObject)
        EmergencyOrderDefinition.cs(ScriptableObject)
        MissionDefinition.cs       (ScriptableObject)
        EventDefinition.cs         (ScriptableObject)
        PressureProfileDefinition.cs(ScriptableObject)
        EarlyIncidentDefinition.cs (ScriptableObject)
        JobSlot.cs                 (enum)
      Runtime/
        ZoneState.cs               (plain C# class)
        WorkerAllocation.cs        (plain C# class)
        ActiveMission.cs           (plain C# class)
        ScheduledIncident.cs       (plain C# class)
        NightRaidDebuff.cs         (plain C# class)
    Systems/
      Step01_LawPassives.cs
      Step02_EmergencyOrders.cs
      Step03_Production.cs
      Step04_Consumption.cs
      Step05_DeficitPenalties.cs
      Step06_Overcrowding.cs
      Step07_Sickness.cs
      Step08_Morale.cs
      Step09_Unrest.cs
      Step10_SiegeDamage.cs
      Step11_Repairs.cs
      Step12_Events.cs
      Step13_LossConditions.cs
      MissionResolver.cs
    UI/
      UIManager.cs
      Panels/
        TopBarPanel.cs
        ZonePanel.cs
        WorkerAllocationPanel.cs
        LawsPanel.cs
        EmergencyOrdersPanel.cs
        MissionsPanel.cs
        EvacuationPanel.cs
        DailyReportPanel.cs
        WhyAmIDyingPanel.cs
        GameOverPanel.cs
        VictoryPanel.cs
      Widgets/
        ZoneRingWidget.cs
        ResourceDisplay.cs
        MeterBar.cs
        StepperButton.cs
    Telemetry/
      TelemetryLogger.cs
  Data/
    Zones/
      (5 ZoneDefinition .asset files)
    Laws/
      (12 LawDefinition .asset files)
    EmergencyOrders/
      (6 EmergencyOrderDefinition .asset files)
    Missions/
      (4 MissionDefinition .asset files)
    Events/
      (7 EventDefinition .asset files)
    PressureProfiles/
      (4 PressureProfileDefinition .asset files)
    EarlyIncidents/
      (4 EarlyIncidentDefinition .asset files)
  Prefabs/
    UI/
      MainCanvas.prefab
      TopBarPanel.prefab
      ZonePanel.prefab
      WorkerAllocationPanel.prefab
      LawsPanel.prefab
      EmergencyOrdersPanel.prefab
      MissionsPanel.prefab
      EvacuationPanel.prefab
      DailyReportPanel.prefab
      WhyAmIDyingPanel.prefab
      GameOverPanel.prefab
      VictoryPanel.prefab
  Scenes/
    SiegePrototype.unity
```

---

## Phase 1 — Core Data Model & State

### 1.1 Enums — `Assets/Scripts/Data/Definitions/JobSlot.cs`

Define enum `JobSlot` with 8 values:

```
FoodProduction, WaterDrawing, MaterialsCrafting, Repairs,
Sanitation, GuardDuty, ClinicStaff, FuelScavenging
```

`GuardDuty` exists in the enum but is NOT player-allocated; all Guards are automatically on duty.

### 1.2 Zone Definitions — `Assets/Scripts/Data/Definitions/ZoneDefinition.cs`

ScriptableObject fields:

| Field | Type | Notes |
|---|---|---|
| `zoneName` | string | Display name |
| `order` | int | 0=Outer Farms … 4=Keep |
| `baseIntegrity` | int | Default starting integrity |
| `integrityRangeMin` | int | For random (Farms only: 70) |
| `integrityRangeMax` | int | For random (Farms only: 85) |
| `capacity` | int | Base capacity |
| `perimeterFactor` | float | Damage multiplier (1.0 / 0.9 / 0.8 / 0.7 / 0.6) |
| `foodProductionModifier` | float | 1.5 if Farms intact, 1.0 for others |
| `foodProductionLostModifier` | float | 0.6 if Farms lost (represents -40%) |
| `materialsProductionModifier` | float | 1.4 if Artisan intact, 1.0 for others |
| `materialsProductionLostModifier` | float | 0.5 if Artisan lost |
| `fuelScavengingLostModifier` | float | 0.5 if Farms lost, 1.0 for others |
| `unrestGrowthModifier` | float | -10% for Inner District → 0.9 |
| `moraleBonus` | int | +10 for Keep, 0 for others |
| `onLossUnrest` | int | |
| `onLossSickness` | int | |
| `onLossMorale` | int | Natural loss morale hit (negative) |
| `onEvacMorale` | int | Evacuation morale hit (negative, typically -15) |
| `onLossProductionDesc` | string | Description for UI |
| `isKeep` | bool | If true, loss = immediate game over |

Create 5 asset instances:

| Zone | Integrity | Capacity | PerimFactor | On-Loss: Unrest/Sick/Morale |
|---|---|---|---|---|
| Outer Farms | 70–85 (random) | 20 | 1.0 | +15 / +10 / -10 |
| Outer Residential | 70 | 40 | 0.9 | +15 / +10 / -10 |
| Artisan Quarter | 75 | 25 | 0.8 | +10 / 0 / -5 |
| Inner District | 90 | 50 | 0.7 | +25 / 0 / -20 |
| Keep | 100 | 60 | 0.6 | N/A (game over) |

### 1.3 Zone Runtime State — `Assets/Scripts/Data/Runtime/ZoneState.cs`

Plain C# class (not MonoBehaviour):

| Field | Type |
|---|---|
| `definition` | ZoneDefinition |
| `currentIntegrity` | int |
| `currentPopulation` | int |
| `isLost` | bool |
| `effectiveCapacity` | int (base + L5 bonus if Inner District) |

Properties:
- `OvercrowdingPercent`: `max(0, (currentPopulation - effectiveCapacity) / (float)effectiveCapacity * 100)`
- `OvercrowdingTiers10Pct`: `floor(OvercrowdingPercent / 10)` (for food consumption)
- `IsOvercrowded`: `currentPopulation > effectiveCapacity`
- `IsOvercrowded20Pct`: `currentPopulation > effectiveCapacity * 1.2` (for fuel modifier)

### 1.4 Law Definitions — `Assets/Scripts/Data/Definitions/LawDefinition.cs`

ScriptableObject. Each law is **unique** and identified by an enum `LawId` (L1–L12). Fields:

| Field | Type |
|---|---|
| `lawId` | LawId enum |
| `displayName` | string |
| `description` | string |
| `requirementsDescription` | string |
| `effectsDescription` | string |

Unlock logic and effects are **hardcoded** per `LawId` in `Step01_LawPassives.cs` and in the law enactment handler (not data-driven), because each law has unique conditional logic. The ScriptableObject is for display metadata only.

### 1.5 Emergency Order, Mission, Event, Profile, Incident Definitions

Same pattern: ScriptableObjects hold display metadata (`displayName`, `description`, `costDescription`, `effectDescription`). Gameplay logic is hardcoded per ID enum in the corresponding system scripts.

Enums: `EmergencyOrderId` (O1–O6), `MissionId` (M1–M4), `EventId` (E1–E7), `PressureProfileId` (P1–P4), `EarlyIncidentId` (MinorFire, FeverCluster, FoodTheft, GuardDesertion).

### 1.6 Game State — `Assets/Scripts/Core/GameState.cs`

Plain C# class holding ALL mutable state for one run:

| Field | Type | Initial |
|---|---|---|
| `currentDay` | int | 1 |
| `food` | int | 320 |
| `water` | int | 360 |
| `fuel` | int | 240 |
| `medicine` | int | 40 |
| `materials` | int | 120 |
| `morale` | int | 55 |
| `unrest` | int | 25 |
| `sickness` | int | 20 |
| `siegeIntensity` | int | 1 |
| `healthyWorkers` | int | 85 |
| `guards` | int | 10 |
| `sick` | int | 15 |
| `elderly` | int | 10 |
| `zones` | ZoneState[5] | See 1.3 |
| `enactedLaws` | List\<LawId\> | empty |
| `daysSinceLastLaw` | int | 3 (can enact on Day 1) |
| `todayEmergencyOrder` | EmergencyOrderId? | null |
| `quarantineZoneIndex` | int | -1 (for O5) |
| `activeMission` | ActiveMission | null |
| `nightRaidDebuff` | NightRaidDebuff | null (intensity reduction + days remaining) |
| `wellsDamaged` | bool | false |
| `activeProfile` | PressureProfileId | randomly selected |
| `scheduledIncidents` | List\<ScheduledIncident\> | 2 incidents on Days 3–6 |
| `consecutiveFoodWaterZeroDays` | int | 0 |
| `consecutiveFoodDeficitDays` | int | 0 |
| `daysSinceLastLawEnacted` | int | 0 (tracks 3-day no-law unrest) |
| `workerAllocation` | Dictionary\<JobSlot, int\> | all 0 |
| `isGameOver` | bool | false |
| `gameOverReason` | string | null |
| `isVictory` | bool | false |

**Population helpers:**
- `TotalPopulation` = healthyWorkers + guards + sick + elderly
- `AvailableWorkers` = healthyWorkers - (activeMission != null ? 10 : 0)
- `AssignedWorkers` = sum of workerAllocation values (excluding GuardDuty)
- `IdleWorkers` = AvailableWorkers - AssignedWorkers
- `IdlePercent` = IdleWorkers / (float)TotalPopulation * 100

### 1.7 Worker Allocation — `Assets/Scripts/Data/Runtime/WorkerAllocation.cs`

Validation rules:
- Each slot value must be a multiple of 5
- Total assigned across FoodProduction, WaterDrawing, MaterialsCrafting, Repairs, Sanitation, ClinicStaff, FuelScavenging ≤ `AvailableWorkers`
- GuardDuty is always = `guards` (not player-set)
- Clinic requires Medicine > 0 to be effective (warn in UI if not)
- Repairs requires Materials > 0 to be effective (warn in UI if not)

### 1.8 Active Mission — `Assets/Scripts/Data/Runtime/ActiveMission.cs`

| Field | Type |
|---|---|
| `missionId` | MissionId |
| `startDay` | int |
| `workersCommitted` | int (always 10) |

Missions resolve at end of the day they are started (Step 12, after events? Actually missions resolve at "Day end" per spec. I'll resolve them between Step 12 and Step 13, in a sub-step 12b).

### 1.9 Night Raid Debuff — `Assets/Scripts/Data/Runtime/NightRaidDebuff.cs`

| Field | Type |
|---|---|
| `intensityReduction` | int (10 or 5) |
| `daysRemaining` | int (starts at 3) |

Decremented each day at the start of the siege step. When 0, remove.

### 1.10 Simulation Context — `Assets/Scripts/Core/SimulationContext.cs`

A transient struct/class created fresh each day to accumulate modifiers before applying them. Fields:

| Field | Type | Purpose |
|---|---|---|
| `foodProductionMult` | float (starts 1.0) | Accumulated production multiplier |
| `waterProductionMult` | float (starts 1.0) | |
| `materialsProductionMult` | float (starts 1.0) | |
| `fuelProductionMult` | float (starts 1.0) | |
| `repairOutputMult` | float (starts 1.0) | |
| `foodConsumptionMult` | float (starts 1.0) | |
| `waterConsumptionMult` | float (starts 1.0) | |
| `fuelConsumptionMult` | float (starts 1.0) | (not currently used by anything, reserved) |
| `flatFoodConsumption` | int (starts 0) | L4 adds +15 |
| `sicknessDelta` | int (starts 0) | Flat daily sickness additions from laws/orders |
| `unrestDelta` | int (starts 0) | Flat daily unrest additions from laws/orders |
| `moraleDelta` | int (starts 0) | Flat daily morale changes from laws/orders |
| `deathsSick` | int (starts 0) | Deaths among Sick this day |
| `deathsDefault` | int (starts 0) | Deaths using default priority |
| `deathsHealthyFirst` | int (starts 0) | Deaths using L6 priority |
| `clinicMedicineCostMult` | float (starts 1.0) | L9 halves it |
| `siegeDamageMult` | float (starts 1.0) | L11 |
| `unrestCap` | int? (null) | L12 caps at 60 |
| `moraleCap` | int? (null) | L12 caps at 40 |
| `allProductionMult` | float (starts 1.0) | Applies to Food/Water/Materials/Fuel/Repairs |
| `sanitationReduction` | int | From Sanitation workers |
| `clinicReduction` | int | From Clinic workers |

### 1.11 Causality Log — `Assets/Scripts/Core/CausalityLog.cs`

A list of `CausalityEntry` records for the current day:

```
struct CausalityEntry {
    string category;    // "Food", "Morale", "Unrest", "Sickness", "Integrity", etc.
    string source;      // "Strict Rations (L1)", "Overcrowding in Inner District", etc.
    int value;          // +5, -10, etc. (for flat changes)
    float multiplier;   // 0.75, 1.25, etc. (for production/consumption modifiers)
    string description; // Human-readable summary
}
```

Every system step appends entries. The Daily Report panel reads this log.

### 1.12 Random Provider — `Assets/Scripts/Core/RandomProvider.cs`

Wraps `System.Random` with a seed (logged by Telemetry). Methods:
- `int Range(int min, int maxExclusive)`
- `float Range01()`
- `bool Chance(float probability)` — returns true if roll < probability

Use a single seed per run for reproducibility.

---

## Phase 2 — Simulation Engine (13 Steps)

### 2.0 Pre-Simulation: Emergency Order Costs

Before the 13-step loop, deduct emergency order costs (if one was selected):

| Order | Immediate Cost |
|---|---|
| O1 | Food -30, Water -20 |
| O2 | Food -40 |
| O3 | (no resource cost; sickness applied in step) |
| O4 | (deaths + morale applied in step) |
| O5 | (no resource cost) |
| O6 | Materials -15 |

Clamp resources to 0 minimum after deduction. Log each deduction.

Also deduct Night Raid fuel cost (M2) if mission is Night Raid: Fuel -40 (clamp to 0).

### 2.1 Step 1 — Law Passives (`Step01_LawPassives.cs`)

Iterate `GameState.enactedLaws`. For each law, apply ongoing modifiers to `SimulationContext`:

| Law | Modifier on SimulationContext |
|---|---|
| L1 Strict Rations | `foodConsumptionMult ×= 0.75`; `unrestDelta += 5` |
| L2 Diluted Water | `waterConsumptionMult ×= 0.8`; `sicknessDelta += 5` |
| L3 Extended Shifts | `foodProductionMult ×= 1.25`; `waterProductionMult ×= 1.25`; `materialsProductionMult ×= 1.25`; `fuelProductionMult ×= 1.25`; `sicknessDelta += 8` |
| L4 Mandatory Guard | `flatFoodConsumption += 15` |
| L5 Emergency Shelters | Inner District `effectiveCapacity += 30`; `sicknessDelta += 10` |
| L6 Public Executions | (no ongoing) |
| L7 Faith Processions | (no ongoing) |
| L8 Food Confiscation | (no ongoing) |
| L9 Medical Triage | `clinicMedicineCostMult ×= 0.5`; `deathsSick += 5` (kill 5 Sick/day; if Sick < 5, kill all Sick) |
| L10 Curfew | `unrestDelta -= 10`; `allProductionMult ×= 0.8` |
| L11 Abandon Outer Ring | `siegeDamageMult ×= 0.8` |
| L12 Martial Law | `unrestCap = 60`; `moraleCap = 40` |

Log every modifier with source attribution.

### 2.2 Step 2 — Emergency Order Effects (`Step02_EmergencyOrders.cs`)

If `todayEmergencyOrder` is set:

| Order | Effect on SimulationContext |
|---|---|
| O1 Divert Supplies | `repairOutputMult ×= 1.5`; also fixes wells if damaged (`wellsDamaged = false`) |
| O2 Soup Kitchens | `unrestDelta -= 15` |
| O3 Emergency Water | `waterConsumptionMult ×= 0.5`; `sicknessDelta += 10` |
| O4 Crackdown Patrols | `unrestDelta -= 20`; `deathsDefault += 2`; `moraleDelta -= 10` |
| O5 Quarantine District | `allProductionMult ×= 0.5`; `sicknessDelta -= 10` |
| O6 Inspire the People | `moraleDelta += 15` |

### 2.3 Step 3 — Production (`Step03_Production.cs`)

**Pre-check**: If fuel ≤ 0 at start of this step, add `foodProductionMult ×= 0.85` (Fuel penalty: -15% Food production). Log it.

For each job slot with assigned workers (let `units = assignedWorkers / 5`):

#### 3a. Food Production

```
baseFoodPerUnit = 10
zoneMult = OuterFarms.isLost ? OuterFarms.foodProductionLostModifier : OuterFarms.foodProductionModifier
                 // Lost: 0.6, Intact: 1.5
moraleMult = (morale < 40) ? 0.8 : 1.0
unrestMult = (unrest > 60) ? 0.7 : 1.0
fuelMult   = (fuel <= 0) ? 0.85 : 1.0   // already applied above via foodProductionMult

totalMult = ctx.foodProductionMult × ctx.allProductionMult × zoneMult × moraleMult × unrestMult
produced = floor(units × baseFoodPerUnit × totalMult)
```

Add `produced` to `food`. Log each multiplier.

#### 3b. Water Drawing

```
baseWaterPerUnit = 12
wellsMult = wellsDamaged ? 0.5 : 1.0

totalMult = ctx.waterProductionMult × ctx.allProductionMult × wellsMult
produced = floor(units × baseWaterPerUnit × totalMult)
```

#### 3c. Materials Crafting

```
baseMaterialsPerUnit = 8
zoneMult = ArtisanQuarter.isLost ? ArtisanQuarter.materialsProductionLostModifier : ArtisanQuarter.materialsProductionModifier
           // Lost: 0.5, Intact: 1.4

totalMult = ctx.materialsProductionMult × ctx.allProductionMult × zoneMult
produced = floor(units × baseMaterialsPerUnit × totalMult)
```

#### 3d. Repairs

```
baseRepairPerUnit = 8
materialCostPerUnit = 4

totalMult = ctx.repairOutputMult × ctx.allProductionMult
            // Note: L10's allProductionMult (×0.8) applies here
repairAmount = floor(units × baseRepairPerUnit × totalMult)
materialCost = units × materialCostPerUnit
```

Deduct `materialCost` from `materials` (clamp to 0). Repair amount is stored; actual application is Step 11. If materials insufficient for full cost, scale repair proportionally: `repairAmount = floor(repairAmount × (materials / materialCost))`, deduct all available materials. Log.

#### 3e. Sanitation

```
baseSanitationPerUnit = 5
ctx.sanitationReduction = units × baseSanitationPerUnit
```

No production multipliers apply to Sanitation (it's a service, not production).

#### 3f. Guard Duty (automatic)

```
guardUnits = guards / 5   // integer division
siegeDamageReduction = guardUnits × 1
unrestReduction = guardUnits × 3
```

Store `siegeDamageReduction` for Step 10. Apply `ctx.unrestDelta -= unrestReduction`.

#### 3g. Clinic Staff

```
baseClinicPerUnit = 8
baseMedicineCostPerUnit = 5

adjustedMedicineCost = ceil(baseMedicineCostPerUnit × ctx.clinicMedicineCostMult)
                        // Normal: 5, with L9: ceil(2.5) = 3
totalMedicineCost = units × adjustedMedicineCost
```

If `medicine < totalMedicineCost`: scale down — effective units = `floor(medicine / adjustedMedicineCost)`, deduct `effective units × adjustedMedicineCost` from medicine. Else deduct full cost.

```
ctx.clinicReduction = effectiveUnits × baseClinicPerUnit
```

#### 3h. Fuel Scavenging

```
baseFuelPerUnit = 15
zoneMult = OuterFarms.isLost ? 0.5 : 1.0

totalMult = ctx.fuelProductionMult × ctx.allProductionMult × zoneMult
produced = floor(units × baseFuelPerUnit × totalMult)
```

**Death risk**: If `siegeIntensity >= 4` AND `units > 0`: roll `RandomProvider.Chance(0.20)`. If true: `deathsDefault += 2`. Log "Fuel scavenging ambush: 2 deaths."

### 2.4 Step 4 — Consumption (`Step04_Consumption.cs`)

#### 4a. Food Consumption

```
totalFood = 0
for each non-lost zone:
    overcrowdingTiers = zone.OvercrowdingTiers10Pct  // each 10% above capacity
    zoneFoodMult = 1.0 + (0.05 × overcrowdingTiers)
    totalFood += ceil(zone.currentPopulation × 1.0 × zoneFoodMult)

totalFood = ceil(totalFood × ctx.foodConsumptionMult) + ctx.flatFoodConsumption
```

Deduct from `food`. If `food` goes below 0, set to 0, mark `foodDeficit = true`.

#### 4b. Water Consumption

```
totalWater = ceil(TotalPopulation × 1.0 × ctx.waterConsumptionMult)
```

Deduct from `water`. If below 0, set to 0, mark `waterDeficit = true`.

#### 4c. Fuel Consumption

```
zonesOver20Pct = count of non-lost zones where IsOvercrowded20Pct
overcrowdingFuelMod = 1.0 + (0.10 × zonesOver20Pct)

totalFuel = ceil(120.0 × (TotalPopulation / 120.0) × overcrowdingFuelMod)
```

No global fuel consumption multiplier currently exists, but the field is reserved. Deduct from `fuel`. Fuel CAN go negative (track the value but for gameplay treat ≤ 0 as "out of fuel"). Actually, clamp to 0. Mark `fuelDeficit = true` if result was ≤ 0 before clamping.

Log all consumption with per-zone breakdown for food.

### 2.5 Step 5 — Deficit Penalties (`Step05_DeficitPenalties.cs`)

Check resource deficits after consumption:

**Food deficit** (food = 0):
- `ctx.moraleDelta -= 5`
- `consecutiveFoodDeficitDays += 1`
- Log "Food deficit: Morale -5"

If no food deficit: `consecutiveFoodDeficitDays = 0`

**Water deficit** (water = 0):
- `ctx.moraleDelta -= 5`
- Log "Water deficit: Morale -5"

**Fuel deficit** (fuel = 0):
- `ctx.sicknessDelta += 10`
- `ctx.moraleDelta -= 10`
- `ctx.unrestDelta += 5`
- Log all three

**Consecutive Food+Water = 0** tracking:
If `food = 0 AND water = 0`: `consecutiveFoodWaterZeroDays += 1`, else reset to 0.

### 2.6 Step 6 — Overcrowding Penalties (`Step06_Overcrowding.cs`)

For each non-lost zone where `currentPopulation > effectiveCapacity`:

```
overcrowdingPercent = (currentPopulation - effectiveCapacity) / (float)effectiveCapacity × 100
tiers10 = floor(overcrowdingPercent / 10)

ctx.unrestDelta += 2 × tiers10
ctx.sicknessDelta += 2 × tiers10
```

Log per zone: "Overcrowding in {zone}: {percent}% → +{unrest} Unrest, +{sickness} Sickness"

(Food/Fuel overcrowding effects already applied in Step 4 consumption calculations.)

### 2.7 Step 7 — Sickness Progression (`Step07_Sickness.cs`)

```
baseSickness = 2  // always +2/day
totalSicknessChange = baseSickness + ctx.sicknessDelta - ctx.sanitationReduction - ctx.clinicReduction

sickness = clamp(sickness + totalSicknessChange, 0, 100)
```

Log each component: base +2, law additions, overcrowding additions, fuel deficit, sanitation reduction, clinic reduction. Show net change.

Apply L9 deaths: if `deathsSick > 0`, kill min(`deathsSick`, `sick`) from Sick population. Log deaths.

### 2.8 Step 8 — Morale Progression (`Step08_Morale.cs`)

Collect all morale changes:

```
moraleDelta = ctx.moraleDelta  // already includes law/order/deficit/zone contributions

// Sickness > 60 penalty
if sickness > 60: moraleDelta -= 3

// Overcrowding present (any zone)
if any non-lost zone is overcrowded: moraleDelta -= 2

// Keep intact bonus
if Keep is not lost: moraleDelta += 10  // Keep's moraleBonus

// Recovery check
noDeficits = (food > 0 AND water > 0 AND fuel > 0)
noOvercrowding = no zone is overcrowded
if noDeficits AND noOvercrowding AND sickness < 30 AND unrest < 40:
    moraleDelta += 2

morale = clamp(morale + moraleDelta, 0, 100)

// L12 Martial Law cap
if ctx.moraleCap != null:
    morale = min(morale, ctx.moraleCap)
```

Wait — the Keep bonus "+10 Morale while intact" — is this applied every day as +10? That seems very strong. Re-reading the spec: "Keep: +10 Morale while intact." This is a daily +10 bonus. OK, it makes sense as a pressure source when it's threatened to be lost.

Actually, re-reading: this feels like it might be a passive state (morale floor/bonus) rather than +10/day. But the spec says "+10 Morale while intact" under the zone data, parallel to "+50% Food production while intact" for Farms. The food one is clearly a modifier, not +50 food/day. So "+10 Morale while intact" is likely a daily +10 to the morale delta. This is counterbalanced by the many morale drains. I'll implement it as +10/day.

Actually wait. Let me reconsider. If it's +10/day, on Day 1 with no other penalties, morale would jump to 65 immediately. The initial state is 55. With +10/day from Keep, morale would climb fast. But recovery is only +2 and requires many conditions met. Let me look more carefully.

The morale section says:
- Conditional drift: various -5, -10, -3, -2, -10, -15 sources
- Recovery: +2 if no deficits, no overcrowding, Fuel>0, Sickness<30, Unrest<40
- Keep: +10 while intact

Given the "no flat decay" rule — morale doesn't decay on its own, only conditionally. The +10 from Keep acts as a stabilizer that offsets some conditional losses. Once Keep is threatened/lost, morale collapses. This seems intentional. I'll implement as daily +10/day while Keep is intact.

Log every component.

### 2.9 Step 9 — Unrest Progression (`Step09_Unrest.cs`)

```
unrestDelta = ctx.unrestDelta  // laws, orders, guards

// Base +1 conditions (each adds +1, they stack)
if foodDeficit: unrestDelta += 1
if waterDeficit: unrestDelta += 1     // "any deficit"
if fuelDeficit: unrestDelta += 1
if any zone overcrowded: unrestDelta += 1
if morale < 50: unrestDelta += 1
if daysSinceLastLawEnacted > 3: unrestDelta += 1  // "No law enacted in 3 days"

// Idle worker penalty
idlePercent = IdleWorkers / (float)TotalPopulation × 100
if idlePercent > 20: unrestDelta += 5
else if idlePercent > 10: unrestDelta += 2

// Inner District intact bonus
if InnerDistrict is not lost:
    // "-10% Unrest growth while intact" = multiply positive unrest growth by 0.9
    if unrestDelta > 0: unrestDelta = floor(unrestDelta × 0.9)

unrest = clamp(unrest + unrestDelta, 0, 100)

// L12 Martial Law cap
if ctx.unrestCap != null:
    unrest = min(unrest, ctx.unrestCap)
```

Log every component. The "No law enacted in 3 days" check uses `daysSinceLastLawEnacted` which increments each day and resets to 0 when a law is enacted.

### 2.10 Step 10 — Siege Damage (`Step10_SiegeDamage.cs`)

```
// Determine effective intensity
effectiveIntensity = siegeIntensity
if nightRaidDebuff != null:
    effectiveIntensity = max(0, effectiveIntensity - nightRaidDebuff.intensityReduction)
    nightRaidDebuff.daysRemaining -= 1
    if nightRaidDebuff.daysRemaining <= 0: nightRaidDebuff = null

// Intensity schedule: +1 every 6 days
if currentDay > 1 AND (currentDay - 1) % 6 == 0:
    siegeIntensity = min(siegeIntensity + 1, 6)

// Calculate damage
activePerimeter = outermost non-lost zone
baseDamage = 3 + effectiveIntensity
perimFactor = activePerimeter.definition.perimeterFactor
guardReduction = guardUnits × 1  // from Step 3

rawDamage = baseDamage × perimFactor
afterGuards = max(0, rawDamage - guardReduction)
finalDamage = floor(afterGuards × ctx.siegeDamageMult)

activePerimeter.currentIntegrity -= finalDamage
```

Log: base damage, perimeter factor, guard reduction, L11 multiplier, final damage, resulting integrity.

**Zone loss check** (if integrity ≤ 0):

```
if activePerimeter.currentIntegrity <= 0:
    activePerimeter.currentIntegrity = 0
    if activePerimeter.definition.isKeep:
        // Handled in Step 13 (loss condition)
        mark keepBreached = true
    else:
        activePerimeter.isLost = true
        // Apply on-loss effects (natural)
        unrest += activePerimeter.definition.onLossUnrest
        sickness += activePerimeter.definition.onLossSickness
        morale += activePerimeter.definition.onLossMorale
        // Apply production loss effects (stored in zone definition flags)
        // Move population inward
        PopulationManager.ForcePopulationInward(activePerimeter)
```

Log all on-loss effects.

### 2.11 Step 11 — Repairs (`Step11_Repairs.cs`)

Apply repair amount (calculated in Step 3) to active perimeter zone:

```
activePerimeter.currentIntegrity = min(
    activePerimeter.currentIntegrity + repairAmount,
    activePerimeter.definition.baseIntegrity  // cap at starting integrity, not above
)
```

Repairs can only target the active perimeter (outermost non-lost zone). Log repair amount and resulting integrity.

### 2.12 Step 12 — Events (`Step12_Events.cs`)

Check each event trigger in order. Multiple events CAN fire on the same day.

#### E1. Hunger Riot
- Trigger: `consecutiveFoodDeficitDays >= 2 AND unrest > 50`
- Effect: `food -= 80` (clamp 0); 5 deaths (default priority); `unrest += 15`
- Log: "Hunger Riot triggered: 2+ days food deficit + Unrest > 50"

#### E2. Fever Outbreak
- Trigger: `sickness > 60`
- Effect: 10 deaths (default priority); `unrest += 10`
- Log cause

#### E3. Desertion Wave
- Trigger: `morale < 30`
- Effect: Remove min(10, healthyWorkers) Healthy Workers permanently; unassign them from jobs if needed
- Log cause

#### E4. Wall Breach Attempt
- Trigger: active perimeter `currentIntegrity < 30`
- Effect: If `guards >= 15` → negated (log "Wall Breach negated by guards"); else `activePerimeter.currentIntegrity -= 15` (can trigger zone loss — if so, apply loss effects again)
- Log cause

#### E5. Fire in Artisan Quarter
- Trigger: `siegeIntensity >= 4 AND RandomProvider.Chance(0.10)`
- Effect: `materials -= 50` (clamp 0); if Artisan Quarter not lost: `ArtisanQuarter.currentIntegrity -= 10`
- Log cause

#### E6. Council Revolt (Loss)
- Trigger: `unrest > 85`
- Effect: Game Over — handled in Step 13

#### E7. Total Collapse (Loss)
- Trigger: `consecutiveFoodWaterZeroDays >= 2`
- Effect: Game Over — handled in Step 13

#### Early Incidents (separate from main events)
- Check if `currentDay` matches any scheduled incident day
- Apply incident effect:
  - Minor Fire: `materials -= 20`
  - Fever Cluster: `sickness += 8`
  - Food Theft: `food -= 40`; `unrest += 5`
  - Guard Desertion: `guards -= 5` (min 0); `unrest += 5`
- Show warning the day before (in Daily Report or as a toast)

#### Mission Resolution (sub-step 12b)
If `activeMission != null`:

```
fuelRiskMod = 0
if fuel >= 100: fuelRiskMod = 0
else if fuel >= 50: fuelRiskMod = 0.05
else if fuel >= 1: fuelRiskMod = 0.15
else: fuelRiskMod = 0.25
```

##### M1. Forage Beyond Walls
```
baseAmbush = 0.15
if siegeIntensity >= 4: baseAmbush = 0.30
adjustedAmbush = baseAmbush + fuelRiskMod

// Normalize remaining outcomes proportionally
remainingGood = 1.0 - adjustedAmbush
ratioGreat = 0.60 / (0.60 + 0.25)  // if base; use original non-ambush ratio
ratioOk = 0.25 / (0.60 + 0.25)
if siegeIntensity >= 4:
    ratioGreat = 0.5294 / (0.5294 + 0.1706)  // pre-fuel-mod ratios
    ratioOk = 0.1706 / (0.5294 + 0.1706)

chanceGreat = remainingGood × ratioGreat
chanceOk = remainingGood × ratioOk

roll = RandomProvider.Range01()
if roll < adjustedAmbush: // Ambushed
    kill 5 (default priority)
else if roll < adjustedAmbush + chanceOk: // +80 Food
    food += 80
else: // +120 Food
    food += 120
```

##### M2. Night Raid on Siege Camp
```
baseCaptured = 0.20
if fuel was < 40 when mission started: baseCaptured += 0.20
adjustedCaptured = baseCaptured + fuelRiskMod

remaining = 1.0 - adjustedCaptured
ratio40 = 0.40 / 0.80  // original good outcomes
ratio40b = 0.40 / 0.80

chanceGreat = remaining × ratio40
chanceOk = remaining × ratio40b

roll = RandomProvider.Range01()
if roll < adjustedCaptured: // Captured
    kill 8 (default priority); unrest += 15; siegeIntensity = min(siegeIntensity + 1, 6)
else if roll < adjustedCaptured + chanceOk: // -5 intensity for 3 days
    nightRaidDebuff = { intensityReduction: 5, daysRemaining: 3 }
else: // -10 intensity for 3 days  (was already accounted for: the "Siege Intensity -10" is ×1 debuff)
    nightRaidDebuff = { intensityReduction: 10, daysRemaining: 3 }
```

Wait, the spec says "Siege Intensity -10 for 3 days" and "Siege Intensity -5 for 3 days." The intensity ranges 1-6, so -10 effectively makes it 0 (clamped). This is the effective intensity used for damage calculation, not a permanent reduction.

##### M3. Search Abandoned Homes
```
basePlague = 0.20
adjustedPlague = basePlague + fuelRiskMod
remaining = 1.0 - adjustedPlague

chanceMatl = remaining × (0.50 / 0.80)
chanceMed = remaining × (0.30 / 0.80)

roll = RandomProvider.Range01()
if roll < adjustedPlague: sickness += 15
else if roll < adjustedPlague + chanceMed: medicine += 40
else: materials += 60
```

##### M4. Negotiate with Black Marketeers
```
baseScandal = 0.20
adjustedScandal = baseScandal + fuelRiskMod
remaining = 1.0 - adjustedScandal

chanceWater = remaining × (0.50 / 0.80)
chanceFood = remaining × (0.30 / 0.80)

roll = RandomProvider.Range01()
if roll < adjustedScandal: unrest += 20
else if roll < adjustedScandal + chanceFood: food += 80
else: water += 100
```

After resolution: `activeMission = null`, return 10 workers to available pool. Log outcome with adjusted odds.

### 2.13 Step 13 — Loss Conditions (`Step13_LossConditions.cs`)

Check in order:

1. **Keep Breach**: Keep integrity ≤ 0 → Game Over "Breach"
2. **Council Revolt**: `unrest > 85` → Game Over "Revolt"
3. **Total Collapse**: `consecutiveFoodWaterZeroDays >= 2` → Game Over "Collapse"

If any triggers: `isGameOver = true`, `gameOverReason = reason string`.

### 2.14 Post-Simulation

- Increment `currentDay`
- Increment `daysSinceLastLaw` (for cooldown tracking)
- Increment `daysSinceLastLawEnacted` (for "no law in 3 days" unrest)
- Clear `todayEmergencyOrder`
- If `currentDay > 40 AND !isGameOver`: `isVictory = true`

Apply all queued deaths using `PopulationManager`.

---

## Phase 2b — Population Manager (`Assets/Scripts/Core/PopulationManager.cs`)

### Death Application

**Default priority**: Sick → Elderly → Healthy Workers → Guards

1. Reduce `sick` by min(count, sick)
2. Remaining → reduce `elderly`
3. Remaining → reduce `healthyWorkers`
4. Remaining → reduce `guards`

**L6 priority (Healthy first, then Sick)**: Healthy Workers → Sick → Elderly → Guards

After any death, if `healthyWorkers` decreased, validate worker allocations: if total assigned > available, auto-unassign from lowest-priority slot (Fuel Scavenging first, then working upward).

### Force Population Inward

When a zone is lost/evacuated:

1. Take `displacedPop = zone.currentPopulation`
2. Set `zone.currentPopulation = 0`
3. Find next inner non-lost zone (by `order`)
4. Add `displacedPop` to that zone's `currentPopulation`
5. If that zone doesn't exist (all inner zones lost), continue to next — this should never happen unless Keep is lost (game over)

### Zone Population Initialization

At game start, fill outer-to-inner:
- Outer Farms: min(20, remaining) = 20
- Outer Residential: min(40, remaining) = 40
- Artisan Quarter: min(25, remaining) = 25
- Inner District: min(50, remaining) = 35 (only 35 left)
- Keep: 0

---

## Phase 3 — Player Actions & Game Loop

### 3.1 Game Manager — `Assets/Scripts/Core/GameManager.cs` (MonoBehaviour)

State machine with states:

```
enum GamePhase { Setup, PlayerTurn, Simulating, ShowReport, GameOver, Victory }
```

#### Setup Phase
1. Create `GameState` with default values
2. Pick random `PressureProfileId` via `RandomProvider`
3. Apply profile modifications (see below)
4. Randomize Outer Farms integrity (70–85) unless overridden by profile
5. Pick 2 unique early incidents, schedule on random days 3–6 (no two on same day)
6. Initialize zone populations (outer-to-inner)
7. Transition to `PlayerTurn`

#### Profile Application

| Profile | Modifications |
|---|---|
| P1 Disease Wave | `sickness += 10`; `medicine -= 10`; food consumption gets permanent ×0.98 multiplier (store in GameState as a profile modifier) |
| P2 Supply Spoilage | `food -= 60`; `unrest += 5`; `materials += 10` |
| P3 Sabotaged Wells | `wellsDamaged = true`; `morale += 10`; `unrest -= 10` |
| P4 Heavy Bombardment | `siegeIntensity = 2`; Outer Farms integrity = 65 (override random); `food += 40` |

#### Player Turn Phase
- Enable all UI interaction panels
- Player may: allocate workers, enact law, select emergency order, start mission, evacuate zone, repair wells
- On "End Day" button: validate allocations → transition to `Simulating`

#### Simulating Phase
- Create fresh `SimulationContext`
- Run Steps 1–13 via `DaySimulator`
- Build causality log
- Transition to `ShowReport` (or `GameOver`/`Victory`)

#### Show Report Phase
- Display `DailyReportPanel` with full causality breakdown
- Player clicks "Continue" → transition to `PlayerTurn`
- If Day > 40 and alive → `Victory`

### 3.2 Player Action: Enact Law

Validation:
- `daysSinceLastLaw >= 3` (cooldown met)
- Law not already enacted
- Unlock condition met (checked per LawId):

| Law | Unlock Condition |
|---|---|
| L1 | Always |
| L2 | Water deficit occurred today (water = 0 after step 4 on previous day or projected) OR `water < 100` |
| L3 | `currentDay >= 5` |
| L4 | `unrest > 40` |
| L5 | Any zone has `isLost = true` |
| L6 | `unrest > 60` |
| L7 | `morale < 40` |
| L8 | `food < 100` |
| L9 | `medicine < 20` |
| L10 | `unrest > 50` |
| L11 | Outer Farms `currentIntegrity < 40` AND Outer Farms not lost |
| L12 | `unrest > 75` |

On enact:
1. Add `LawId` to `enactedLaws`
2. Reset `daysSinceLastLaw = 0`
3. Reset `daysSinceLastLawEnacted = 0`
4. Apply immediate on-enact effects:

| Law | Immediate Effect |
|---|---|
| L1 | `morale -= 10` |
| L2 | `morale -= 5` |
| L3 | `morale -= 15` |
| L4 | Convert 10 Healthy to Guards: `healthyWorkers -= 10`; `guards += 10`; `morale -= 10`. Validate worker allocations |
| L5 | `unrest += 10` |
| L6 | `unrest -= 25`; `morale -= 20`; kill 5 (L6 priority: Healthy first) |
| L7 | `materials -= 10` (must have ≥10); `morale += 15`; `unrest += 5` |
| L8 | `food += 100`; `unrest += 20`; `morale -= 20` |
| L9 | (none) |
| L10 | (none) |
| L11 | Outer Farms becomes Lost immediately; apply natural on-loss shock (+15 Unrest, +10 Sickness, -10 Morale, Food production -40%); additional `unrest += 15`; move population inward |
| L12 | (none) |

### 3.3 Player Action: Select Emergency Order

- 1 per day, selected during PlayerTurn
- Costs paid immediately (before simulation)
- Must meet cost requirements (enough Food/Water/Materials)
- O4: deaths applied immediately
- O5: player picks a zone (UI selector, any non-lost zone)

### 3.4 Player Action: Start Mission

- Only if `activeMission == null`
- Only if `healthyWorkers >= 10` (after accounting for allocations? No — mission workers are separate from allocation. Available workers = healthyWorkers - 10 if mission active)
- On start: `activeMission = new ActiveMission(missionId, currentDay, 10)`
- Workers become unavailable for allocation this day
- If M2 Night Raid: Fuel -40 deducted at pre-simulation (Step 2.0)

### 3.5 Player Action: Evacuate Zone

Eligibility check (any one of):
- All zones with `order <= 2` (Farms + Residential) are lost
- Active perimeter `currentIntegrity < 40`
- `siegeIntensity >= 5`

Can only evacuate the **active perimeter** zone (outermost non-lost, non-Keep zone).

Cost:
- `materials -= 20` (must have ≥ 20)
- `sickness += 10`
- `unrest += 10`
- `morale -= 15` (this replaces the zone's natural morale loss)

Then apply zone's on-loss effects (Unrest, Sickness, production penalties) but use `onEvacMorale` instead of `onLossMorale`. Move population inward.

### 3.6 Player Action: Repair Wells

- Only available if `wellsDamaged = true`
- Cost: `materials -= 10` (must have ≥ 10)
- Effect: `wellsDamaged = false`
- Alternative: using O1 also repairs wells (handled in Step 2)

---

## Phase 4 — UI Implementation

All UI built with **uGUI 2.0** (Canvas-based). Use MCP tools where possible per the skill definition. Use `Canvas Scaler` with "Scale With Screen Size" (reference 1920×1080, match 0.5).

### 4.1 Main Canvas — `Assets/Prefabs/UI/MainCanvas.prefab`

- Canvas (Screen Space - Overlay)
- Canvas Scaler (Scale With Screen Size, 1920×1080, Match = 0.5)
- Vertical Layout Group for main structure

Layout (top to bottom):
```
┌─────────────────────────────────────────────┐
│ TOP BAR                                       │
├──────────┬──────────┬──────────┬─────────────┤
│ ZONE     │ WORKER   │ ACTIONS  │ WHY AM I    │
│ PANEL    │ ALLOC    │ PANEL    │ DYING?      │
│          │ PANEL    │ (Laws/   │ ---         │
│          │          │  Orders/ │ Event Log   │
│          │          │  Missions│             │
│          │          │  Evac)   │             │
├──────────┴──────────┴──────────┴─────────────┤
│ END DAY BUTTON                                │
└─────────────────────────────────────────────┘
```

The Daily Report and Game Over panels are overlays that appear on top.

### 4.2 Top Bar Panel — `Assets/Scripts/UI/Panels/TopBarPanel.cs`

Display:
- **Day counter**: "Day {X} / 40"
- **Resources** (horizontal row): Food, Water, Fuel, Medicine, Materials — each showing current value + projected delta (e.g., "Food: 280 (-40)")
- **Meters** (horizontal row): Morale bar (0–100), Unrest bar (0–100), Sickness bar (0–100)
- **Siege Intensity**: numeric display "Siege: {X}/6"
- **Population**: "Pop: {total} (H:{healthy} G:{guards} S:{sick} E:{elderly})"
- **Pressure Profile**: label showing active profile name

Color coding for meters:
- Morale: green > 50, yellow 30–50, red < 30
- Unrest: green < 40, yellow 40–60, red > 60
- Sickness: green < 30, yellow 30–60, red > 60

Projected deltas: calculated by running a preview simulation (or estimating from current allocation + known modifiers). At minimum, show simple burn rate: `-(consumption per day)` for resources and `+(production per day)`.

### 4.3 Zone Panel — `Assets/Scripts/UI/Panels/ZonePanel.cs`

Vertical list of 5 zones (ordered Outer Farms → Keep):

Each zone row:
- Zone name
- Status badge: "ACTIVE PERIMETER" (highlighted) / "Intact" / "LOST" (greyed out / red)
- Integrity bar: `{current}/{max}` with color (green > 60%, yellow 30–60%, red < 30%)
- Population: `{current}/{capacity}` (show effective capacity including L5 bonus)
- Overcrowding: show percentage if > 0%, in red
- Production bonuses: show active zone bonuses (e.g., "+50% Food" for Farms)

### 4.4 Zone Visualization Widget — `Assets/Scripts/UI/Widgets/ZoneRingWidget.cs`

A simple concentric rectangle/ring visual:
- 5 nested rectangles (Keep innermost, Farms outermost)
- Each colored by status: blue=active perimeter, green=intact, dark grey=lost
- Integrity shown as fill amount on the perimeter ring
- When a zone is lost: animate it fading to grey and the perimeter highlight moving inward (simple DOTween-style lerp or coroutine animation)

This is purely visual flavor; all real info is in the Zone Panel.

### 4.5 Worker Allocation Panel — `Assets/Scripts/UI/Panels/WorkerAllocationPanel.cs`

Header: "Workers Available: {available}/{totalHealthy}" (show mission lock if active)

7 job rows (one per allocatable slot):

| Row | Info Shown |
|---|---|
| Food Production | Assigned: {n}, [−5] [+5], Projected: +{food}/day |
| Water Drawing | Assigned: {n}, [−5] [+5], Projected: +{water}/day |
| Materials Crafting | Assigned: {n}, [−5] [+5], Projected: +{materials}/day |
| Repairs | Assigned: {n}, [−5] [+5], Projected: +{integrity}/day, Cost: {materials}/day |
| Sanitation | Assigned: {n}, [−5] [+5], Projected: -{sickness}/day |
| Clinic Staff | Assigned: {n}, [−5] [+5], Projected: -{sickness}/day, Cost: {medicine}/day |
| Fuel Scavenging | Assigned: {n}, [−5] [+5], Projected: +{fuel}/day |

Guard Duty row (not allocatable): "Guards on Duty: {guards} → Siege Dmg -{X}, Unrest -{X}/day"

Idle Workers warning: if idle % >10 show yellow, >20 show red with text.

Stepper buttons disabled when:
- [+5] disabled if no available workers left
- [−5] disabled if slot is at 0
- Show warning icon if slot is ineffective (e.g., Repairs with 0 Materials, Clinic with 0 Medicine)

Projected values should account for active modifiers (zone bonuses, laws, etc.) so the player sees realistic numbers.

### 4.6 Laws Panel — `Assets/Scripts/UI/Panels/LawsPanel.cs`

Header: "Laws — Cooldown: {days remaining}" (or "Ready" if ≥ 3 days since last)

Scrollable list of 12 laws. Each row:
- Name
- Unlock status: "Locked: {requirement}" (greyed out) or "Available" (highlighted)
- If already enacted: "ENACTED" badge (no interaction)
- On-enact effects: explicit text (e.g., "Morale -10")
- Ongoing effects: explicit text (e.g., "Food consumption -25%, Unrest +5/day")
- [Enact] button (enabled only if: unlocked, not enacted, cooldown met)

### 4.7 Emergency Orders Panel — `Assets/Scripts/UI/Panels/EmergencyOrdersPanel.cs`

Header: "Emergency Order — 1 per day" (show "USED" if already selected today)

6 order cards:
- Name
- Cost: explicit (e.g., "Food -30, Water -20")
- Effect: explicit (e.g., "Repair output +50% today")
- [Issue] button (disabled if already used today or insufficient resources)
- O5: show zone dropdown/selector when selected

### 4.8 Missions Panel — `Assets/Scripts/UI/Panels/MissionsPanel.cs`

If mission active: show "Mission in progress: {name} — resolves end of day"

If no mission:
- Header: "Missions — Requires 10 Workers"
- Enable/disable based on healthyWorkers ≥ 10

4 mission cards:
- Name
- Outcomes with **adjusted probabilities** (show fuel modifier impact):
  e.g., "M1 Forage: +120 Food (52.9%) | +80 Food (17.1%) | Ambushed: 5 deaths (30.0%)"
- [Launch] button
- M2: show "Fuel cost: 40" and warning if insufficient

### 4.9 Evacuation Panel — `Assets/Scripts/UI/Panels/EvacuationPanel.cs`

Only visible when evacuation is eligible.

Show:
- "Evacuate {zone name}"
- Cost breakdown: "20 Materials, +10 Sickness, +10 Unrest, -15 Morale"
- Zone-specific penalties preview
- [Evacuate] button (disabled if < 20 Materials)

### 4.10 Wells Repair Button

If `wellsDamaged`: show "Repair Wells — Cost: 10 Materials" button (in Zone Panel or as a separate widget). Disabled if < 10 Materials.

### 4.11 Daily Report Panel — `Assets/Scripts/UI/Panels/DailyReportPanel.cs`

Full-screen overlay after simulation. Scrollable.

Sections:
1. **Day Summary**: "Day {X} complete"
2. **Resource Changes**: Table with columns [Resource | Start | Produced | Consumed | End | Net]
   - Each row expands to show breakdown (e.g., "Food consumed: 120 base + 15 L4 + 8 overcrowding × 0.75 L1 = 101")
3. **Morale Breakdown**: List of all contributions ("+10 Keep bonus", "-5 Food deficit", etc.) → Net change
4. **Unrest Breakdown**: Same format
5. **Sickness Breakdown**: Same format
6. **Siege Damage Breakdown**: "Base (3+{intensity}) × {perimFactor} - {guardReduction} × {L11mult} = {final}"
7. **Events Triggered**: List with cause and effect
8. **Mission Result**: If resolved, show outcome
9. **Deaths**: Count by cause
10. **Warnings**: Early incident warnings for next day

Every number must cite its source. No hidden changes.

[Continue] button → back to PlayerTurn.

### 4.12 "Why Am I Dying?" Panel — `Assets/Scripts/UI/Panels/WhyAmIDyingPanel.cs`

Expandable/collapsible panel (toggle button). Shows **top 3 current pressures**, calculated by:

Priority scoring system (show the top 3 by urgency):

1. **Resource depletion**: For each resource, calculate days until 0: `resource / dailyNetBurn`. If ≤ 3 days: CRITICAL. If ≤ 7: WARNING.
   - Display: "Food will run out in {X} days (consuming {Y}/day, producing {Z}/day)"

2. **Perimeter breach**: `daysUntilBreach = activePerimeter.integrity / dailySiegeDamage`. If ≤ 5: CRITICAL.
   - Display: "Perimeter ({zone}) breach in ~{X} days (taking {Y} damage/day)"

3. **Unrest threshold**: If unrest > 60: "Unrest at {X}/85 — gaining {Y}/day from: {top causes}"

4. **Sickness spiral**: If sickness > 50: "Sickness at {X}, growing {Y}/day — {causes}"

5. **Morale collapse**: If morale < 35: "Morale at {X} — dropping {Y}/day due to {causes}"

6. **Overcrowding**: If any zone > 120% capacity: "Overcrowding in {zone}: {X}% — causing +{U} Unrest, +{S} Sickness/day"

Each entry shows the **exact numeric causality** from the last day's log.

### 4.13 Game Over Panel — `Assets/Scripts/UI/Panels/GameOverPanel.cs`

- "GAME OVER — Day {X}"
- Cause: "Breach" / "Revolt" / "Collapse"
- Final stats: Population, Resources, Meters
- Telemetry summary (see Phase 5)
- [Restart] button → fresh Setup

### 4.14 Victory Panel — `Assets/Scripts/UI/Panels/VictoryPanel.cs`

- "SURVIVED — 40 Days"
- Final stats
- Telemetry summary
- [Restart] button

### 4.15 Event Log Panel - `Assets/Scripts/UI/Panels/EventLogPanel.cs`

* Shows all the actions/events that has happened in the game since the start.
* Separate each day by `=========== DAY {NUMBER} ==============`
* Add yesterday's entry to this panel after daily report is closed.
* This panel should be scrollable.
* This panel lives in the same space as Why Am I Dying panel. Why Am I Dying is placed on top and Event Log Panel is placed at the bottom in a vertical layout group.

---

## Phase 5 — Telemetry & Debug

### 5.1 Telemetry Logger — `Assets/Scripts/Telemetry/TelemetryLogger.cs`

Log per run (output to Unity console AND a `telemetry.json` file in `Application.persistentDataPath`):

| Field | Captured When |
|---|---|
| `pressureProfile` | Setup |
| `randomSeed` | Setup |
| `earlyIncidents` | Setup (which 2, which days) |
| `dayOfFirstDeficit` | First time any resource hits 0 |
| `whichResourceFirst` | Which resource hit 0 first |
| `dayOfFirstZoneLost` | First zone loss |
| `whichZoneFirst` | Which zone was lost first |
| `firstLawEnacted` | LawId + day |
| `causeOfLoss` | "Breach" / "Revolt" / "Collapse" / "Survived" |
| `dayOfLoss` | Day number (or 40 for survival) |
| `finalPopulation` | Total pop at end |
| `averageUnrest` | Running average across all days |
| `dayByDaySnapshot` | Array of per-day snapshots: resources, meters, population, events |

### 5.2 Debug Console Commands (Optional but recommended)

Add keyboard shortcuts (Editor only):
- `F1`: Toggle "Why Am I Dying?" panel
- `F2`: Log full game state to console
- `F5`: Force advance day without player input (auto-sim with current allocation)

---

## Phase 6 — Tuning Validation

After implementation, verify these **guardrails** by running test scenarios:

| Scenario | Expected Outcome | How to Test |
|---|---|---|
| No food allocation | Food hits 0 by Day 6–8 | Start game, assign 0 workers to Food, auto-advance |
| No repairs | First zone loss by Day 8–12 | Assign 0 to Repairs, auto-advance |
| Enact L3+L6+L12 early | Revolt by Day 10–14 | Enact harshest laws ASAP |
| All workers → Guards/Guard focus | Economic collapse by Day 12–18 | Maximize guard-related choices |
| Ignore fuel entirely | Fuel penalties start Day 3–4 | Assign 0 to Fuel Scavenging |

If any guardrail fails, adjust **numeric values only** (not system logic).

### Verification Commands

```
# Run in Unity Play Mode:
# 1. Start game, observe Day 1 state matches Initial State spec
# 2. End Day with no allocations, verify simulation order
# 3. Check Daily Report shows all 13 steps with causality
# 4. Verify resource deltas match hand-calculated values
# 5. Run 5+ full playthroughs to test all pressure profiles
# 6. Verify each law's unlock condition triggers correctly
# 7. Verify each event triggers at correct thresholds
# 8. Verify mission odds display correctly with fuel modifiers
# 9. Verify zone loss cascades population correctly
# 10. Verify game over triggers for all 3 conditions
```

### Unit Tests (in a new test assembly `Assets/Tests/`)

Create an assembly definition `SiegeTests.asmdef` referencing the game scripts assembly.

Key test cases:
- `ProductionCalculation_WithAllModifiers_ReturnsCorrectMultiplicativeResult`
- `FoodConsumption_WithOvercrowding_CalculatesPerZone`
- `FuelConsumption_WithOvercrowdingZones_AppliesGlobalModifier`
- `PopulationForceInward_GoesToNextInnerZone`
- `LawUnlock_L5_RequiresZoneLost`
- `EventTrigger_HungerRiot_Requires2ConsecutiveDeficitAndUnrest`
- `MissionOdds_WithFuelModifier_AdjustsCorrectly`
- `SiegeDamage_WithGuardsAndL11_ReducesCorrectly`
- `Evacuation_CostAndPopulationMovement`
- `MartialLaw_CapsUnrestAndMorale`
- `MedicalTriage_KillsSickDaily`
- `SimulationOrder_ExecutesStepsInCorrectSequence`

---

## Implementation Order

| Order | Phase | Estimated Scope |
|---|---|---|
| 1 | Phase 1: Data Model + State | Enums, ScriptableObjects, GameState, runtime classes |
| 2 | Phase 2: Simulation Engine | 13 step scripts + PopulationManager + ModifierCalculator |
| 3 | Phase 3: Game Loop + Player Actions | GameManager state machine, law/order/mission/evacuation logic |
| 4 | Phase 4: UI (Top Bar + Zones + Workers) | Core gameplay panels |
| 5 | Phase 4: UI (Laws + Orders + Missions) | Action panels |
| 6 | Phase 4: UI (Daily Report + Why Am I Dying) | Information panels |
| 7 | Phase 4: UI (Game Over + Victory + Zone Viz) | End state + polish |
| 8 | Phase 5: Telemetry | Logging system |
| 9 | Phase 6: Tuning Validation | Automated test runs + unit tests |

---

## Decisions

- **Modifier stacking**: Multiplicative chaining for all production/consumption multipliers; flat additions for meter deltas. Per user answer Q3.
- **Death priority**: Default Sick→Elderly→Healthy→Guards; L6 exception Healthy→Sick. Per user answer Q5.
- **Fuel consumption**: Global formula with coarse overcrowding breakpoints (≥20% per zone counts as +0.10). Per user answer Q6.
- **O5 Quarantine**: Always -50% all production globally regardless of zone selection. Per user answer Q2.
- **L11 costs**: Zone natural-loss shock + L11's +15 Unrest + population movement. No evacuation base cost. Per user answer Q4.
- **Population routing**: Forced exclusively to next inner non-lost zone, NOT distributed. Per user answer Q1.
- **Law effects hardcoded**: ScriptableObjects hold display text only; gameplay logic is per-LawId in C# for clarity and to avoid overly complex data structures for 12 unique laws.
- **Wells damage**: Only from P3 (Sabotaged Wells). No other source in V1. Per answer Q6 (wells).
- **No save/load**: Single session runs. Per answer Q8.
- **UI framework**: uGUI 2.0 with Canvas Scaler. Per skill definition and answer Q9.
- **No tilemap**: Zone visualization is a simple UI widget. Per answer Q7.
