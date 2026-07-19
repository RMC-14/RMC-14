using Content.Shared._RMC14.Machines.CoffeeMachine;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Machines.CoffeeMachine;

public sealed class RMCCoffeeMachineVisualizerSystem : VisualizerSystem<RMCCoffeeMachineComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RMCCoffeeMachineComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, RMCCoffeeMachineVisuals.HasCup, out bool hasCup, args.Component))
            hasCup = false;

        if (!AppearanceSystem.TryGetData(uid, RMCCoffeeMachineVisuals.CupType, out RMCCoffeeMachineCupType cupType, args.Component))
            cupType = RMCCoffeeMachineCupType.Generic;

        args.Sprite.LayerSetVisible(CoffeeMachineLayers.Cup, hasCup && cupType == RMCCoffeeMachineCupType.Generic);
        args.Sprite.LayerSetVisible(CoffeeMachineLayers.CoffeeMug, hasCup && cupType == RMCCoffeeMachineCupType.CoffeeMug);
        args.Sprite.LayerSetVisible(CoffeeMachineLayers.WestonYamada, hasCup && cupType == RMCCoffeeMachineCupType.WestonYamada);
        args.Sprite.LayerSetVisible(CoffeeMachineLayers.UNMC, hasCup && cupType == RMCCoffeeMachineCupType.UNMC);
        args.Sprite.LayerSetVisible(CoffeeMachineLayers.UnitedNations, hasCup && cupType == RMCCoffeeMachineCupType.UnitedNations);
        args.Sprite.LayerSetVisible(CoffeeMachineLayers.SocialistPP, hasCup && cupType == RMCCoffeeMachineCupType.SocialistPP);
        args.Sprite.LayerSetVisible(CoffeeMachineLayers.ThreeSunEmpire, hasCup && cupType == RMCCoffeeMachineCupType.ThreeSunEmpire);
        args.Sprite.LayerSetVisible(CoffeeMachineLayers.ColonyLiberationFront, hasCup && cupType == RMCCoffeeMachineCupType.ColonyLiberationFront);
    }
}

public enum CoffeeMachineLayers : byte
{
    Cup,
    CoffeeMug,
    WestonYamada,
    UNMC,
    UnitedNations,
    SocialistPP,
    ThreeSunEmpire,
    ColonyLiberationFront,
}
