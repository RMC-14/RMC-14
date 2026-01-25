using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.UserInterface;
using Robust.Client.Timing;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Chemistry.Master;

public sealed class RMCChemMasterSystem : SharedRMCChemMasterSystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCChemMasterComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnState(Entity<RMCChemMasterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_timing.CurTick != _timing.LastRealTick)
            return;

        RefreshUIs(ent);
    }

    protected override void OnEntInsertedIntoContainer(Entity<RMCChemMasterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        base.OnEntInsertedIntoContainer(ent, ref args);
        RefreshUIs(ent);
    }

    protected override void OnEntRemovedFromContainer(Entity<RMCChemMasterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        base.OnEntRemovedFromContainer(ent, ref args);
        RefreshUIs(ent);
    }

    protected override void RefreshUIs(Entity<RMCChemMasterComponent> ent)
    {
        _rmcUI.RefreshUIs<RMCChemMasterBui>(ent.Owner);
    }
}
