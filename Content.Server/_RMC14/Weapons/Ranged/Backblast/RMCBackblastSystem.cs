using System.Numerics;
using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Stun;
using Content.Shared.Directions;
using Content.Shared.Drunk;
using Content.Shared.Maps;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Server._RMC14.Weapons.Ranged.Backblast;

public sealed class RMCBackblastSystem : EntitySystem
{
    [Dependency] private readonly SharedDeafnessSystem _deafness = default!;
    [Dependency] private readonly SharedDrunkSystem _drunk = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStutteringSystem _stutter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly HashSet<EntityUid> _affected = new();
    private readonly HashSet<EntityUid> _processed = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBackblastComponent, GunShotEvent>(OnGunShot);
    }

    private void OnGunShot(Entity<RMCBackblastComponent> launcher, ref GunShotEvent args)
    {
        var shooter = _transform.GetMapCoordinates(args.User);
        var target = _transform.ToMapCoordinates(args.ToCoordinates);

        if (shooter.MapId != target.MapId)
            return;

        var shotVector = target.Position - shooter.Position;
        if (shotVector == Vector2.Zero)
            return;

        var rearDirection = shotVector.ToWorldAngle().GetCardinalDir().GetOpposite();
        var nearCoordinates = new MapCoordinates(shooter.Position + rearDirection.ToVec(), shooter.MapId);
        var farCoordinates = new MapCoordinates(shooter.Position + rearDirection.ToVec() * 2, shooter.MapId);

        Spawn(launcher.Comp.NearEffect, nearCoordinates);
        Spawn(launcher.Comp.FarEffect, farCoordinates);

        _processed.Clear();
        ApplyBackblast(nearCoordinates, shooter, args.User, launcher.Comp);
        ApplyBackblast(farCoordinates, shooter, args.User, launcher.Comp);
    }

    private void ApplyBackblast(
        MapCoordinates coordinates,
        MapCoordinates shooter,
        EntityUid shooterEntity,
        RMCBackblastComponent component)
    {
        _affected.Clear();
        _turf.GetEntitiesInTile(_transform.ToCoordinates(coordinates), _affected, LookupFlags.Uncontained);

        foreach (var marine in _affected)
        {
            if (marine == shooterEntity || !HasComp<MarineComponent>(marine) || !_processed.Add(marine))
                continue;

            _sizeStun.KnockBack(
                marine,
                shooter,
                component.KnockbackDistance,
                component.KnockbackDistance,
                component.KnockbackSpeed);
            _stun.TryKnockdown(marine, component.KnockdownTime, true);
            _deafness.TryDeafen(marine, component.DeafTime, true);
            _stutter.DoStutter(marine, component.StutterTime, true);
            _drunk.TryApplyDrunkenness(marine, (float) component.DizzyTime.TotalSeconds, false);
        }
    }
}
