### Design Intent

Morale must:

* Feel fragile.
* React to visible hardship.
* Recover only through intentional action.
* Never drift upward passively.
* Never decay arbitrarily without visible cause.

Morale represents psychological resilience under siege.
It should not behave like a hidden countdown timer.

---

# 1) No Flat Automatic Daily Decay

Do **NOT** implement:

* A flat -1/day morale decay.
* A constant automatic morale recovery.
* Any unconditional base drift.

Morale should change only when there is a systemic reason.

---

# 2) Contextual Morale Drift (Conditional Only)

Morale may change daily based on conditions.

Implement the following conditional drift rules during the **morale progression step** (after deficits, before unrest progression if separated; otherwise during unrest/morale phase).

### Hardship Decay

Apply the following:

* If Food deficit occurred today → -5 Morale
* If Water deficit occurred today → -5 Morale
* If Fuel ≤ 0 → -10 Morale (already specified in Fuel system)
* If Sickness > 60 → -3 Morale
* If Overcrowding exists in any zone → -2 Morale
* If a zone was lost today → -10 Morale (natural fall)
* If voluntary evacuation today → -15 Morale (as specified)

These are additive.

---

### Stability Recovery (Limited)

Morale may recover slightly if conditions are stable.

If ALL of the following are true:

* No Food deficit today
* No Water deficit today
* Fuel > 0
* No overcrowding in any zone
* Sickness < 30
* Unrest < 40

Then:
+2 Morale for the day

This represents psychological stabilization.

This recovery must be rare in early and mid game.

---

# 3) Keep Passive Bonus

While Keep is intact:

* +10 Morale static modifier (already defined)

If Keep becomes active perimeter (all outer zones lost):

* Remove any additional morale bonuses tied to outer districts (if applicable).

---

# 4) Laws and Orders Override Base Logic

Laws and emergency orders modify Morale directly and must stack additively with contextual drift.

Examples:

* Strict Rations: -10 Morale on enact
* Public Executions: -20 Morale instantly
* Inspire the People: +15 Morale (1 day)

These are explicit, not drift.

---

# 5) No Upward Passive Scaling

Morale must NOT:

* Gradually trend upward just because nothing is happening.
* Auto-recover from low states without player action.
* Be stabilized permanently by any single law.

If players can keep Morale > 70 without continuous tradeoffs, tuning is incorrect.

---

# 6) Logging Requirement

Every Morale change must be logged in daily report:

Example:

Morale -12
-5 Food Deficit
-2 Overcrowding
-3 High Sickness
-2 Law Pressure

If recovery occurs:

Morale +2
+2 Temporary Stability

No opaque changes.

---

# 7) Tuning Guardrails

After implementation:

* If player ignores food/water → Morale should fall below 40 by Day 6–8.
* If player avoids deficits but enacts harsh laws → Morale should fall below 40 by Day 10–14.
* If player maintains stable conditions (rare early game), Morale should slowly climb but never exceed 80 without positive laws.

If Morale becomes irrelevant or always collapsing uncontrollably, adjust conditional magnitudes — not structure.

---

# Summary

Morale has:

* No flat base decay.
* Conditional hardship decay.
* Rare conditional recovery.
* Strong event/law impact.

Morale must feel reactive, not scripted.

Implement exactly as specified.
