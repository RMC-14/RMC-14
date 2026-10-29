using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.UniversalRecorder;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.UniversalRecorder;

[UsedImplicitly]
public sealed class UniversalRecorderRecorderBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly Dictionary<UniversalRecorderRecorderAction, (string Tooltip, SpriteSpecifier Sprite)> ActionData = new()
    {
        [UniversalRecorderRecorderAction.Record] = ("rmc-universal-recorder-verb-record", new SpriteSpecifier.Rsi(new ResPath("_RMC14/Actions/radial_taperecorder.rsi"), "record")),
        [UniversalRecorderRecorderAction.Play] = ("rmc-universal-recorder-verb-play", new SpriteSpecifier.Rsi(new ResPath("_RMC14/Actions/radial_taperecorder.rsi"), "play")),
        [UniversalRecorderRecorderAction.Stop] = ("rmc-universal-recorder-verb-stop", new SpriteSpecifier.Rsi(new ResPath("_RMC14/Actions/radial_taperecorder.rsi"), "stop")),
        [UniversalRecorderRecorderAction.PrintTranscript] = ("rmc-universal-recorder-verb-print", new SpriteSpecifier.Rsi(new ResPath("_RMC14/Actions/radial_taperecorder.rsi"), "print")),
        [UniversalRecorderRecorderAction.Eject] = ("rmc-universal-recorder-verb-eject", new SpriteSpecifier.Rsi(new ResPath("_RMC14/Actions/radial_taperecorder.rsi"), "eject")),
    };

    private SimpleRadialMenu? _menu;
    private UniversalRecorderRecorderBuiState? _state;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);

        if (_state != null)
            _menu.SetButtons(ConvertToButtons(_state.Actions));

        _menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not UniversalRecorderRecorderBuiState recorderState)
            return;

        _state = recorderState;
        _menu?.SetButtons(ConvertToButtons(recorderState.Actions));
    }

    private IEnumerable<RadialMenuActionOption> ConvertToButtons(IEnumerable<UniversalRecorderRecorderAction> actions)
    {
        return actions.Select(action =>
        {
            var data = ActionData[action];
            return new RadialMenuActionOption<UniversalRecorderRecorderAction>(OnActionPressed, action)
            {
                Sprite = data.Sprite,
                ToolTip = Loc.GetString(data.Tooltip),
            };
        });
    }

    private void OnActionPressed(UniversalRecorderRecorderAction action)
    {
        SendPredictedMessage(new UniversalRecorderRecorderActionBuiMsg(action));
    }
}
