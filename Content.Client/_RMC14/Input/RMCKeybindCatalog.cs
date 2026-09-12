using Content.Shared._RMC14.Input;
using Robust.Shared.Input;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Input;

public static class RMCKeybindCatalog
{
    public static readonly IReadOnlyList<RMCKeybindCategory> Categories =
    [
        new("ui-options-header-rmc-human",
        [
            CMKeyFunctions.RMCRest,
            CMKeyFunctions.RMCResist,
            CMKeyFunctions.RMCMarineIssueOrder,
            CMKeyFunctions.RMCMarineIssueOrderMove,
            CMKeyFunctions.RMCMarineIssueOrderHold,
            CMKeyFunctions.RMCMarineIssueOrderFocus,
            CMKeyFunctions.RMCMarineCycleHelmetHud,
            CMKeyFunctions.RMCToggleAutoEject,
            CMKeyFunctions.RMCToggleIff,
        ]),
        new("ui-options-header-rmc-combat",
        [
            CMKeyFunctions.RMCActivateAttachableBarrel,
            CMKeyFunctions.RMCActivateAttachableRail,
            CMKeyFunctions.RMCActivateAttachableStock,
            CMKeyFunctions.RMCActivateAttachableUnderbarrel,
            CMKeyFunctions.RMCFieldStripHeldItem,
            CMKeyFunctions.RMCCycleFireMode,
            CMKeyFunctions.CMUniqueAction,
            CMKeyFunctions.RMCMarineSpecialistOne,
            CMKeyFunctions.RMCMarineSpecialistTwo,
            CMKeyFunctions.RMCUnloadWeapon,
        ]),
        new("ui-options-header-rmc-inventory",
        [
            CMKeyFunctions.CMHolsterPrimary,
            CMKeyFunctions.CMHolsterSecondary,
            CMKeyFunctions.CMHolsterTertiary,
            CMKeyFunctions.CMHolsterQuaternary,
            CMKeyFunctions.RMCPickUpDroppedItems,
            CMKeyFunctions.RMCInteractWithOtherHand,
            CMKeyFunctions.RMCQuickEquipInventory,
        ]),
        new("ui-options-header-rmc-xeno",
        [
            CMKeyFunctions.CMXenoWideSwing,
            CMKeyFunctions.RMCXenoRest,
            CMKeyFunctions.RMCXenoPrimaryActionOne,
            CMKeyFunctions.RMCXenoPrimaryActionTwo,
            CMKeyFunctions.RMCXenoPrimaryActionThree,
            CMKeyFunctions.RMCXenoPrimaryActionFour,
            CMKeyFunctions.RMCXenoPrimaryActionFive,
            CMKeyFunctions.RMCXenoPheromones,
            CMKeyFunctions.RMCXenoPheromonesFrenzy,
            CMKeyFunctions.RMCXenoPheromonesWarding,
            CMKeyFunctions.RMCXenoPheromonesRecovery,
            CMKeyFunctions.RMCXenoCorrosiveAcid,
            CMKeyFunctions.RMCXenoScreech,
            CMKeyFunctions.RMCXenoWordQueen,
            CMKeyFunctions.RMCXenoTailStab,
            CMKeyFunctions.RMCXenoHide,
            CMKeyFunctions.RMCXenoEvolve,
            CMKeyFunctions.RMCXenoPurchaseStrain,
        ]),
    ];

    public static string GetDescription(BoundKeyFunction function)
    {
        return $"ui-options-description-{CaseConversion.PascalToKebab(function.FunctionName)}";
    }
}

public sealed record RMCKeybindCategory(string Header, IReadOnlyList<BoundKeyFunction> Functions);
