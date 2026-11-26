using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Mortar;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.TacticalMap;

public static class TacticalMapRadiusPreview
{
    private static readonly EntProtoId MortarKit = "RMCMortarKit";
    private static readonly EntProtoId HiveCore = "HiveCoreXeno";
    private static readonly EntProtoId HivePylon = "HivePylonXeno";

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
}
