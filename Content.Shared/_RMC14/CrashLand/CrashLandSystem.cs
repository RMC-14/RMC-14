using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Rules;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.CrashLand;

public sealed class CrashLandSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private static readonly ProtoId<DamageTypePrototype> CrashLandDamageType = "Blunt";
    private const int CrashLandDamageAmount = 10000;

    private bool _crashLandEnabled;

    private EntityQuery<CrashLandableComponent> _crashLandableQuery;

    public override void Initialize()
    {
        _crashLandableQuery = GetEntityQuery<CrashLandableComponent>();

        SubscribeLocalEvent<CrashLandableComponent, EntParentChangedMessage>(OnCrashLandableParentChanged);

        SubscribeLocalEvent<CrashLandOnTouchComponent, StartCollideEvent>(OnCrashLandOnTouchStartCollide);

        SubscribeLocalEvent<DeleteCrashLandableOnTouchComponent, StartCollideEvent>(OnDeleteCrashLandableOnTouchStartCollide);

        Subs.CVar(_config, RMCCVars.RMCFTLCrashLand, v => _crashLandEnabled = v, true);
    }

    private void OnCrashLandableParentChanged(Entity<CrashLandableComponent> crashLandable, ref EntParentChangedMessage args)
    {
        if (!_crashLandEnabled || !HasComp<FTLMapComponent>(args.Transform.ParentUid))
            return;

        TryCrashLand(crashLandable, true);
    }

    private void OnCrashLandOnTouchStartCollide(Entity<CrashLandOnTouchComponent> ent, ref StartCollideEvent args)
    {
        if (!_crashLandEnabled || !_crashLandableQuery.HasComp(args.OtherEntity))
            return;

        TryCrashLand(args.OtherEntity, true);
    }

    private void OnDeleteCrashLandableOnTouchStartCollide(Entity<DeleteCrashLandableOnTouchComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsClient)
            return;

        if (!_crashLandEnabled || !_crashLandableQuery.HasComp(args.OtherEntity))
            return;

        QueueDel(args.OtherEntity);
    }

    public bool TryGetCrashLandLocation(out EntityCoordinates location)
    {
        location = default;
        var distressQuery = EntityQueryEnumerator<RMCPlanetComponent>();
        while (distressQuery.MoveNext(out var grid, out _))
        {
            if (!TryComp<MapGridComponent>(grid, out var gridComp))
                return false;

            var xform = Transform(grid);
            location = xform.Coordinates;
            for (var i = 0; i < 250; i++)
            {
                // TODO RMC14 every single method used in content and engine for "random spot" is broken with planet maps. Splendid!
                var randomX = _random.Next(-200, 200);
                var randomY = _random.Next(-200, 200);
                var tile = new Vector2i(randomX, randomY);
                if (!_mapSystem.TryGetTileRef(grid, gridComp, tile, out var tileRef) ||
                    tileRef.GetContentTileDefinition().ID == ContentTileDefinition.SpaceID)
                    continue;

                // no air-blocked areas.
                if (tileRef.IsSpace() ||
                    _turf.IsTileBlocked(tileRef, CollisionGroup.MobMask))
                {
                    continue;
                }

                // don't spawn inside of solid objects
                var physQuery = GetEntityQuery<PhysicsComponent>();
                var valid = true;

                var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(grid, gridComp, tile);
                while (anchored.MoveNext(out var ent))
                {
                    if (!physQuery.TryGetComponent(ent, out var body))
                        continue;
                    if (body.BodyType != BodyType.Static ||
                        !body.Hard ||
                        (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                        continue;

                    valid = false;
                    break;
                }

                if (!valid)
                    continue;

                location = _mapSystem.GridTileToLocal(grid, gridComp, tile);
                return true;
            }
        }

        return false;
    }

    public void TryCrashLand(EntityUid crashLandable, bool doDamage)
    {
        if (_net.IsClient)
            return;

        if (!TryGetCrashLandLocation(out var location))
            return;

        if (doDamage)
        {
            var damage = new DamageSpecifier
            {
                DamageDict =
                {
                    [CrashLandDamageType] = CrashLandDamageAmount,
                },
            };

            _damageable.TryChangeDamage(crashLandable, damage);
        }

        _transform.SetMapCoordinates(crashLandable, _transform.ToMapCoordinates(location));
    }
}
