using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Designer.Events;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Designer;

public sealed class DesignerRemoteThickenResinSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruction = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DesignerStrainComponent, DesignerRemoteThickenResinDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<DesignerStrainComponent> ent, ref DesignerRemoteThickenResinDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        var target = GetEntity(args.TargetEntity);
        if (!target.Valid || Deleted(target) || Terminating(target))
            return;
        var targetXform = Transform(target);

        if (args.Range > 0)
        {
            var origin = _transform.GetMoverCoordinates(ent.Owner);
            var targetCoords = targetXform.Coordinates;
            if (!_transform.InRange(origin, targetCoords, args.Range))
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-cant-reach-there"), ent.Owner, ent.Owner, PopupType.SmallCaution);
                return;
            }
        }

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

        if (!_plasma.TryRemovePlasmaPopup(ent.Owner, args.PlasmaCost))
            return;

        var coords = targetXform.Coordinates;
        var rotation = targetXform.LocalRotation;

        _popup.PopupClient(Loc.GetString("rmc-xeno-designer-thicken-success"), ent.Owner, ent.Owner);
        _audio.PlayPredicted(ent.Comp.RemoteThickenSound, coords, ent.Owner);

        if (_net.IsClient)
            return;

        try
        {
            _xenoConstruction.BeginStructureUpgrade(target);
            Del(target);
            var thickened = Spawn(upgradeable.To.Value, coords);
            _transform.SetLocalRotation(thickened, rotation);
            _hive.SetSameHive(ent.Owner, thickened);
        }
        finally
        {
            _xenoConstruction.EndStructureUpgrade(target);
        }
    }
}
