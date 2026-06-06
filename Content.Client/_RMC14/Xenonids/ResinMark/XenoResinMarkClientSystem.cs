using Content.Shared._RMC14.Xenonids.ResinMark;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Input;

namespace Content.Client._RMC14.Xenonids.ResinMark;

public sealed class XenoResinMarkClientSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private KeyFunctionId _mouseMiddleId;
    private EntityUid? _openMenuOwner;

    public override void Initialize()
    {
        base.Initialize();
        _mouseMiddleId = _input.NetworkBindMap.KeyFunctionID(ContentKeyFunctions.MouseMiddle);
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MouseMiddle, new PointerInputCmdHandler(OnMiddleClick, outsidePrediction: true))
            .Register<XenoResinMarkClientSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<XenoResinMarkClientSystem>();
        _openMenuOwner = null;
    }

    public void SetMenuOpen(EntityUid owner, bool open)
    {
        if (open)
        {
            _openMenuOwner = owner;
            return;
        }

        if (_openMenuOwner == owner)
            _openMenuOwner = null;
    }

    private bool OnMiddleClick(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.OriginalMessage.InputFunctionId != _mouseMiddleId)
            return false;

        if (_ui.CurrentlyHovered is not IViewportControl)
            return false;

        if (_player.LocalEntity is not { } player ||
            !HasComp<XenoResinMarkComponent>(player))
        {
            return false;
        }

        if (_openMenuOwner != player)
            return false;

        RaisePredictiveEvent(new XenoResinMarkPlaceRequestEvent(GetNetCoordinates(args.Coordinates)));
        return true;
    }
}
