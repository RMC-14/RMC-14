using Content.Shared._RMC14.Dropship.Weapon;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Dropship.Weapon;

public sealed class DropshipAmmoVisualizerSystem : VisualizerSystem<DropshipAmmoComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, DropshipAmmoComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite is not { } spriteComp)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, DropshipAmmoVisuals.Fill, out var fill, args.Component))
            return;

        if (!spriteComp.LayerMapTryGet(DropshipAmmoVisuals.Fill, out var layer))
            return;

        if (component.AmmoType == null)
            return;

        var fillNum = Math.Clamp(fill / component.RoundsPerShot, 0, component.MaxRounds / component.RoundsPerShot);
        var state = component.AmmoType + "_" + fillNum;

        spriteComp.LayerSetState(layer, state);
    }
}
