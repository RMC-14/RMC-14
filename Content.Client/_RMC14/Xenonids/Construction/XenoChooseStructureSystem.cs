using Content.Shared._RMC14.Xenonids.Construction;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.Construction;

public sealed class XenoChooseStructureSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoConstructionComponent, AfterAutoHandleStateEvent>(OnXenoConstructionAfterState);
        SubscribeLocalEvent<QueenBuildingBoostComponent, ComponentStartup>(OnBoostAdded);
        SubscribeLocalEvent<QueenBuildingBoostComponent, ComponentRemove>(OnBoostRemoved);
    }

    private void OnBoostAdded(Entity<QueenBuildingBoostComponent> ent, ref ComponentStartup args)
    {
        RefreshUI(ent.Owner);
    }

    private void OnBoostRemoved(Entity<QueenBuildingBoostComponent> ent, ref ComponentRemove args)
    {
        RefreshUI(ent.Owner);
    }

    private void RefreshUI(EntityUid entity)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp(entity, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is XenoChooseStructureBui chooseUi)
            {
                chooseUi.Close();
            }
        }
    }

    private void OnXenoConstructionAfterState(Entity<XenoConstructionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp(ent, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is XenoChooseStructureBui chooseUi)
                chooseUi.Refresh();
        }
    }
}
