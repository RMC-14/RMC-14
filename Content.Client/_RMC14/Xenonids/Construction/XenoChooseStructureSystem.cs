using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Designer;
using Content.Shared._RMC14.Xenonids.Evolution;
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
        SubscribeLocalEvent<DesignerStrainComponent, AfterAutoHandleStateEvent>(OnDesignerAfterState);
        SubscribeLocalEvent<XenoComponent, AfterXenoChangedPrototypeEvent>(OnAfterXenoChangedPrototype);
    }

    private void OnXenoConstructionAfterState(Entity<XenoConstructionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshUI(ent);
    }


    private void OnBoostAdded(Entity<QueenBuildingBoostComponent> ent, ref ComponentStartup args)
    {
        RefreshUI(ent.Owner);
    }

    private void OnBoostRemoved(Entity<QueenBuildingBoostComponent> ent, ref ComponentRemove args)
    {
        RefreshUI(ent.Owner);
    }

    private void OnDesignerAfterState(Entity<DesignerStrainComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshUI(ent);
    }

    private void OnAfterXenoChangedPrototype(Entity<XenoComponent> ent, ref AfterXenoChangedPrototypeEvent args)
    {
        RefreshUI(ent);
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
                if (HasComp<XenoConstructionComponent>(entity))
                {
                    chooseUi.Refresh();
                }
                else
                {
                    chooseUi.Close();
                }
            }
        }
    }
}
