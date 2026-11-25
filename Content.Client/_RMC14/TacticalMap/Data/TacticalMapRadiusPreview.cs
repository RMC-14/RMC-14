using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Mortar;
using Content.Shared._RMC14.OrbitalCannon;
using Content.Shared.Explosion.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.TacticalMap;

public static class TacticalMapRadiusPreview
{
    private static readonly EntProtoId OrbitalExplosion = "RMCOrbitalCannonExplosionExplosive";
    private static readonly EntProtoId MortarShell = "RMCMortarShellHE";
    private static readonly EntProtoId MortarKit = "RMCMortarKit";
    private static readonly EntProtoId HiveCore = "HiveCoreXeno";
    private static readonly EntProtoId HivePylon = "HivePylonXeno";

    public static float GetOrbitalRadius(IPrototypeManager prototypes, IComponentFactory factory, float fallback)
    {
        if (!prototypes.TryIndex(OrbitalExplosion, out var proto) ||
            !proto.TryGetComponent(out OrbitalCannonExplosionComponent? explosion, factory))
        {
            return fallback;
        }

        var radius = 0f;
        foreach (var step in explosion.Steps)
        {
            if (step.Type == null || step.Total <= 0f || step.Slope <= 0f)
                continue;

            radius = MathF.Max(radius, IntensityToRadius(step.Total, step.Slope, step.Max));
        }

        return radius > 0f ? radius : fallback;
    }

    public static float GetMortarRadius(IPrototypeManager prototypes, IComponentFactory factory, float fallback)
    {
        if (!prototypes.TryIndex(MortarShell, out var proto) ||
            !proto.TryGetComponent(out ExplosiveComponent? explosive, factory) ||
            explosive.TotalIntensity <= 0f ||
            explosive.IntensitySlope <= 0f)
        {
            return fallback;
        }

        var radius = IntensityToRadius(explosive.TotalIntensity, explosive.IntensitySlope, explosive.MaxIntensity);
        return radius > 0f ? radius : fallback;
    }

    public static (int Min, int Max) GetMortarRange(IPrototypeManager prototypes, IComponentFactory factory, (int Min, int Max) fallback)
    {
        if (!prototypes.TryIndex(MortarKit, out var proto) ||
            !proto.TryGetComponent(out MortarComponent? mortar, factory))
        {
            return fallback;
        }

        var min = Math.Max(0, mortar.MinimumRange);
        var max = Math.Max(min, mortar.MaximumRange);
        return max > 0 ? (min, max) : fallback;
    }

    public static float GetHiveCoreRange(IPrototypeManager prototypes, IComponentFactory factory, float fallback)
    {
        return GetRoofRange(prototypes, factory, HiveCore, fallback);
    }

    public static float GetHivePylonRange(IPrototypeManager prototypes, IComponentFactory factory, float fallback)
    {
        return GetRoofRange(prototypes, factory, HivePylon, fallback);
    }

    private static float GetRoofRange(IPrototypeManager prototypes, IComponentFactory factory, EntProtoId id, float fallback)
    {
        if (!prototypes.TryIndex(id, out var proto) ||
            !proto.TryGetComponent(out RoofingEntityComponent? roofing, factory) ||
            roofing.Range <= 0f)
        {
            return fallback;
        }

        return roofing.Range;
    }

    private static float IntensityToRadius(float totalIntensity, float slope, float maxIntensity)
    {
        if (totalIntensity <= 0f || slope <= 0f)
            return 0f;

        if (maxIntensity <= 0f)
            return MathF.Cbrt(3f * totalIntensity / (slope * MathF.PI));

        var r0 = maxIntensity / slope;
        var v0 = RadiusToIntensity(r0, slope, maxIntensity);
        if (totalIntensity <= v0)
            return MathF.Cbrt(3f * totalIntensity / (slope * MathF.PI));

        return r0 * (MathF.Sqrt(12f * totalIntensity / v0 - 3f) / 6f + 0.5f);
    }

    private static float RadiusToIntensity(float radius, float slope, float maxIntensity)
    {
        var cone = slope * MathF.PI / 3f * MathF.Pow(radius, 3f);
        if (maxIntensity <= 0f || slope * radius < maxIntensity)
            return cone;

        var h = slope * radius - maxIntensity;
        return cone - h * MathF.PI / 3f * MathF.Pow(h / slope, 2f);
    }
}
