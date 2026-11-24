using System.Linq;
using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Client.UserInterface.Systems.Viewport;
using Content.Shared._RMC14.MotionDetector;
using Content.Shared.CCVar;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.MotionDetector;

public sealed class MotionDetectorOverlaySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        if (!_overlay.HasOverlay<MotionDetectorOverlay>())
            _overlay.AddOverlay(new MotionDetectorOverlay());
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<MotionDetectorOverlay>();
    }

    public void DrawBlips<T>(DrawingHandleWorld handle, ref TimeSpan last, List<(Vector2 Pos, bool QueenEye)> blips, Texture texture, Texture queenEyeTexture) where T : IComponent, IDetectorComponent
    {
        if (_player.LocalEntity is not { } player)
            return;

        var transform = _entity.System<TransformSystem>();
        var playerCoords = transform.GetMapCoordinates(player);

        float vpHeight = ViewportUIController.ViewportHeight;
        float vpWidth = _config.GetCVar(CCVars.ViewportWidth);

        var eye = _eye.CurrentEye;
        var vpSize = eye.Zoom;
        if (eye.Rotation.GetCardinalDir() is Direction.East or Direction.West)
        {
            (vpWidth, vpHeight) = (vpHeight, vpWidth);
        }

        var hands = _entity.System<HandsSystem>();
        var inventory = _entity.System<InventorySystem>();
        var time = _timing.CurTime;

        var ents = hands.EnumerateHeld(player).ToList();
        if (inventory.TryGetContainerSlotEnumerator(player, out var inv))
        {
            while (inv.NextItem(out var item))
            {
                ents.Add(item);

                if (_entity.HasComponent<PropagateDetectorsComponent>(item) &&
                    _entity.TryGetComponent<StorageComponent>(item, out var itemStorage))
                    foreach (var deepItem in itemStorage.StoredItems.Keys)
                        ents.Add(deepItem);
            }
        }

        foreach (var held in ents)
        {
            if (!_entity.TryGetComponent(held, out T? detector))
                continue;

            var duration = detector.ScanDuration;
            if (_net.ServerChannel is { } channel)
                duration += TimeSpan.FromMilliseconds(channel.Ping / 2f);

            if (time > detector.LastScan + duration)
                continue;

            if (last != detector.LastScan)
            {
                last = detector.LastScan;
                blips.Clear();

                foreach (var blip in detector.Blips)
                {
                    if (playerCoords.MapId != blip.Coordinates.MapId)
                        continue;

                    vpWidth *= vpSize.X;
                    vpHeight *= vpSize.Y;
                    var diff = blip.Coordinates.Position - new Vector2(0.5f, 0.5f) - playerCoords.Position;
                    Cap(ref diff.X, vpWidth);
                    Cap(ref diff.Y, vpHeight);
                    blips.Add((diff, blip.QueenEye));
                }
            }

            foreach (var diff in blips)
            {
                handle.DrawTexture(diff.QueenEye ? queenEyeTexture : texture, playerCoords.Position + diff.Pos);
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
