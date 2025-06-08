using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;

public sealed class ResinWhispererSystem : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _weeds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResinDoorComponent, GetVerbsEvent<AlternativeVerb>>(OnDoorAltVerb);

        SubscribeLocalEvent<ResinWhispererComponent, XenoSecreteStructureAdjustFields>(OnRemoteSecreteStructure);
        SubscribeLocalEvent<ResinWhispererComponent, InRangeOverrideEvent>(OnInRangeOverride);
    }

    private void OnDoorAltVerb(Entity<ResinDoorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
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
                if (!CanRemoteOpenDoorPopup(user, target))
                    return;

                if (!TryComp(target, out DoorComponent? doorComp))
                    return;

                if (!_door.TryToggleDoor(target, predicted: true))
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

    private bool CanRemoteOpenDoorPopup(Entity<ResinWhispererComponent?> user, EntityUid target, bool doPopup = true)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        if (!_weeds.IsOnFriendlyWeeds(user.Owner))
        {
            if (doPopup)
                _popup.PopupClient(Loc.GetString("rmc-xeno-construction-remote-failed-need-on-weeds"), user, user);

            return false;
        }

        if (!HasComp<DoorComponent>(target) ||
            !HasComp<ResinDoorComponent>(target))
        {
            return false;
        }

        return true;
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

        if (!_weeds.IsOnFriendlyWeeds(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-construction-remote-failed-need-on-weeds"), ent, ent);
            return;
        }

        constructComp.BuildDelay = ent.Comp.StandardConstructDelay.Value.Multiply(ent.Comp.RemoteConstructDelayMultiplier);
        constructComp.BuildRange = ent.Comp.MaxRemoteConstructDistance;
    }

    private void OnInRangeOverride(Entity<ResinWhispererComponent> ent, ref InRangeOverrideEvent args)
    {
        if (!CanRemoteOpenDoorPopup(ent.Owner, args.Target, false))
            return;

        args.InRange = true;
        args.Handled = true;
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
