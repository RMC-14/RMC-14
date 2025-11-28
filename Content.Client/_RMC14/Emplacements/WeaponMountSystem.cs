using Content.Shared._RMC14.Emplacements;
using Content.Shared.Foldable;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Emplacements;

public sealed class WeaponMountSystem : SharedWeaponMountSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string FoldedLayer = "foldedLayer";

    public override void Initialize()
    {
        base.Initialize();
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
                state = state && !foldable.IsFolded && !mount.Comp.Broken;
            }

            // Only show the mounted ammo sprite if the mount is not folded.
            if (foldable != null && foldable.IsFolded)
                _sprite.LayerSetVisible((mount,sprite), WeaponMountComponentVisualLayers.MountedAmmo, false);

            if (foldable == null || !foldable.IsFolded)
                    UpdateAmmoVisual(mount, (mount, sprite), WeaponMountComponentVisualLayers.MountedAmmo);

            // Enable the mounted layer if the mount is deployed
            _sprite.LayerSetVisible((mount,sprite), mountLayer, state);

            // Show broken state
            if (_sprite.LayerMapTryGet((mount, sprite), WeaponMountComponentVisualLayers.Broken, out var brokenLayer, false))
                _sprite.LayerSetVisible((mount,sprite), brokenLayer, mount.Comp.Broken);
        }

        if (_sprite.LayerMapTryGet((mount, sprite), WeaponMountComponentVisualLayers.Folded, out var transportLayer, false) &&
            foldable != null)
        {
            // Only show the folded ammo sprite if the mount is folded
            if (foldable.IsFolded)
                UpdateAmmoVisual(mount, (mount, sprite), WeaponMountComponentVisualLayers.FoldedAmmo);
            else
            {
                _sprite.LayerSetVisible((mount, sprite), WeaponMountComponentVisualLayers.FoldedAmmo, false);
            }

            var folded = foldable.IsFolded && !mount.Comp.Broken;

            // Set the folded state based on if the mount is folded and has a mounted entity.
            _sprite.LayerSetVisible((mount,sprite), transportLayer, folded && mount.Comp.MountedEntity != null);

            // Disable the folded layer from the FoldedComponent
            if (_sprite.LayerMapTryGet((mount, sprite), FoldedLayer, out var foldedLayer, false))
                _sprite.LayerSetVisible((mount,sprite), foldedLayer, folded && mount.Comp.MountedEntity == null);

            // Show broken state
            if (_sprite.LayerMapTryGet((mount, sprite), WeaponMountComponentVisualLayers.Broken, out var brokenLayer, false))
                _sprite.LayerSetVisible((mount,sprite), brokenLayer, mount.Comp.Broken);
        }

        // Set the draw depth based on the mount's assembly/folded state
        if (mount.Comp.MountedEntity == null || foldable != null && foldable.IsFolded)
            _sprite.SetDrawDepth((mount,sprite), (int)DrawDepth.Items);
        else
            _sprite.SetDrawDepth((mount,sprite), (int)DrawDepth.Mobs);
    }

    /// <summary>
    ///     Updates the ammo sprite based on if there is any ammo left in the gun.
    /// </summary>
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
