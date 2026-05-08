using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Designer;
using Content.Shared._RMC14.Xenonids.Evolution;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.Construction;

public sealed class XenoConstructionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoConstructionComponent, AfterAutoHandleStateEvent>(OnXenoConstructionAfterState);
        SubscribeLocalEvent<QueenBuildingBoostComponent, ComponentStartup>(OnBoostAdded);
        SubscribeLocalEvent<QueenBuildingBoostComponent, ComponentRemove>(OnBoostRemoved);
        SubscribeLocalEvent<DesignerStrainComponent, AfterAutoHandleStateEvent>(OnDesignerAfterState);
        SubscribeLocalEvent<AfterXenoChangedPrototypeEvent>(OnAfterXenoChangedPrototype);
    }

    private void OnXenoConstructionAfterState(Entity<XenoConstructionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshUIs(ent);
    }

    private void OnBoostAdded(Entity<QueenBuildingBoostComponent> ent, ref ComponentStartup args)
    {
        RefreshUIs(ent.Owner);
    }

    private void OnBoostRemoved(Entity<QueenBuildingBoostComponent> ent, ref ComponentRemove args)
    {
        RefreshUIs(ent.Owner);
    }

    private void OnDesignerAfterState(Entity<DesignerStrainComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshUIs(ent);
    }

    private void OnAfterXenoChangedPrototype(ref AfterXenoChangedPrototypeEvent args)
    {
        RefreshUIs(args.Xeno);
    }

    private void RefreshUIs(EntityUid entity)
    {
        RefreshSecretionUI(entity);
        RefreshOrderConstructionUI(entity);
    }

    private void RefreshSecretionUI(EntityUid entity)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp(entity, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is XenoChooseStructureBui chooseUi)
            {
                if (TryComp<XenoConstructionComponent>(entity, out var comp) && comp.CanBuild.Count > 0)
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

    private void RefreshOrderConstructionUI(EntityUid entity)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp(entity, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is XenoOrderConstructionBui chooseUi)
            {
                if (TryComp<XenoConstructionComponent>(entity, out var comp) && comp.CanOrderConstruction.Count > 0)
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
