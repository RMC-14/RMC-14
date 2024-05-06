using Content.Shared._CM14.Marines.HyperSleep;
using Robust.Client.GameObjects;

namespace Content.Client._CM14.Marines.HyperSleep;

public sealed class HyperSleepChamberSystem : SharedHyperSleepChamberSystem
{
    protected override void ChamberFilled(Entity<HyperSleepChamberComponent> chamber)
    {
        base.ChamberFilled(chamber);

        if (TryComp(chamber, out SpriteComponent? sprite))
            sprite.LayerSetState(HyperSleepChamberLayers.Base, chamber.Comp.FilledState);
    }

    protected override void ChamberEmptied(Entity<HyperSleepChamberComponent> chamber)
    {
        base.ChamberEmptied(chamber);

        if (TryComp(chamber, out SpriteComponent? sprite))
            sprite.LayerSetState(HyperSleepChamberLayers.Base, chamber.Comp.EmptyState);
    }
}
