using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;

public sealed class ResinWhispererSystem : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _weeds = default!;

    private readonly List<EntProtoId> _resinDoorPrototypes = new() { "DoorXenoResin", "DoorXenoResinThick" };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, GetVerbsEvent<AlternativeVerb>>(OnDoorAltVerb);

        SubscribeLocalEvent<ResinWhispererComponent, XenoSecreteStructureAdjustFields>(OnRemoteSecreteStructure);
    }

    private void OnDoorAltVerb(Entity<DoorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!HasComp<ResinWhispererComponent>(args.User))
            return;

        var target = args.Target;
        var user = args.User;
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = "Open Door",
            Impact = LogImpact.Low,
            Act = () =>
            {
                if (!_weeds.IsOnWeeds(user))
                {
                    _popup.PopupClient(Loc.GetString("rmc-xeno-construction-remote-failed-need-on-weeds"), user, user);
                    return;
                }

                if (Prototype(target) is not { } targetProto ||
                    !_resinDoorPrototypes.Contains(targetProto.ID) ||
                    !TryComp(target, out DoorComponent? doorComp))
                {
                    return;
                }

                if (!_door.TryToggleDoor(target))
                    return;

                if (doorComp.State == DoorState.Opening)
                {
                    _popup.PopupClient(Loc.GetString("rmc-xeno-construction-remote-open-door"), user, user);
                }
                if (doorComp.State == DoorState.Closing)
                {
                    _popup.PopupClient(Loc.GetString("rmc-xeno-construction-remote-close-door"), user, user);
                }
            },
            Priority = 100,
        });
    }

    private void OnRemoteSecreteStructure(Entity<ResinWhispererComponent> ent, ref XenoSecreteStructureAdjustFields args)
    {
        if (!TryComp(ent, out XenoConstructionComponent? constructComp))
            return;

        if (ent.Comp.StandardConstructDelay != null)
            constructComp.BuildDelay = ent.Comp.StandardConstructDelay.Value;
        else
            ent.Comp.StandardConstructDelay = constructComp.BuildDelay;

        if (ent.Comp.MaxConstructDistance != null)
            constructComp.BuildRange = ent.Comp.MaxConstructDistance.Value;
        else
            ent.Comp.MaxConstructDistance = constructComp.BuildRange;

        if (_interaction.InRangeUnobstructed(ent, args.TargetCoordinates, ent.Comp.MaxConstructDistance.Value.Float()))
            return;

        if (!TileIsVisible(ent, args.TargetCoordinates))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-construction-remote-failed-need-line-of-sight"), ent, ent);
            return;
        }

        if (!_weeds.IsOnWeeds(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-construction-remote-failed-need-on-weeds"), ent, ent);
            return;
        }

        constructComp.BuildDelay = ent.Comp.StandardConstructDelay.Value.Multiply(ent.Comp.RemoteConstructDelayMultiplier);
        constructComp.BuildRange = ent.Comp.MaxRemoteConstructDistance;
    }

    private bool TileIsVisible(Entity<ResinWhispererComponent> ent, EntityCoordinates targetCoordinates)
    {
        //Check coordinates of center, then check 4 corners and 4 edges, clockwise starting from the eastern edge
        var pointCoordinates = _transform.ToMapCoordinates(targetCoordinates);
        for (int i = 0; i < 9; i++)
        {
            switch (i)
            {
                case 1: case 7: case 8:
                    pointCoordinates = pointCoordinates.Offset(0.499f, 0);
                    break;
                case 2:
                    pointCoordinates = pointCoordinates.Offset(0, -0.499f);
                    break;
                case 3: case 4:
                    pointCoordinates = pointCoordinates.Offset(-0.499f, 0);
                    break;
                case 5: case 6:
                    pointCoordinates = pointCoordinates.Offset(0, 0.499f);
                    break;
                default:
                    break;
            }
                
            if (_examineSystem.InRangeUnOccluded(ent, pointCoordinates, ent.Comp.MaxRemoteConstructDistance))
            {
                return true;
                break;
            }
        }

        return false;
    }
}
