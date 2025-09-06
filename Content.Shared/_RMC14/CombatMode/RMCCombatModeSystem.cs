using Content.Shared._RMC14.Attachable.Components;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.CombatMode;

public sealed class RMCCombatModeSystem : EntitySystem
{
    public SpriteSpecifier.Rsi? GetCrosshair(Entity<WieldedCrosshairComponent?, WieldableComponent?> crosshair)
    {
        // Require the held item to be wielded (this keeps existing behavior).
        if (!Resolve(crosshair, ref crosshair.Comp1, ref crosshair.Comp2, false))
            return null;

        if (crosshair.Comp2 is not { Wielded: true })
            return null;

        var heldUid = crosshair.Owner;

        // Prefer the superceding attachable (e.g., the underbarrel) if present.
        if (TryComp<AttachableHolderComponent>(heldUid, out var holder) &&
            holder.SupercedingAttachable is { } active &&
            TryComp<WieldedCrosshairComponent>(active, out var ubXhair) &&
            ubXhair.Rsi is { } ubSpec)
        {
            // Underbarrel is active and defines a crosshair — use it.
            return ubSpec;
        }

        // Fallback to the held item’s own crosshair (your normal rifle behavior).
        return crosshair.Comp1?.Rsi;
    }
}
