### Purpose of Fuel

Fuel represents **heating + cooking + sanitation fires**. It is not a direct “survival bar” like Food/Water. Instead, Fuel is a **spiral amplifier** that interacts with Morale, Unrest, Sickness, and Food efficiency. Fuel must create hard tradeoffs but must not force a deterministic Day 1 priority.

---

# 1) Fuel as a Real System (Not a Ghost Resource)

Fuel must have:

* Explicit **daily consumption**
* A **way to produce more**
* Clear **penalties when empty**
* Interaction with **missions** as a risk modifier (not a hard gate)

Do not implement Fuel as a flat countdown with no player agency.

---

# 2) Daily Fuel Consumption

### Starting Fuel

* Fuel starts at **240**.

### Base daily consumption

* BaseFuelConsumption = **120/day** at baseline population 120.

### Scale with population

FuelConsumption must scale linearly with total population:

* FuelConsumption = 120 × (TotalPopulation / 120) × OvercrowdingFuelModifier

### Overcrowding modifier

If any zone is overcrowded, increase FuelConsumption:

* OvercrowdingFuelModifier = 1 + 0.10 × (TotalOvercrowdingPercent / 20%)

Where:

* TotalOvercrowdingPercent can be computed as sum over zones of max(0, (Pop - Capacity)/Capacity) expressed as a percent.
* Keep it simple: if you prefer, approximate with:

  * +10% fuel consumption for each zone above capacity by ≥20%.

**Important:** Fuel consumption is applied during the daily simulation step **after production, before deficit penalties** (consistent with the global order: production → consumption → deficit penalties).

---

# 3) Fuel Production (New Job Slot)

Add a job slot:

### Job Slot: Fuel Scavenging

* Allocation in increments of 5 workers (same as others)

Output:

* Base: **+15 Fuel per 5 workers per day**

Modifiers:

* If Outer Farms is Lost: **-50% Fuel Scavenging output**
* If Siege Intensity ≥ 4: **20% chance per day** the scavenging job causes **2 deaths** (representing risk outside the walls).

  * This risk applies only if at least one Fuel Scavenging slot is staffed.

Fuel Scavenging competes with Food, Repairs, etc. It should be meaningful but not mandatory Day 1.

---

# 4) Fuel Shortage / Zero Fuel Effects (Spiral Amplifier)

Fuel can go to 0. Do not clamp it above 0. If Fuel is 0 (or ≤ 0) at the time penalties are applied:

Apply these daily penalties:

* **+10 Sickness**
* **-10 Morale**
* **+5 Unrest**
* **-15% Food Production** (represents reduced cooking efficiency / spoilage / inability to process)

These penalties should be logged in the causality breakdown as “No Fuel: Cold & Kitchen Failure”.

Fuel alone must not trigger an instant game-over condition. It should push the city into collapse over several days.

---

# 5) Missions: Fuel Influences Risk (Not Access)

Fuel must **not** hard-gate missions in general. Missions are always selectable even at 0 fuel. Fuel modifies mission outcome odds by shifting probability toward the bad outcome.

### Fuel Risk Bands (global modifier)

Compute a “Fuel Risk Tier” based on Fuel at the moment the mission is launched:

* Fuel ≥ 100: no change
* 50 ≤ Fuel < 100: +5% bad outcome chance
* 1 ≤ Fuel < 50: +15% bad outcome chance
* Fuel ≤ 0: +25% bad outcome chance

Implementation detail:

* Increase the probability of the mission’s worst outcome by the specified amount.
* Reduce the other outcomes proportionally (preserve 100% total).
* Display the modified odds to the player before confirming the mission.

### Exception: Night Raid consumes Fuel

Only the mission “Night Raid on Siege Camp” explicitly consumes Fuel:

* Night Raid costs **40 Fuel** on launch.
* If Fuel < 40:

  * Mission still allowed.
  * Apply an additional **+20%** bad outcome chance (on top of the tier modifier).

This creates a deliberate “burn heat for a chance to slow siege” tradeoff.

No other missions consume Fuel directly.

---

# 6) UI & Causality Logging Requirements

Fuel must appear in:

* Stockpiles
* Projected deltas
* Daily report breakdown

Daily report must show:

* Fuel gained (by Fuel Scavenging)
* Fuel spent (daily consumption)
* Any Fuel-related penalties (No Fuel penalties, Night Raid cost)
* Any deaths caused by Fuel Scavenging risk

All Fuel effects must be visible and attributable. No hidden modifiers.

---

# 7) Tuning Guardrails (Must Hold)

After implementing Fuel, validate these behaviors:

* If player ignores Fuel Scavenging entirely:

  * Fuel reaches ~0 by Day 3–4
  * Fuel penalties then begin compounding collapse; Fuel alone should not instantly end the run but should contribute to failure within ~5–7 additional days if unmanaged.

* If player over-focuses on Fuel Scavenging early:

  * Other pressures (repairs/food/water) must worsen enough that the player is not “safe”.

If telemetry shows players always assign Fuel Scavenging on Day 1, Fuel is too punishing; reduce base consumption slightly or soften penalties.
If telemetry shows players ignore Fuel forever, Fuel is too weak; strengthen penalties or reduce scavenging output.

---

# 8) Data-Driven Implementation

Fuel-related constants must be editable via data (ScriptableObject/JSON), including:

* BaseFuelConsumption
* FuelScavengingOutput
* NoFuel penalties values
* Fuel risk tier thresholds and percentage shifts
* Night Raid fuel cost and extra risk

Do not hardcode these into simulation logic.
