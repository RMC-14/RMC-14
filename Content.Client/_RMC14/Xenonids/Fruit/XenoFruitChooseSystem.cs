using Content.Shared._RMC14.Xenonids.Fruit;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.Fruit;

public sealed class XenoFruitChooseSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoFruitPlanterComponent, AfterAutoHandleStateEvent>(OnXenoFruitAfterState);
    }

    private void OnXenoFruitAfterState(Entity<XenoFruitPlanterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is XenoFruitChooseBui chooseUi)
                    chooseUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(XenoFruitChooseBui)}\n{e}");
        }
    }
}
