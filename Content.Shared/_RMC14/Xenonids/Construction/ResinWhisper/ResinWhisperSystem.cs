using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;

public sealed partial class ResinWhisperSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _weeds = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;

    private List<EntProtoId> _resinDoorPrototypes = new() { "DoorXenoResin", "DoorXenoResinThick" };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResinWhisperComponent, XenoSecreteStructureAdjustFields>(OnRemoteSecreteStructure);
        //SubscribeLocalEvent<ResinWhisperComponent, UserActivateInWorldEvent>(OnRemoteOpenDoor);
    }

    private void OnRemoteSecreteStructure(EntityUid ent, ResinWhisperComponent comp, XenoSecreteStructureAdjustFields args)
    {
        if (!TryComp(ent, out XenoConstructionComponent? constructComp))
        {
            return;
        }

        if (comp.StandardConstructDelay is TimeSpan)
        {
            constructComp.BuildDelay = comp.StandardConstructDelay.Value;
        }
        else
        {
            comp.StandardConstructDelay = constructComp.BuildDelay;
        }

        if (comp.MaxConstructDistance is FixedPoint2)
        {
            constructComp.BuildRange = comp.MaxConstructDistance.Value;
        }
        else
        {
            comp.MaxConstructDistance = constructComp.BuildRange;
        }

        if (_interaction.InRangeUnobstructed(ent, args.TargetCoordinates, comp.MaxConstructDistance.Value.Float()))
        {
            return;
        }

        if (!_interaction.InRangeUnobstructed(ent, args.TargetCoordinates, comp.MaxRemoteConstructDistance))
        {
            return;
        }

        if (!_weeds.IsOnWeeds(ent))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-remote-failed-need-on-weeds"), ent, ent);
            return;
        }

        constructComp.BuildDelay = comp.StandardConstructDelay.Value.Multiply(comp.RemoteConstructDelayMultiplier);
        constructComp.BuildRange = comp.MaxRemoteConstructDistance;
    }

    private void OnRemoteOpenDoor(EntityUid ent, ResinWhisperComponent comp, UserActivateInWorldEvent args)
    {

    }
}
