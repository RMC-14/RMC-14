using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Designer.Events;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Designer;

public sealed class DesignerRemoteThickenResinSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DesignerStrainComponent, DesignerRemoteThickenResinDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<DesignerStrainComponent> ent, ref DesignerRemoteThickenResinDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (HasComp<WeedboundWallComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-designer-thicken-weedbound"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        if (!TryComp(target, out XenoStructureUpgradeableComponent? upgradeable) || upgradeable.To is null)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-designer-thicken-none"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        if (!_plasma.TryRemovePlasmaPopup(ent.Owner, args.PlasmaCost, predicted: false))
            return;

        var coords = Transform(target).Coordinates;
        var rotation = Transform(target).LocalRotation;

        var thickened = Spawn(upgradeable.To.Value, coords);
        _transform.SetLocalRotation(thickened, rotation);
        _hive.SetSameHive(ent.Owner, thickened);
        QueueDel(target);

        _popup.PopupClient(Loc.GetString("rmc-xeno-designer-thicken-success"), ent.Owner, ent.Owner);
    }
}
