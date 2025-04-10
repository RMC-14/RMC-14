using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Light;

public sealed class RMCLightAnimationSystem : EntitySystem
{

    /*
     * SetColor(colorHex, duration): animate from CurrentColor to ColorHex over Duration
     * SetColor(List<ColorHex>, duration): animate through all ColorHexes over Duration
     *
     */

    [Dependency] private readonly IGameTiming _timing = default!;

    public void SetColor(Entity<RMCLightAnimationComponent> ent, string colorHex, TimeSpan duration)
    {
        var mapLight = EnsureComp<MapLightComponent>(ent);

        ent.Comp.ColorHexes = [mapLight.AmbientLightColor.ToHex(), colorHex];
        ent.Comp.Duration = duration;
        ent.Comp.StartTime = _timing.CurTime;

        Dirty(ent);
        Dirty(ent.Owner, mapLight);
    }

    public Color GetColor(Entity<RMCLightAnimationComponent> ent)
    {
        //TODO: Doublecheck all this
        
        var currentTime = _timing.CurTime;
        var progress = (currentTime - ent.Comp.StartTime) / ent.Comp.Duration;

        var segmentCount = ent.Comp.ColorHexes.Count - 1;
        var segmentLength = 1.0/segmentCount;
        var prevColorIndex = (int)(progress / segmentLength);
        var nextColorIndex = prevColorIndex + 1;

        // Progress in current segment normalized
        var segmentProgress = (progress - (prevColorIndex * segmentLength)) / segmentLength;

        var prevColor = Color.FromHex(ent.Comp.ColorHexes[prevColorIndex]);
        var nextColor = Color.FromHex(ent.Comp.ColorHexes[nextColorIndex]);

        var color = new Color(
            (byte)Math.Min(255, prevColor.RByte + (nextColor.RByte - prevColor.RByte) * segmentProgress),
            (byte)Math.Min(255, prevColor.GByte + (nextColor.GByte - prevColor.GByte) * segmentProgress),
            (byte)Math.Min(255, prevColor.BByte + (nextColor.BByte - prevColor.BByte) * segmentProgress));

        return color;
    }



}
