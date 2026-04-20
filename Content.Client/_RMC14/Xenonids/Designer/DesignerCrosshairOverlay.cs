using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;

namespace Content.Client._RMC14.Xenonids.Designer;

public sealed class DesignerCrosshairOverlay : Overlay
{
    private readonly IInputManager _input;
    private readonly IEntityManager _entMan;
    private readonly IEyeManager _eye;
    private readonly IPlayerManager _player;
    private readonly DesignerCrosshairSystem _designerCrosshair;
    private readonly SpriteSystem _sprite;

    private const float IconScale = 1.4f;
    private static readonly Vector2 Offset = new(28f, 0f);

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public DesignerCrosshairOverlay(
        IInputManager input,
        IEntityManager entMan,
        IEyeManager eye,
        IPlayerManager player)
    {
        _input = input;
        _entMan = entMan;
        _eye = eye;
        _player = player;
        _designerCrosshair = entMan.System<DesignerCrosshairSystem>();
        _sprite = entMan.System<SpriteSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player)
            return false;

        return _designerCrosshair.GetDesignerCrosshair(player) != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player)
            return;

        if (_designerCrosshair.GetDesignerCrosshair(player) is not { } crosshair)
            return;

        var mouseScreenPosition = _input.MouseScreenPosition;
        var mousePosMap = _eye.PixelToMap(mouseScreenPosition);
        if (mousePosMap.MapId != args.MapId)
            return;

        var mousePos = mouseScreenPosition.Position;
        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var limitedScale = uiScale > 1.25f ? 1.25f : uiScale;

        var sight = _sprite.Frame0(crosshair);
        var sightSize = sight.Size * limitedScale * IconScale;
        var rect = UIBox2.FromDimensions(mousePos - sightSize * 0.5f + Offset, sightSize);
        args.ScreenHandle.DrawTextureRect(sight, rect);
    }
}
