using Content.Shared._RMC14.Dropship.Weapon;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Dropship.Weapon;

public sealed class DropshipAmmoVisualizerSystem : VisualizerSystem<DropshipAmmoComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, DropshipAmmoComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite is not { } spriteComp)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, DropshipAmmoVisuals.Fill, out var fill, args.Component))
            return;

        if (!_sprite.LayerMapTryGet(new Entity<SpriteComponent?>(uid, spriteComp), DropshipAmmoVisuals.Fill, out var layer, false))
            return;

        if (component.AmmoType == null)
            return;

        var fillNum = Math.Clamp(fill / component.RoundsPerShot, 0, component.MaxRounds / component.RoundsPerShot);
        var state = component.AmmoType + "_" + fillNum;

        _sprite.LayerSetRsiState(new Entity<SpriteComponent?>(uid, spriteComp), layer, state);
    }
}
