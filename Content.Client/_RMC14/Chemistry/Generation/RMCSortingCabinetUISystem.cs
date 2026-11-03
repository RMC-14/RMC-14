using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Chemistry.Generation;

public sealed class RMCSortingCabinetUISystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSortingCabinetComponent, AfterAutoHandleStateEvent>(OnCabinetState);
        SubscribeLocalEvent<RMCSortingCabinetComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RMCSortingCabinetComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<RMCChemResearchReportComponent, AfterAutoHandleStateEvent>(OnReportState);
    }

    private void OnCabinetState(Entity<RMCSortingCabinetComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<RMCSortingCabinetBui>(ent.Owner);
    }

    private void OnInserted(Entity<RMCSortingCabinetComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        _rmcUI.RefreshUIs<RMCSortingCabinetBui>(ent.Owner);
    }

    private void OnRemoved(Entity<RMCSortingCabinetComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        _rmcUI.RefreshUIs<RMCSortingCabinetBui>(ent.Owner);
    }

    private void OnReportState(Entity<RMCChemResearchReportComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            HasComp<RMCSortingCabinetComponent>(container.Owner))
        {
            _rmcUI.RefreshUIs<RMCSortingCabinetBui>(container.Owner);
        }
    }
}
