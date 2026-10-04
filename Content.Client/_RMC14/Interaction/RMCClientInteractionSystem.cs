using Content.Client._RMC14.Movement;
using Content.Client.ContextMenu.UI;
using Content.Client.Gameplay;
using Content.Shared._RMC14.Interaction;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Graphics;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Interaction;

public sealed partial class RMCClientInteractionSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly RMCLagCompensationSystem _rmcLag = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private EntityQuery<PullerComponent> _pullerQuery;
    private EntityQuery<PullableComponent> _pullableQuery;

    public override void Initialize()
    {
        _pullerQuery = GetEntityQuery<PullerComponent>();
        _pullableQuery = GetEntityQuery<PullableComponent>();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TryPullObject,
                new PointerInputCmdHandler(HandleTryPullObject))
            .Register<RMCClientInteractionSystem>();
    }

    private bool HandleTryPullObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (_playerManager.LocalEntity is not { } user)
            return false;

        if (!_pullerQuery.TryComp(user, out var puller))
            return false;

        var entityMenuController = _ui.GetUIController<EntityMenuUIController>();

        // Only look for better targets if the target didn't come specifically from the context menu.
        if (!entityMenuController.HandlingContextMenuInput)
        {
            var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

            if (mousePos.MapId == MapId.Nullspace)
                return false;

            if (_stateManager.CurrentState is not GameplayStateBase screen)
                return false;

            var clickables = screen.GetClickableEntities(mousePos);

            foreach (var clickable in clickables)
            {
                if (!_pullableQuery.HasComp(clickable)
                    || !_pulling.CanPull(user, clickable, puller))
                {
                    continue;
                }

                uid = clickable;
                break;
            }
        }

        _rmcLag.SendLastRealTick();

        if (_timing.IsFirstTimePredicted)
        {
            RaisePredictiveEvent(new RMCTryGrabEvent(GetNetCoordinates(coords), GetNetEntity(uid)));
        }

        return true;
    }

    public bool IsInteractionTransparency(EntityUid target, EntityUid? localEntity, IEye? eye)
    {
        if (localEntity is not { } user ||
            eye == null ||
            !HasComp<InteractionTransparencyComponent>(target))
        {
            return false;
        }

        if (!TryComp(target, out TransformComponent? entXform) ||
            !TryComp(target, out SpriteComponent? sprite) ||
            !TryComp(user, out TransformComponent? playerXform))
        {
            return false;
        }

        var (spritePos, spriteRot) = _transform.GetWorldPositionRotation(entXform);
        var spriteBox = _sprite.CalculateBounds((target, sprite), spritePos, spriteRot, eye.Rotation);
        var playerPos = _transform.GetMapCoordinates(playerXform).Position;

        return spriteBox.Contains(playerPos);
    }
}
