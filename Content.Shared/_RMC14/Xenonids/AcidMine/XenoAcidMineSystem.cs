using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.AcidMine;

public sealed class XenoAcidMineSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAcidMineComponent, XenoAcidMineActionEvent>(OnXenoAcidMineAction);
    }

    private void OnXenoAcidMineAction(Entity<XenoAcidMineComponent> xeno, ref XenoAcidMineActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;

        if (!_interaction.InRangeUnobstructed(
                _transform.GetMapCoordinates(xeno.Owner),
                _transform.ToMapCoordinates(args.Target),
                xeno.Comp.Range,
                CollisionGroup.Opaque,
                e => e == xeno.Owner || !Transform(e).Anchored))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-acid-mine-see-fail"), xeno, xeno);
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        var popupSelf = Loc.GetString("rmc-xeno-acid-mine-self");
        var popupOthers = Loc.GetString("rmc-xeno-acid-mine-others", ("xeno", xeno));
        _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

        if (_net.IsClient)
            return;

        var protoId = xeno.Comp.BlastProto;
        var center = args.Target.Position.Floored() + Vector2.One / 2;
        var alreadyHit = new HashSet<EntityUid>();

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var blastUid = Spawn(protoId, new EntityCoordinates(args.Target.EntityId, center + new Vector2(x, y)));
                var blast = EnsureComp<XenoAcidBlastComponent>(blastUid);
                blast.Attached = xeno.Owner;
                blast.Empowered = xeno.Comp.Empowered;
                blast.AlreadyHit = alreadyHit;
                Dirty(blastUid, blast);
                _hive.SetSameHive(xeno.Owner, blastUid);
            }
        }

        xeno.Comp.Empowered = false;
        Dirty(xeno);
    }
}
