using Robust.Shared.Map.Components;

namespace Content.Shared._RMC14.Light;

public sealed class RMCLightAnimationSystem : EntitySystem
{

    /*
     * SetColor(colorHex, duration): animate from CurrentColor to ColorHex over Duration
     * SetColor(List<ColorHex>, duration): animate through all ColorHexes over Duration
     *
     */

    public void SetColor(Entity<RMCLightAnimationComponent> ent, string colorHex, TimeSpan duration)
    {
        var mapLight = EnsureComp<MapLightComponent>(ent);

        ent.Comp.ColorHexes = new List<string>(mapLight.AmbientLightColor)
        mapLight.AmbientLightColor = ent.Comp.ColorHexes;
        Dirty(ent.Owner, mapLight);
    }



}
