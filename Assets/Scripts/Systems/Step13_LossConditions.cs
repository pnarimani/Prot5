using SiegeSurvival.Core;

namespace SiegeSurvival.Systems
{
    /// <summary>
    /// Step 13: Check loss conditions. Order: Keep Breach → Council Revolt → Total Collapse.
    /// </summary>
    public static class Step13_LossConditions
    {
        public static void Execute(GameState state, SimulationContext ctx, CausalityLog log)
        {
            // 1. Keep Breach
            if (ctx.keepBreached || state.Keep.currentIntegrity <= 0)
            {
                state.isGameOver = true;
                state.gameOverReason = "Breach";
                log.AddFlat(CausalityCategory.General, "GAME OVER", 0,
                    "The Keep has been breached. The city has fallen.");
                return;
            }

            // 2. Council Revolt (Unrest > 85)
            if (state.unrest > 85)
            {
                state.isGameOver = true;
                state.gameOverReason = "Council Revolt";
                log.AddFlat(CausalityCategory.General, "GAME OVER", 0,
                    $"Council Revolt! Unrest reached {state.unrest} (> 85). The people have overthrown you.");
                return;
            }

            // 3. Total Collapse (Food AND Water = 0 for 2 consecutive days)
            if (state.consecutiveFoodWaterZeroDays >= 2)
            {
                state.isGameOver = true;
                state.gameOverReason = "Total Collapse";
                log.AddFlat(CausalityCategory.General, "GAME OVER", 0,
                    "Total Collapse! Food and Water depleted for 2 consecutive days.");
                return;
            }
        }
    }
}
