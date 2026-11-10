using Content.Client._RMC14.UserInterface.Systems.TargetDoll.Widgets;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._RMC14.Body.Zones;
using Content.Shared._RMC14.Xenonids;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.UserInterface.Systems.TargetDoll;

public sealed class TargetDollUIController : UIController
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    [UISystemDependency] private readonly SpriteSystem? _sprite = default;
    [UISystemDependency] private readonly RMCTargetZoneSystem? _targetZone = default;

    private static readonly ResPath Rsi = new("/Textures/_RMC14/Interface/target_doll.rsi");

    private EntityUid? _lastEntity;
    private RMCTargetZone? _lastZone;

    private TargetDollGui? UI => UIManager.GetActiveUIWidgetOrNull<TargetDollGui>();

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
        if (UI is { } ui)
            ui.ZonePressed += OnZonePressed;
    }

    private void OnScreenUnload()
    {
        if (UI is { } ui)
            ui.ZonePressed -= OnZonePressed;
    }

    private void OnZonePressed(RMCTargetZone zone)
    {
        _targetZone?.RequestZone(zone);
    }

    // the zone can change from a key press, a click, or the server correcting a prediction,
    // so the doll just follows the component each frame and only redraws when something differs
    public override void FrameUpdate(FrameEventArgs args)
    {
        if (UI is not { } ui || _sprite == null)
            return;

        if (_player.LocalEntity is not { } user ||
            !_entity.TryGetComponent(user, out RMCTargetZoneComponent? zone))
        {
            ui.Visible = false;
            _lastEntity = null;
            _lastZone = null;
            return;
        }

        if (_lastEntity == user && _lastZone == zone.Selected && ui.Visible)
            return;

        var doll = _entity.HasComponent<XenoComponent>(user) ? "doll_xeno" : "doll";
        ui.Doll.Texture = _sprite.Frame0(new SpriteSpecifier.Rsi(Rsi, doll));
        ui.Zone.Texture = _sprite.Frame0(new SpriteSpecifier.Rsi(Rsi, StateName(zone.Selected)));
        ui.Visible = true;

        _lastEntity = user;
        _lastZone = zone.Selected;
    }

    private static string StateName(RMCTargetZone zone)
    {
        return zone switch
        {
            RMCTargetZone.Head => "head",
            RMCTargetZone.Eyes => "eyes",
            RMCTargetZone.Mouth => "mouth",
            RMCTargetZone.Abdomen => "abdomen",
            RMCTargetZone.LeftArm => "left_arm",
            RMCTargetZone.RightArm => "right_arm",
            RMCTargetZone.LeftHand => "left_hand",
            RMCTargetZone.RightHand => "right_hand",
            RMCTargetZone.LeftLeg => "left_leg",
            RMCTargetZone.RightLeg => "right_leg",
            RMCTargetZone.LeftFoot => "left_foot",
            RMCTargetZone.RightFoot => "right_foot",
            _ => "chest",
        };
    }
}
