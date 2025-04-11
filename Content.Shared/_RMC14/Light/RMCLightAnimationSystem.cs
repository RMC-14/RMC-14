using System.Linq;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Light;

public sealed class RMCLightAnimationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public void SetColor(Entity<RMCLightAnimationComponent> ent, string colorHex, TimeSpan duration)
    {
        var mapLight = EnsureComp<MapLightComponent>(ent);

        ent.Comp.Colors = [mapLight.AmbientLightColor, Color.FromHex(colorHex, Color.Black)];
        ent.Comp.Duration = duration;
        ent.Comp.StartTime = _timing.CurTime;

        Dirty(ent);
    }

    public void SetColor(Entity<RMCLightAnimationComponent> ent, List<string> colorHexes, TimeSpan duration)
    {
        if (colorHexes.Count == 0 || duration <= TimeSpan.Zero)
            return;

        var mapLight = EnsureComp<MapLightComponent>(ent);

        ent.Comp.Colors = colorHexes.Select(hex => Color.FromHex(hex, Color.Black)).ToList();
        ent.Comp.Duration = duration;
        ent.Comp.StartTime = _timing.CurTime;

        Dirty(ent);
    }

    public Color GetColor(Entity<RMCLightAnimationComponent> ent, TimeSpan curTime)
    {
        //TODO: Doublecheck all this
        if (ent.Comp.Colors.Count == 0 || ent.Comp.Duration <= TimeSpan.Zero)
            return Color.Black;
        if (ent.Comp.Colors.Count == 1)
            return ent.Comp.Colors[0];

        var elapsedTime = curTime - ent.Comp.StartTime;
        var progress = Math.Clamp(elapsedTime / ent.Comp.Duration, 0.0, 1.0);

        var segmentCount = ent.Comp.Colors.Count - 1;
        var segmentLength = 1.0/segmentCount;
        var prevColorIndex = Math.Min((int)(progress / segmentLength), segmentCount - 1);
        var nextColorIndex = prevColorIndex + 1;

        var segmentProgress = Math.Clamp((progress - (prevColorIndex * segmentLength)) / segmentLength, 0.0, 1.0);

        var prevColor = ent.Comp.Colors[prevColorIndex];
        var nextColor = ent.Comp.Colors[nextColorIndex];

        var color = Color.InterpolateBetween(prevColor, nextColor, (float)segmentProgress);

        return color;
    }

    public void ClearComponent(Entity<RMCLightAnimationComponent> ent)
    {
        ent.Comp.Colors.Clear();
        ent.Comp.Duration = TimeSpan.Zero;
        ent.Comp.StartTime = _timing.CurTime;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var lightQuery = EntityQueryEnumerator<RMCLightAnimationComponent, MapLightComponent>();
        var curTime = _timing.CurTime;

        while (lightQuery.MoveNext(out var uid, out var animComponent, out var lightComponent))
        {
            if (curTime>= animComponent.EndTime)
            {
                lightComponent.AmbientLightColor = animComponent.Colors[^1];
                ClearComponent((uid, animComponent));
                continue;
            }

            var targetColor = GetColor((uid, animComponent), curTime);
            lightComponent.AmbientLightColor = targetColor;
            Dirty(uid, lightComponent);
        }
    }

}
