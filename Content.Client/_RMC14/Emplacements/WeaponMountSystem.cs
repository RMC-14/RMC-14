using Content.Shared._RMC14.Emplacements;
using Content.Shared.Foldable;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Emplacements;

public sealed class WeaponMountSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponMountComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<WeaponMountComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnHandleState(Entity<WeaponMountComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnAppearanceChange(Entity<WeaponMountComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<WeaponMountComponent> mount)
    {
        if (!TryComp(mount, out SpriteComponent? sprite))
            return;

        TryComp(mount, out FoldableComponent? foldable);

        if (_sprite.LayerMapTryGet((mount, sprite), WeaponMountComponentVisualLayers.Mounted, out var mountLayer, false))
        {
            var state = mount.Comp.MountedEntity != null;

            if (foldable != null)
            {
                state = state && !foldable.IsFolded;
            }

            UpdateAmmoVisual(mount, (mount, sprite), WeaponMountComponentVisualLayers.MountedAmmo);
            _sprite.LayerSetVisible((mount,sprite), mountLayer, state);
        }

        if (_sprite.LayerMapTryGet((mount, sprite), WeaponMountComponentVisualLayers.Folded, out var foldedLayer, false) &&
            foldable != null)
        {

            UpdateAmmoVisual(mount, (mount, sprite), WeaponMountComponentVisualLayers.FoldedAmmo);
            _sprite.LayerSetVisible((mount,sprite),foldedLayer, foldable.IsFolded && mount.Comp.MountedEntity != null);
            _sprite.LayerSetVisible((mount,sprite),"foldedLayer", foldable.IsFolded && mount.Comp.MountedEntity == null);
        }

        // Set the draw depth based on the mount's assembly/folded state
        if (mount.Comp.MountedEntity == null || foldable != null && foldable.IsFolded)
            _sprite.SetDrawDepth((mount,sprite), (int)DrawDepth.Items);
        else
            _sprite.SetDrawDepth((mount,sprite), (int)DrawDepth.Mobs);
    }

    private void UpdateAmmoVisual(Entity<WeaponMountComponent> mount, Entity<SpriteComponent?> sprite, Enum mapKey)
    {
        var hasAmmo = false;

        if (!_sprite.LayerMapTryGet(sprite, mapKey, out var mountAmmoLayer, false))
            return;

        if (mount.Comp.MountedEntity != null)
        {
            var ev = new GetAmmoCountEvent();
            RaiseLocalEvent(mount.Comp.MountedEntity.Value, ref ev);
            if (ev.Count > 0)
                hasAmmo = true;
        }

        _sprite.LayerSetVisible((mount,sprite), mountAmmoLayer, hasAmmo);
    }
}
