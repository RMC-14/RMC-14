using System.Linq;
using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Client.UserInterface.Systems.Viewport;
using Content.Shared._RMC14.MotionDetector;
using Content.Shared.CCVar;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.MotionDetector;

public sealed class MotionDetectorOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private TimeSpan _last;
    private readonly List<Vector2> _blips = new();

    private readonly SlotFlags DetectSlots = SlotFlags.SUITSTORAGE | SlotFlags.BELT;

    public MotionDetectorOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player)
            return;

        var handle = args.WorldHandle;

        var sprite = _entity.System<SpriteSystem>();
        var frame = sprite.GetFrame(new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Objects/Tools/motion_detector.rsi"), "detector_blip"), _timing.CurTime);

        var transform = _entity.System<TransformSystem>();
        var playerCoords = transform.GetMapCoordinates(player);

        float vpHeight = ViewportUIController.ViewportHeight;
        float vpWidth = _config.GetCVar(CCVars.ViewportWidth);

        var eye = _eye.CurrentEye;
        var vpSize =eye.Zoom;
        if (eye.Rotation.GetCardinalDir() is Direction.East or Direction.West)
        {
            (vpWidth, vpHeight) = (vpHeight, vpWidth);
        }

        var hands = _entity.System<HandsSystem>();
        var inventory = _entity.System<InventorySystem>();
        var time = _timing.CurTime;

        List<EntityUid> ents = hands.EnumerateHeld(player).ToList();

        if(inventory.TryGetContainerSlotEnumerator(player, out var inv, DetectSlots))
        {
            while (inv.NextItem(out var item))
                ents.Add(item);
        }

        foreach (var held in ents)
        {
            if (!_entity.TryGetComponent(held, out MotionDetectorComponent? detector))
                continue;

            var duration = detector.ScanDuration;
            if (_net.ServerChannel is { } channel)
                duration += TimeSpan.FromMilliseconds(channel.Ping / 2f);

            if (time > detector.LastScan + duration)
                continue;

            if (_last != detector.LastScan)
            {
                _last = detector.LastScan;
                _blips.Clear();

                foreach (var coordinates in detector.Blips)
                {
                    if (playerCoords.MapId != coordinates.MapId)
                        continue;

                    vpWidth *= vpSize.X;
                    vpHeight *= vpSize.Y;
                    var diff = coordinates.Position - new Vector2(0.5f, 0.5f) - playerCoords.Position;
                    Cap(ref diff.X, vpWidth);
                    Cap(ref diff.Y, vpHeight);
                    _blips.Add(diff);
                }
            }

            foreach (var diff in _blips)
            {
                handle.DrawTexture(frame, playerCoords.Position + diff);
            }
        }
    }

    private void Cap(ref float i, float size)
    {
        var max = size / 2f - 0.5f;
        if (i > max)
            i = max;
        else if (i < -max)
            i = -max;
    }
}
