using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Despoiler;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Xenonids.Despoiler;

public sealed class XenoDespoilerAcidBarrageProjectileSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<XenoDespoilerLingeringAcidComponent> _lingeringQuery;

    public override void Initialize()
    {
        _xformQuery = GetEntityQuery<TransformComponent>();
        _lingeringQuery = GetEntityQuery<XenoDespoilerLingeringAcidComponent>();

        SubscribeLocalEvent<XenoDespoilerAcidBarrageProjectileComponent, EntityTerminatingEvent>(OnTerminate);
    }

    private void OnTerminate(EntityUid uid, XenoDespoilerAcidBarrageProjectileComponent comp, ref EntityTerminatingEvent args)
    {
        if (!_xformQuery.TryComp(uid, out var xform))
            return;

        if (TerminatingOrDeleted(xform.ParentUid))
            return;

        if (!_random.Prob(comp.LingeringAcidChance))
            return;

        var puddle = Spawn(comp.LingeringAcidProto, xform.Coordinates.SnapToGrid(EntityManager));
        if (comp.Shooter is { } shooter)
            _hive.SetSameHive(shooter, puddle);

        if (_lingeringQuery.TryComp(puddle, out var puddleComp))
        {
            puddleComp.Caster = comp.Shooter;
            Dirty(puddle, puddleComp);
        }
    }
}
