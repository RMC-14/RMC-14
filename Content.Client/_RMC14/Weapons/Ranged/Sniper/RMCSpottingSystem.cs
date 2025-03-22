using Content.Client.Gameplay;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared._RMC14.Weapons.Common;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;

namespace Content.Client._RMC14.Weapons.Ranged.Sniper;

public sealed class RMCSpottingSystem : SharedRMCSpottingSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _state = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpottingComponent, UniqueActionEvent>(OnUniqueAction);
        SubscribeLocalEvent<RequestSpotEvent>(OnSpotRequest);
    }

    /// <summary>
    ///     Send a request to start spotting the entity below the mouse cursor when the unique action key is pressed.
    /// </summary>
    private void OnUniqueAction(Entity<SpottingComponent> ent, ref UniqueActionEvent args)
    {
        if (!Timing.IsFirstTimePredicted || args.Handled)
            return;

        var entity = _player.LocalEntity;

        if (entity == null || !ent.Comp.Activated)
            return;

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

        NetEntity? target = null;
        if (_state.CurrentState is GameplayStateBase screen)
            target = GetNetEntity(screen.GetClickedEntity(mousePos));

        if (_player.LocalSession is not { } session || target == null)
            return;

        RaisePredictiveEvent(new RequestSpotEvent()
        {
            Target = target.Value,
            User = GetNetEntity(args.UserUid),
            SpottingTool = GetNetEntity(ent.Owner),
        });

        args.Handled = true;
    }

    private void OnSpotRequest(RequestSpotEvent ev, EntitySessionEventArgs args)
    {
        SpotRequested(ev.SpottingTool, ev.User, ev.Target);
    }
}
