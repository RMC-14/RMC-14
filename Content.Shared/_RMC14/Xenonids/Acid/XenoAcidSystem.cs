using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Acid;

public sealed class XenoAcidSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoAcidComponent, XenoCorrosiveAcidEvent>(OnXenoCorrosiveAcid);
        SubscribeLocalEvent<XenoAcidComponent, XenoCorrosiveAcidDoAfterEvent>(OnXenoCorrosiveAcidDoAfter);
    }

    private void OnXenoCorrosiveAcid(Entity<XenoAcidComponent> xeno, ref XenoCorrosiveAcidEvent args)
    {
        if (xeno.Owner != args.Performer ||
            !CheckCorrodiblePopups(xeno, args.Target))
        {
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.AcidDelay, new XenoCorrosiveAcidDoAfterEvent(args), xeno, args.Target)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoCorrosiveAcidDoAfter(Entity<XenoAcidComponent> xeno, ref XenoCorrosiveAcidDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        if (!CheckCorrodiblePopups(xeno, target))
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
            return;

        if (_net.IsClient)
            return;

        args.Handled = true;

        var acid = SpawnAttachedTo(args.AcidId, target.ToCoordinates());
        AddComp(target, new CorrodingComponent
        {
            Acid = acid,
            CorrodesAt = _timing.CurTime + args.Time
        });
    }

    private bool CheckCorrodiblePopups(Entity<XenoAcidComponent> xeno, EntityUid target)
    {
        if (!TryComp(target, out CorrodibleComponent? corrodible) ||
            !corrodible.IsCorrodible)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-acid-not-corrodible", ("target", target)), xeno, xeno, PopupType.SmallCaution);
            return false;
        }

        if (HasComp<CorrodingComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-acid-already-corroding", ("target", target)), xeno, xeno);
            return false;
        }

        return true;
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
