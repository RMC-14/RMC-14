using Content.Shared.Wieldable.Components;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.CombatMode;

public sealed class RMCCombatModeSystem : EntitySystem
{
    public SpriteSpecifier.Rsi? GetCrosshair(Entity<WieldedCrosshairComponent?, WieldableComponent?> crosshair)
    {
        if (!Resolve(crosshair, ref crosshair.Comp1, ref crosshair.Comp2, false))
            return null;

        if (!crosshair.Comp2.Wielded)
            return null;

        return crosshair.Comp1.Rsi;
    }
}
