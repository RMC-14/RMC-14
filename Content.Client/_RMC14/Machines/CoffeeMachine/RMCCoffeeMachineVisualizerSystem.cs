using Content.Shared._RMC14.Machines.CoffeeMachine;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Machines.CoffeeMachine;

public sealed class RMCCoffeeMachineVisualizerSystem : VisualizerSystem<RMCCoffeeMachineComponent>
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    protected override void OnAppearanceChange(EntityUid uid, RMCCoffeeMachineComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, RMCCoffeeMachineVisuals.HasCup, out bool hasCup, args.Component))
            hasCup = false;

        var entity = new Entity<SpriteComponent?>(uid, args.Sprite);
        _spriteSystem.LayerSetVisible(entity, CoffeeMachineLayers.Cup, hasCup);
    }
}

public enum CoffeeMachineLayers : byte
{
    Cup,
}
