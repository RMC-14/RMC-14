using Content.Shared.Coordinates;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.CM14.Xenos.Acid;

public sealed class XenoAcidSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoCorrosiveAcidEvent>(OnXenoCorrosiveAcid);
        SubscribeLocalEvent<CorrodingComponent, EntityUnpausedEvent>(OnCorrodingUnpaused);
    }

    private void OnXenoCorrosiveAcid(Entity<XenoComponent> xeno, ref XenoCorrosiveAcidEvent args)
    {
        if (!HasComp<CorrodableComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-acid-not-corrodable", ("target", args.Target)), xeno, xeno);
            return;
        }

        if (HasComp<CorrodingComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-acid-already-corroding", ("target", args.Target)), xeno, xeno);
            return;
        }

        if (!_xeno.TryRemovePlasmaPopup(xeno, args.PlasmaCost))
            return;

        // TODO CM14 fake acid spawning on the client
        if (_net.IsClient)
            return;

        var acid = SpawnAttachedTo(args.AcidId, args.Target.ToCoordinates());
        AddComp(args.Target, new CorrodingComponent
        {
            Acid = acid,
            CorrodesAt = _timing.CurTime + args.Time
        });
    }

    private void OnCorrodingUnpaused(Entity<CorrodingComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.CorrodesAt += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<CorrodingComponent>();
        var time = _timing.CurTime;

        while (query.MoveNext(out var uid, out var corroding))
        {
            if (time < corroding.CorrodesAt)
                continue;

            QueueDel(uid);
            QueueDel(corroding.Acid);
        }
    }
}
