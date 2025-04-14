using System.Linq;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Light;

public sealed class SharedRMCAmbientLightSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;




    public Color GetColor(Entity<RMCAmbientLightComponent> ent, TimeSpan curTime)
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

    /*public void ClearComponent(Entity<RMCAmbientLightComponent> ent)
    {
        ent.Comp.Colors.Clear();
        ent.Comp.Duration = TimeSpan.Zero;
        ent.Comp.StartTime = _timing.CurTime;
        ent.Comp.Running = false;
        Dirty(ent);
    }*/

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var lightQuery = EntityQueryEnumerator<RMCAmbientLightComponent, MapLightComponent>();
        var curTime = _timing.CurTime;

        while (lightQuery.MoveNext(out var uid, out var animComponent, out var lightComponent))
        {
            if (!animComponent.Running)
                continue;

            if (curTime>= animComponent.EndTime)
            {
                lightComponent.AmbientLightColor = animComponent.Colors[^1];
                animComponent.Running = false;
                Dirty(uid, lightComponent);
                continue;
            }

            var targetColor = GetColor((uid, animComponent), curTime);
            lightComponent.AmbientLightColor = targetColor;
            Dirty(uid, lightComponent);
        }
    }

}
