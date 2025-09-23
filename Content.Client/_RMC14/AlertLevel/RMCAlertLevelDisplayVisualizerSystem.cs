using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared.Clock;
using Content.Shared.GameTicking;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.AlertLevel;

public sealed class RMCAlertLevelDisplayVisualizerSystem : EntitySystem
{

    [Dependency] private readonly SharedGameTicker _ticker = default!;
    [Dependency] private readonly RMCAlertLevelSystem _alertLevel = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var current = _alertLevel.Get();

        if (current > RMCAlertLevels.Green)
            return;

        var query = EntityQueryEnumerator<RMCAlertLevelDisplayComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            if (!sprite.LayerMapTryGet(RMCAlertLevelDisplayVisualLayers.HourTens, out var hourTensLayer) ||
                !sprite.LayerMapTryGet(RMCAlertLevelDisplayVisualLayers.HourOnes, out var hourOnesLayer) ||
                !sprite.LayerMapTryGet(RMCAlertLevelDisplayVisualLayers.Separator, out var separatorLayer) ||
                !sprite.LayerMapTryGet(RMCAlertLevelDisplayVisualLayers.MinuteTens, out var minuteTensLayer) ||
                !sprite.LayerMapTryGet(RMCAlertLevelDisplayVisualLayers.MinuteOnes, out var minuteOnesLayer)
                )
                continue;

            var worldTime = (EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault()?.TimeOffset ?? TimeSpan.Zero) + _ticker.RoundDuration();
            var timeString = worldTime.ToString(@"hh\:mm");
            var hourTensState = $"{timeString[0]}";
            var hourOnesState = $"{timeString[1]}";
            var separatorState = "~";
            var minuteTensState = $"{timeString[3]}";
            var minuteOnesState = $"{timeString[4]}";

            sprite.LayerSetOffset(hourTensLayer, new Vector2(0.11f, -0.4375f));
            sprite.LayerSetOffset(hourOnesLayer, new Vector2(0.28f, -0.4375f));
            sprite.LayerSetOffset(separatorLayer, new Vector2(0.406f, -0.4375f));
            sprite.LayerSetOffset(minuteTensLayer, new Vector2(0.56f, -0.4375f));
            sprite.LayerSetOffset(minuteOnesLayer, new Vector2(0.73f, -0.4375f));

            sprite.LayerSetState(hourTensLayer, hourTensState);
            sprite.LayerSetState(hourOnesLayer, hourOnesState);
            sprite.LayerSetState(separatorLayer, separatorState);
            sprite.LayerSetState(minuteTensLayer, minuteTensState);
            sprite.LayerSetState(minuteOnesLayer, minuteOnesState);
        }
    }
}
