using System.Numerics;
using Content.Client._RMC14.Chemistry.Master;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Labeler;

public sealed class RMCHandLabelerPillBottleColorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RMCChemMasterPopupWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window?.Close();

        var spriteSystem = EntMan.System<SpriteSystem>();
        _window = new RMCChemMasterPopupWindow { Title = Loc.GetString("rmc-hand-labeler-pill-bottle-color") };
        _window.OnClose += () => _window = null;
        _window.OpenCentered();

        var pillCanisterRsi = new ResPath("_RMC14/Objects/Chemistry/pill_canister.rsi");
        var colors = Enum.GetValues<RMCPillBottleColors>();
        var colorCount = colors.Length - 1;

        for (var i = 0; i < colorCount; i++)
        {
            var state = spriteSystem.GetState(new SpriteSpecifier.Rsi(pillCanisterRsi, $"pill_canister{i}"));
            var button = new TextureButton
            {
                TextureNormal = state.Frame0,
                Scale = new Vector2(2, 2),
            };

            var color = colors[i];
            button.OnPressed += _ =>
            {
                SendPredictedMessage(new RMCChemMasterPillBottleColorMsg(color));
                _window?.Close();
            };

            _window.Grid.AddChild(button);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Close();
    }
}
