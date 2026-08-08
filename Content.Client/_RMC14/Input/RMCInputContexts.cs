using Content.Shared._RMC14.Input;
using Content.Shared.Input;
using Robust.Shared.Input;

namespace Content.Client._RMC14.Input;

public static class RMCInputContexts
{
    public static readonly IReadOnlyList<BoundKeyFunction> HumanFunctions =
    [
        CMKeyFunctions.RMCActivateAttachableBarrel,
        CMKeyFunctions.RMCActivateAttachableRail,
        CMKeyFunctions.RMCActivateAttachableStock,
        CMKeyFunctions.RMCActivateAttachableUnderbarrel,
        CMKeyFunctions.CMHolsterPrimary,
        CMKeyFunctions.CMHolsterQuaternary,
        CMKeyFunctions.CMHolsterSecondary,
        CMKeyFunctions.CMHolsterTertiary,
        CMKeyFunctions.CMUniqueAction,
        CMKeyFunctions.RMCCycleFireMode,
        CMKeyFunctions.RMCFieldStripHeldItem,
        CMKeyFunctions.RMCInteractWithOtherHand,
        CMKeyFunctions.RMCMarineCycleHelmetHud,
        CMKeyFunctions.RMCMarineIssueOrder,
        CMKeyFunctions.RMCMarineIssueOrderFocus,
        CMKeyFunctions.RMCMarineIssueOrderHold,
        CMKeyFunctions.RMCMarineIssueOrderMove,
        CMKeyFunctions.RMCMarineSpecialistOne,
        CMKeyFunctions.RMCMarineSpecialistTwo,
        CMKeyFunctions.RMCPickUpDroppedItems,
        CMKeyFunctions.RMCQuickEquipInventory,
        CMKeyFunctions.RMCRest,
        CMKeyFunctions.RMCResist,
        CMKeyFunctions.RMCToggleAutoEject,
        CMKeyFunctions.RMCToggleIff,
        CMKeyFunctions.RMCUnloadWeapon,
    ];

    public static readonly IReadOnlyList<BoundKeyFunction> XenoFunctions =
    [
        CMKeyFunctions.CMXenoWideSwing,
        CMKeyFunctions.RMCResist,
        CMKeyFunctions.RMCXenoCorrosiveAcid,
        CMKeyFunctions.RMCXenoEvolve,
        CMKeyFunctions.RMCXenoHide,
        CMKeyFunctions.RMCXenoPheromones,
        CMKeyFunctions.RMCXenoPheromonesFrenzy,
        CMKeyFunctions.RMCXenoPheromonesRecovery,
        CMKeyFunctions.RMCXenoPheromonesWarding,
        CMKeyFunctions.RMCXenoPrimaryActionFive,
        CMKeyFunctions.RMCXenoPrimaryActionFour,
        CMKeyFunctions.RMCXenoPrimaryActionOne,
        CMKeyFunctions.RMCXenoPrimaryActionThree,
        CMKeyFunctions.RMCXenoPrimaryActionTwo,
        CMKeyFunctions.RMCXenoPurchaseStrain,
        CMKeyFunctions.RMCXenoRest,
        CMKeyFunctions.RMCXenoScreech,
        CMKeyFunctions.RMCXenoTailStab,
        CMKeyFunctions.RMCXenoWordQueen,
    ];

    public static readonly IReadOnlyList<BoundKeyFunction> XenoInteractionFunctions =
    [
        EngineKeyFunctions.MoveUp,
        EngineKeyFunctions.MoveDown,
        EngineKeyFunctions.MoveLeft,
        EngineKeyFunctions.MoveRight,
        EngineKeyFunctions.Walk,
        ContentKeyFunctions.SwapHands,
        ContentKeyFunctions.SwapHandsReverse,
        ContentKeyFunctions.Drop,
        ContentKeyFunctions.UseItemInHand,
        ContentKeyFunctions.AltUseItemInHand,
        ContentKeyFunctions.ActivateItemInWorld,
        ContentKeyFunctions.AltActivateItemInWorld,
        ContentKeyFunctions.ThrowItemInHand,
        ContentKeyFunctions.TryPullObject,
        ContentKeyFunctions.MovePulledObject,
        ContentKeyFunctions.ReleasePulledObject,
        ContentKeyFunctions.OpenEmotesMenu,
        ContentKeyFunctions.MouseMiddle,
    ];

    public static void Setup(IInputContextContainer contexts)
    {
        var human = contexts.New("rmc-human", "human");
        AddFunctions(human, HumanFunctions);

        var xenonid = contexts.New("xenonid", "common");
        AddFunctions(xenonid, XenoInteractionFunctions);
        AddFunctions(xenonid, XenoFunctions);
    }

    private static void AddFunctions(IInputCmdContext context, IEnumerable<BoundKeyFunction> functions)
    {
        foreach (var function in functions)
        {
            context.AddFunction(function);
        }
    }
}
