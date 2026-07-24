using Content.Server.Shuttles.Components;
using Content.Shared._RMC14.Overwatch;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Overwatch;

public sealed class RMCServerOverwatchTripodCameraSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RMCOverwatchTripodCameraSystem _tripod = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActorComponent, RMCOverwatchTripodRenameInputEvent>(OnRenameInput);
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, RMCOverwatchTripodDeployAttemptEvent>(OnDeployAttempt);
    }

    private void OnDeployAttempt(Entity<RMCOverwatchTripodCameraComponent> ent, ref RMCOverwatchTripodDeployAttemptEvent args)
    {
        var grid = Transform(args.User).GridUid;
        if (grid is { } gridId && HasComp<ShuttleComponent>(gridId))
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("rmc-overwatch-tripod-camera-invalid-location"), ent, args.User, PopupType.SmallCaution);
        }
    }

    private void OnRenameInput(Entity<ActorComponent> actor, ref RMCOverwatchTripodRenameInputEvent args)
    {
        if (_timing.ApplyingState || !TryGetEntity(args.Camera, out var camera) || !TryComp(camera, out RMCOverwatchTripodCameraComponent? tripod))
            return;

        if (!TryGetEntity(args.User, out var user) || user.Value != actor.Owner)
            return;

        if (!_mobState.IsAlive(user.Value) ||
            _mobState.IsIncapacitated(user.Value) ||
            !_actionBlocker.CanInteract(user.Value, camera.Value) ||
            !_interaction.InRangeUnobstructed(user.Value, camera.Value) ||
            (!tripod.Deployed && _hands.GetActiveItem(user.Value) != camera.Value))
        {
            return;
        }

        var label = args.Message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (label.Length == 0)
            return;

        _tripod.SetLabel((camera.Value, tripod), label[..Math.Min(label.Length, 32)]);
        _popup.PopupEntity(
            Loc.GetString("rmc-overwatch-tripod-camera-renamed",
                ("name", FormattedMessage.EscapeText(_tripod.GetDisplayLabel((camera.Value, tripod))))),
            camera.Value,
            user.Value);
    }
}
