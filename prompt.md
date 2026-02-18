### Mission

Implement a complete playable prototype of a siege survival city manager where the player must endure **40 days**. 
The enemy cannot be defeated; the city cannot be stabilized. The core experience is **managing decline under dual pressure**:

* **External siege pressure** causes **zone contraction** (outer districts fall/evacuate, perimeter shrinks).
* **Internal collapse** (morale/unrest/sickness) escalates, worsened by **overcrowding** when zones contract.

Most runs should fail before **Day 25**. Survival to Day 40 should be rare and feel costly.

### Non-negotiables (Do not “fix” these)

* The player must have deficits and tension on Day 1.
* It must be impossible to reach a stable equilibrium before Day 15.
* No dominant strategy.
* No auto-correction: no auto worker redistribution, no hidden safety nets, no “free” recovery.
* No building construction, no combat, no pathfinding, no procedural generation.
* Laws are permanent; zones cannot be reclaimed.
* Randomness is allowed only where explicitly defined; no surprise deaths or opaque RNG.

If a skilled player can stabilize meters early, the implementation is wrong.

### Development notes
* Make variables easy to tune
* Write a deterministic "Core Engine"
    * Having the "Core Engine" will enable us to automatically test
    * It will also enable us to play the game through CLI
    * Playing the game through CLI is OUT OF SCOPE for this implementation
* Make sure to use scriptable object to make the game easy to tune/balance.
* Avoid using serialized fields for intra-prefab or intra-scene dependencies. If an object or component needs to be found, start the name of that gameobject with `#` and find that object or component in runtime.
* Make sure the requirements and consequences of each action is clearly visible in the UI, unless stated otherwise for the feature.

### EMOTIONAL TARGET

The player must feel:

* Constant pressure
* Moral compromise
* Shrinking space
* No clean solution
* Survival equals sacrifice

Success must feel like endurance, not triumph.

### Ask Questions

If something is not clear, or something feels contradictory, ASK QUESTIONS.

Do not move forward until you are confident that you understand both INTENT and REQUIREMENTS of every feature.

---


# GLOBAL STARTING STATE (Day 1)

* Population: **120**

  * 85 Healthy Workers
  * 10 Guards
  * 15 Sick
  * 10 Elderly (consume, don’t work)
* Food: 320 (≈ 2.5 days at normal rations)
* Water: 360 (3 days)
* Fuel: 240 (2 days of heating + kitchens)
* Medicine: 40
* Materials: 120
* Morale: 55/100
* Unrest: 25/100
* Sickness: 20/100
* Siege Intensity: 1 (scales to 6 by Day 40)

City is already unstable. If the player does nothing smart, collapse begins by Day 6–8.

Food and Water must be critical within first 6–8 days without intervention.

---

# 1) Core Loop & Simulation Order (MUST MATCH)

Implement daily cycle with these phases and exact resolution order:

1. Apply **Law** passive modifiers
2. Apply **Emergency Order** effects (1-day)
3. Calculate **production**
4. Apply **consumption**
5. Apply **deficit penalties**
6. Apply **overcrowding penalties**
7. Apply **sickness progression**
8. Apply **unrest progression**
9. Apply **siege damage**
10. Apply **repairs**
11. Resolve **triggered events**
12. Check **loss conditions**

Do not reorder steps.

---

# 2) Data-Driven Requirement

All laws, orders, missions, events, zones, job slots must be defined as data (ScriptableObjects or JSON). Avoid inheritance hierarchies; prefer composition + simple structs.

---

# 3) City Model

### Zones (5, ordered)

1. Outer Farms
2. Outer Residential
3. Artisan Quarter
4. Inner District
5. Keep

Each zone has:

* Integrity (0–100)
* Capacity
* Population currently housed
* Production modifiers / on-loss effects
* “Active perimeter” = the outermost non-lost zone

**Zone Integrity is the defensive line.** There is no separate wall HP. Siege always targets the **active perimeter zone**.

### Starting zone values

* Outer Farms: Integrity 80 (but see pressure profiles), Capacity 20, +50% Food production while active/intact
* Outer Residential: Integrity 70, Capacity 40
* Artisan Quarter: Integrity 75, Capacity 25, +40% Materials output while intact
* Inner District: Integrity 90, Capacity 50, -10% unrest growth while intact
* Keep: Integrity 100, Capacity 60, +10 morale while intact; if lost → immediate breach loss

### Overcrowding rule (stacking)

For every 10% over capacity in a zone:

* +2 Unrest/day
* +2 Sickness/day
* +5% Food consumption

Apply after deficits, before sickness/unrest progression.

### Zone loss

If a zone reaches Integrity ≤ 0 OR is voluntarily evacuated:

* It becomes Lost permanently
* All its population is forced inward to next surviving zone(s)
* Apply loss shock (see below)
* Active perimeter moves inward
* Production modifiers update

**Loss shock (natural fall)**
On natural fall (Integrity ≤ 0):

* +15 Unrest
* +10 Sickness
* -10 Morale

**Controlled evacuation shock**
On voluntary evacuation:

* +10 Unrest
* +10 Sickness
* -15 Morale
  (plus zone-specific penalties below)

---

# 4) Evacuation Rules (Player Agency)

Evacuation exists and is irreversible, intended as “trade land for time.”

### Eligibility (no free out-of-order contraction)

A zone can be evacuated only if:

* All outer zones are already Lost
  OR
* That zone Integrity < 40
  OR
* Siege Intensity ≥ 5

Keep cannot be evacuated.

### Cost

Base:

* 20 Materials
* Apply controlled evacuation shock (above)

### Zone-specific additional penalties

* Evacuate Outer Farms: Food production -50% permanently; +5 Food/day consumption (“disruption”)
* Evacuate Outer Residential: +20 Unrest, +15 Sickness, +5 deaths (panic crush)
* Evacuate Artisan Quarter: Materials production -60% permanently; Repairs cost +25% permanently
* Evacuate Inner District: +30 Unrest, -20 Morale; disable any morale bonus building effects tied to inner stability

### Benefit (must be real)

Implement perimeter scaling:

Daily Siege Damage = (3 + Siege Intensity) × PerimeterFactor
PerimeterFactor based on active perimeter:

* Outer Farms: 1.0
* Outer Residential: 0.9
* Artisan Quarter: 0.8
* Inner District: 0.7
* Keep: 0.6

This is the primary mechanical benefit of early evacuation.

---

# 5) Population, Resources, Meters

### Starting population (120)

* 85 Healthy Workers
* 10 Guards
* 15 Sick
* 10 Elderly (consume only)

### Starting meters

* Morale: 55/100
* Unrest: 25/100
* Sickness: 20/100

### Starting resources

* Food 320
* Water 360
* Fuel 240
* Medicine 40
* Materials 120

---

# 6) Job Slots (7) — Allocation in increments of 5

Implement allocation UI with 5-worker stepper per job.

If the number of workers is not divisible by 5, the remainder will be idle (consume, get sick, but don't work)

1. Food Production

* Base: +10 Food per 5 workers
* Modifiers:

  * -40% if Outer Farms lost
  * -20% if Morale < 40
  * -30% if Unrest > 60
  * +50% while Outer Farms intact (zone modifier)

2. Water Drawing

* Base: +12 Water per 5 workers
* If Wells “damaged” (from profile/event): -50%

3. Materials Crafting

* Base: +8 Materials per 5 workers
* -50% if Artisan Quarter lost
* +40% while Artisan Quarter intact (zone modifier)

4. Repairs (active perimeter only)

* Base: +8 Integrity/day per 5 workers
* Cost: 4 Materials per 5 workers

5. Sanitation

* Reduce daily sickness growth by 5 per 5 workers (cannot reduce below 0 growth from this source)

6. Guard Duty
   Each 5 guards:

* Reduce daily siege damage by 1 (after perimeter scaling, before applying to integrity)
* Reduce Unrest by 3/day
  If Unrest > 70 and total Guards < 15 → revolt risk increases (handled via event chance or unrest progression multiplier)

7. Clinic Staff
   Each 5 workers:

* Spend 5 Medicine
* Reduce Sickness by 8

# Buildings by Zone

Each zone contains specific buildings. Make sure this is adjustable. These can change based on starting profile and map.

---

## Outer Farms

1. Fields (Food Job)
2. Wells (Water Job)
3. Barn Stores (Food spoilage mitigation)

---

## Outer Residential

1. Housing Blocks (Capacity)
2. Communal Kitchen (Food efficiency)
3. Bathhouse (Sickness reduction)

---

## Artisan Quarter

1. Workshop (Materials)
2. Armory (Guard effectiveness)
3. Clinic (Medicine → Sickness reduction)

---

## Inner District

1. Granary (Food storage efficiency)
2. Council Hall (Laws)
3. Gallows (unlocked by harsh laws)

---

## Keep

1. Wall Ramparts (Repair Job)
2. Watchtower (Siege detection)
3. Sanctuary (Morale recovery if not overcrowded)

---

---

# 7) Laws (12) — Permanent, Enact 1 per 3 days

Implement as data-driven, with clear UI effects and prerequisites. Enact limit: 1 law every 3 days. Laws are irreversible.

Players are NOT forced to enact a law.

Laws list (effects must match):

1. Strict Rations: -25% Food consumption; -10 Morale; +5 Unrest/day. Available Day 1.
2. Diluted Water: -20% Water consumption; +5 Sickness/day; -5 Morale. Requires Water deficit.
3. Extended Shifts: +25% all production; +8 Sickness/day; -15 Morale. Day 5+.
4. Mandatory Guard Service: convert 10 workers to Guards permanently; -15 Food/day (lost labor); -10 Morale. Requires Unrest > 40.
5. Emergency Shelters: +30 Capacity in Inner District; +10 Sickness/day; +10 Unrest. Requires first zone loss.
6. Public Executions: -25 Unrest instantly; -20 Morale; 5 random deaths. Requires Unrest > 60.
7. Faith Processions: +15 Morale; -10 Materials; +5 Unrest. Requires Morale < 40.
8. Food Confiscation: +100 Food instantly; +20 Unrest; -20 Morale. Requires Food < 100.
9. Medical Triage: -50% Medicine usage; +5 deaths/day among Sick. Requires Medicine < 20.
10. Curfew: -10 Unrest/day; -20% production. Requires Unrest > 50.
11. Abandon the Outer Ring: immediately lose Outer Farms; reduce daily siege damage by 20% (stack with perimeter factor); +15 Unrest. Requires Outer Farms Integrity < 40.
12. Martial Law: Unrest cannot exceed 60; Morale capped at 40. Requires Unrest > 75.

All ongoing daily modifiers must be included in causality logs.

---

# 8) Emergency Orders (1 per day, 1-day effects)

Implement:

1. Divert Supplies to Repairs: +50% repair output today; -30 Food; -20 Water
2. Soup Kitchens: -15 Unrest today; -40 Food
3. Emergency Water Ration: -50% Water consumption today; +10 Sickness
4. Crackdown Patrols: -20 Unrest today; 2 deaths; -10 Morale
5. Quarantine District: -10 Sickness spread today; -50% production in selected zone today
6. Inspire the People: +15 Morale today; -15 Materials

Orders cannot stack; only one per day.

---

# 9) Missions (1 at a time; costs 10 workers for 1 day)

Show odds explicitly to the player. Odds are modified only where specified.

1. Forage Beyond Walls
   Outcomes:

* +120 Food (60%)
* +80 Food (25%)
* Ambushed: 5 deaths (15%)
  If Siege Intensity ≥ 4 → Ambushed chance doubles (reduce other outcomes proportionally).

2. Night Raid on Siege Camp

* Reduce Siege Intensity by 10 for 3 days (40%)
* Reduce Siege Intensity by 5 for 3 days (40%)
* Captured: 8 deaths + +15 Unrest (20%)

3. Search Abandoned Homes

* +60 Materials (50%)
* +40 Medicine (30%)
* Plague exposure: +15 Sickness (20%)

4. Negotiate with Black Marketeers

* +100 Water (50%)
* +80 Food (30%)
* Scandal: +20 Unrest (20%)

No additional missions.

---

# 10) Events (Trigger Rules)

Events are threshold-driven. Randomness allowed only where defined.

Implement at least these events (exact triggers/effects):

1. Hunger Riot
   Trigger: Food deficit for 2 consecutive days AND Unrest > 50
   Effect: -80 Food; 5 deaths; +15 Unrest

2. Fever Outbreak
   Trigger: Sickness > 60
   Effect: 10 deaths; +10 Unrest

3. Desertion Wave
   Trigger: Morale < 30
   Effect: -10 Workers (remove from Healthy pool)

4. Wall Breach Attempt
   Trigger: Active perimeter zone Integrity < 30
   Effect: Immediate -15 Integrity unless Guards assigned ≥ 15 that day (then negate)

5. Fire in Artisan Quarter
   Trigger: Siege Intensity ≥ 4 AND random 10% each day
   Effect: -50 Materials; -10 Integrity to Artisan Quarter (if already lost, ignore)

6. Council Revolt
   Trigger: Unrest > 85
   Effect: Immediate Game Over

7. Total Collapse
   Trigger: Food = 0 AND Water = 0 for 2 consecutive days
   Effect: Immediate Game Over

Event resolution occurs at step 11 of daily order.

---

# 11) Siege System

* Siege Intensity starts at 1 (may be adjusted by profile)
* Intensity increases by +1 every 6 days
* +1 if Night Raid mission fails (captured outcome)
* Caps at 6

Daily siege damage:

* Compute BaseDamage = (3 + Intensity) × PerimeterFactor
* Reduce by Guards: -1 per 5 guards (applied after scaling)
* Apply remainder to active perimeter zone Integrity

Repairs happen after damage.

---

# 12) Non-Deterministic Early Game (Days 1–10)

Avoid deterministic “always fix food first” openings using controlled run-to-run variation.

Implement **Pressure Profiles**: at game start randomly select 1 of 4 profiles and apply its modifiers. This is the only start-of-run RNG besides small integrity variance and seeded early events.

Profiles:

A) Disease Wave

* +10 starting Sickness
* -10 starting Medicine
* -2% Food consumption modifier (slight relief)

B) Supply Spoilage

* -60 starting Food
* +5 starting Unrest
* +10 starting Materials

C) Sabotaged Wells

* Wells start Damaged (Water output -50%) until repaired via Emergency Order “Divert Supplies to Repairs” or 10 Materials one-time fix
* +10 starting Morale
* -10 starting Unrest

D) Heavy Bombardment

* Siege Intensity starts at 2
* Outer Farms Integrity starts at 65
* +40 starting Food

Also apply:

* Outer Farms Integrity random within 70–85 unless overridden by profile

Also pre-seed exactly 2 early events chosen randomly from a pool (scheduled to occur once each between Days 3–6):

* Minor Fire: -20 Materials
* Fever Cluster: +8 Sickness
* Food Theft: -40 Food, +5 Unrest
* Guard Desertion: -5 Guards, +5 Unrest

These early events must be telegraphed the day before (warning icon).

---

# 13) Loss Conditions (only these)

1. Keep Integrity ≤ 0 → Breach (Game Over)
2. Unrest > 85 → Revolt (Game Over)
3. Food and Water both 0 for 2 consecutive days → Total Collapse (Game Over)

No other hidden fail states.

---

# 14) UI Requirements (Functional, Not Pretty)

Must include:

* Zone view: show each zone’s Integrity, Capacity, Population, Lost/Active status
* Worker allocation panel for 7 job slots (5-worker increments)
* Laws panel: show effects + prerequisites + cooldown (1 per 3 days)
* Emergency Orders panel: 1/day
* Missions panel: 1 active; show odds
* Daily Report panel after each day:

  * Yesterday actual deltas
  * Next-day projected deltas based on current allocations
  * A causality breakdown for each meter/resource

### Mandatory Causality Logging

For every change to:

* Food, Water, Fuel, Medicine, Materials
* Morale, Unrest, Sickness
* Zone Integrity

Record:

* Source label
* Signed amount
* Aggregate

Display these in tooltips or an expandable breakdown list.

No opaque changes.

---

# 15) Tuning Guardrails (Must Hold)

After implementation, confirm these are true:

* If player never reduces food consumption or boosts food production → Food hits 0 by Day 6–8
* If player enacts Strict Rations but ignores Unrest → Unrest reaches dangerous range (≥60) by Day 10–14
* If player assigns 0 Repairs → first zone is lost by Day 8–12
* If player over-allocates Guards early → economic collapse by Day 12–18

If these do not hold, tune numeric constants ONLY (not mechanics) until they do.

---

# 16) Telemetry (Required)

Log per run (to console and to a simple file):

* Selected Pressure Profile
* Cause of loss
* Day of loss
* Day of first deficit (food/water)
* Day of first zone lost
* First law enacted (name + day)
* Total deaths, total desertions
* Unrest/Morale/Sickness at end

---

# Deliverable

A playable build where a player can:

* Allocate workers
* Choose an order and mission daily
* Enact laws every 3 days
* See daily reports and breakdowns
* Experience zone contraction and spirals
* Lose in varied ways
* Rarely survive to Day 40, with significant cost
