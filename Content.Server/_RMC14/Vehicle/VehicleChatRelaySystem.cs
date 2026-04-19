using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle.Viewport;
using Content.Shared.Ghost;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Player;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server._RMC14.Vehicle;

public sealed class VehicleChatRelaySystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly VehicleSystem _vehicle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandRecipients);
    }

    private void OnExpandRecipients(ExpandICChatRecipientsEvent ev)
    {
        if (TryGetRelayTarget(ev.Source, out var sourceTarget))
        {
            AddRecipientsNearTarget(ev, sourceTarget);
            AddRelayUsersNearTarget(ev, sourceTarget);
        }

        AddRelayUsersNearSource(ev);
    }

    private void AddRelayUsersNearSource(ExpandICChatRecipientsEvent ev)
    {
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is not { Valid: true } user)
                continue;

            if (!TryGetRelayTarget(user, out var target))
                continue;

            if (!TryDistance(ev.Source, target, out var distance) || distance > ev.VoiceRange)
                continue;

            ev.Recipients.TryAdd(session, new ICChatRecipientData(distance, HasComp<GhostHearingComponent>(user)));
        }
    }

    private void AddRelayUsersNearTarget(ExpandICChatRecipientsEvent ev, EntityUid sourceTarget)
    {
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is not { Valid: true } user)
                continue;

            if (!TryGetRelayTarget(user, out var target))
                continue;

            if (!TryDistance(sourceTarget, target, out var distance) || distance > ev.VoiceRange)
                continue;

            ev.Recipients.TryAdd(session, new ICChatRecipientData(distance, HasComp<GhostHearingComponent>(user)));
        }
    }

    private void AddRecipientsNearTarget(ExpandICChatRecipientsEvent ev, EntityUid sourceTarget)
    {
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is not { Valid: true } recipient)
                continue;

            if (!TryDistance(sourceTarget, recipient, out var distance) || distance > ev.VoiceRange)
                continue;

            ev.Recipients.TryAdd(session, new ICChatRecipientData(distance, HasComp<GhostHearingComponent>(recipient)));
        }
    }

    private bool TryGetRelayTarget(EntityUid user, out EntityUid target)
    {
        target = default;

        if (TryComp(user, out VehicleViewToggleComponent? viewToggle))
        {
            if (!viewToggle.IsOutside ||
                viewToggle.OutsideTarget is not { } outsideTarget ||
                !HasComp<VehicleComponent>(outsideTarget))
            {
                return false;
            }

            target = outsideTarget;
            return true;
        }

        if (TryComp(user, out VehicleViewportUserComponent? viewport) &&
            viewport.Source is { } source &&
            _vehicle.TryGetVehicleFromInterior(source, out var viewportVehicle) &&
            viewportVehicle is { } viewportVehicleUid)
        {
            target = viewportVehicleUid;
            return true;
        }

        if (TryComp(user, out VehicleWeaponsOperatorComponent? weapons) &&
            weapons.Vehicle is { } weaponsVehicle &&
            Exists(weaponsVehicle))
        {
            target = weaponsVehicle;
            return true;
        }

        if (TryComp(user, out VehicleOperatorComponent? vehicleOperator) &&
            vehicleOperator.Vehicle is { } operatedVehicle &&
            Exists(operatedVehicle))
        {
            target = operatedVehicle;
            return true;
        }

        if (TryComp(user, out EyeComponent? eye) &&
            eye.Target is { } eyeTarget &&
            HasComp<VehicleComponent>(eyeTarget))
        {
            target = eyeTarget;
            return true;
        }

        return false;
    }

    private bool TryDistance(EntityUid first, EntityUid second, out float distance)
    {
        distance = 0f;
        if (!Exists(first) || !Exists(second))
            return false;

        return Transform(first).Coordinates.TryDistance(EntityManager, Transform(second).Coordinates, out distance);
    }
}
