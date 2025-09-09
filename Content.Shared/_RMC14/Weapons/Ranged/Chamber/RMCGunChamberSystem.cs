using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared._RMC14.Weapons.Ranged.AimedShot;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared._RMC14.Weapons.Ranged.Foldable;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged.Chamber;

public sealed class RMCGunChamberSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCGunChamberComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer, after: new[] {typeof(SharedGunSystem)});
        SubscribeLocalEvent<RMCGunChamberComponent, TakeAmmoEvent>(OnTakeAmmo, before: new[] {typeof(SharedGunSystem)});
        SubscribeLocalEvent<RMCGunChamberComponent, UniqueActionEvent>(OnUniqueAction,
            after: new[]
            {
                typeof(SharedRMCAimedShotSystem), typeof(AttachableHolderSystem),
                typeof(SharedRMCFlamerSystem), typeof(RMCFoldableGunSystem),
                typeof(BreechLoadedSystem), typeof(CMGunSystem),
                typeof(RMCAirShotSystem), typeof(SharedPumpActionSystem),
            });
    }

    private void OnEntRemovedFromContainer(Entity<RMCGunChamberComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (args.Container.ID != SharedGunSystem.MagazineSlot)
            return;

        LoadChamber(ent, args.Entity);
    }

    private void OnTakeAmmo(Entity<RMCGunChamberComponent> ent, ref TakeAmmoEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var chamber))
            return;

        var shots = args.Shots;
        for (var i = 0; i < shots; i++)
        {
            if (chamber.ContainedEntities.FirstOrNull() is not { } bullet)
                break;

            args.Shots--;
            if (!_container.Remove(bullet, chamber))
                break;

            args.Ammo.Add((bullet, _gun.EnsureShootable(bullet)));
        }
    }

    private void LoadChamber(Entity<RMCGunChamberComponent> gun, EntityUid magazine)
    {
        if (TerminatingOrDeleted(gun))
            return;

        if (!TryComp(gun, out TransformComponent? xform))
            return;

        var chamber = _container.EnsureContainer<ContainerSlot>(gun, gun.Comp.ContainerId);
        if (chamber.ContainedEntity != null)
            return;

        if (_net.IsClient)
            return;

        var ammo = new List<(EntityUid? Entity, IShootable Shootable)>();
        var take = new TakeAmmoEvent(1, ammo, xform.Coordinates, null);
        RaiseLocalEvent(magazine, take);
        if (take.Ammo.FirstOrNull() is not { Entity: { } firstAmmo })
            return;

        _container.Insert(firstAmmo, chamber);
    }

    private void OnUniqueAction(Entity<RMCGunChamberComponent> ent, ref UniqueActionEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (args.Handled)
            return;

        if (!TryComp(ent, out TransformComponent? xform))
            return;

        args.Handled = true;
        var time = _timing.CurTime;
        if (ent.Comp.LastChamberedAt is { } last && time < last + ent.Comp.ChamberCooldown)
            return;

        var ammo = new List<(EntityUid? Entity, IShootable Shootable)>();
        var take = new TakeAmmoEvent(1, ammo, xform.Coordinates, null);
        RaiseLocalEvent(ent, take);

        if (take.Ammo.Count == 0)
            return;

        foreach (var (ammoEntNullable, _) in take.Ammo)
        {
            if (ammoEntNullable is not { } ammoEnt)
                continue;

            _transform.SetCoordinates(ammoEnt, _transform.GetMoverCoordinates(ammoEnt));
            if (IsClientSide(ammoEnt))
                QueueDel(ammoEnt);
        }

        ent.Comp.LastChamberedAt = time;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.Sound, ent, args.UserUid);
        _gun.UpdateAmmoCount(ent);
    }
}
