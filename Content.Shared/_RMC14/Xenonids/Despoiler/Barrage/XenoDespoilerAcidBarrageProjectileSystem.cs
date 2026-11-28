using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Despoiler.Lingering;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Barrage;

public sealed class XenoDespoilerAcidBarrageProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private EntityQuery<XenoDespoilerLingeringAcidComponent> _lingeringQuery;

    public override void Initialize()
    {
        _lingeringQuery = GetEntityQuery<XenoDespoilerLingeringAcidComponent>();

        SubscribeLocalEvent<XenoDespoilerAcidBarrageProjectileComponent, EntityTerminatingEvent>(OnTerminate);
    }

    private void OnTerminate(Entity<XenoDespoilerAcidBarrageProjectileComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(ent, out TransformComponent? xform))
            return;

        if (TerminatingOrDeleted(xform.ParentUid))
            return;

        if (!_random.Prob(ent.Comp.LingeringAcidChance))
            return;

        var puddle = Spawn(ent.Comp.LingeringAcidProto, xform.Coordinates.SnapToGrid(EntityManager));
        if (ent.Comp.Shooter is { } shooter)
            _hive.SetSameHive(shooter, puddle);

        if (!_lingeringQuery.TryComp(puddle, out var puddleComp))
            return;

        puddleComp.Caster = ent.Comp.Shooter;
        Dirty(puddle, puddleComp);
    }
}
