using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.EntitySystems;

public enum ClothingVisualResolution
{
    None,
    Species,
    Explicit,
    Default,
}

public abstract partial class ClothingSystem
{
    public static readonly IReadOnlyDictionary<string, string> EquippedStateSlotMap = new Dictionary<string, string>
    {
        {"head", "HELMET"},
        {"eyes", "EYES"},
        {"ears", "EARS"},
        {"ears2", "EARS"},
        {"mask", "MASK"},
        {"outerClothing", "OUTERCLOTHING"},
        {"jumpsuit", "INNERCLOTHING"},
        {"neck", "NECK"},
        {"back", "BACKPACK"},
        {"belt", "BELT"},
        {"gloves", "HAND"},
        {"shoes", "FEET"},
        {"id", "IDCARD"},
        {"pocket1", "POCKET1"},
        {"pocket2", "POCKET2"},
        {"suitstorage", "SUITSTORAGE"},
    };

    public static ClothingVisualResolution ResolveEquippedVisuals(
        ClothingComponent clothing,
        string slot,
        string? speciesId,
        string? fallbackRsiPath,
        Func<string, bool> hasState,
        [NotNullWhen(true)] out List<PrototypeLayerData>? layers)
    {
        layers = null;

        if (speciesId != null &&
            clothing.ClothingVisuals.TryGetValue($"{slot}-{speciesId}", out var speciesLayers))
        {
            layers = speciesLayers;
            return ClothingVisualResolution.Species;
        }

        if (clothing.ClothingVisuals.TryGetValue(slot, out var slotLayers))
        {
            layers = slotLayers;
            return ClothingVisualResolution.Explicit;
        }

        if (string.IsNullOrWhiteSpace(fallbackRsiPath) ||
            !TryCreateDefaultEquippedVisual(clothing, slot, speciesId, fallbackRsiPath, hasState, out layers))
        {
            return ClothingVisualResolution.None;
        }

        return ClothingVisualResolution.Default;
    }

    public static string GetEquippedState(ClothingComponent clothing, string slot)
    {
        var correctedSlot = EquippedStateSlotMap.GetValueOrDefault(slot, slot);

        if (clothing.EquippedState != null)
            return clothing.EquippedState;

        if (!string.IsNullOrEmpty(clothing.EquippedPrefix))
            return $"{clothing.EquippedPrefix}-equipped-{correctedSlot}";

        return $"equipped-{correctedSlot}";
    }

    private static bool TryCreateDefaultEquippedVisual(
        ClothingComponent clothing,
        string slot,
        string? speciesId,
        string fallbackRsiPath,
        Func<string, bool> hasState,
        [NotNullWhen(true)] out List<PrototypeLayerData>? layers)
    {
        layers = null;

        var state = GetEquippedState(clothing, slot);

        if (speciesId != null && hasState($"{state}-{speciesId}"))
            state = $"{state}-{speciesId}";
        else if (!hasState(state))
            return false;

        layers = new()
        {
            new PrototypeLayerData
            {
                RsiPath = fallbackRsiPath,
                State = state,
            },
        };

        return true;
    }
}
