using Content.Client._RMC14.Xenonids.Biomass;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Biomass;

namespace Content.Client._RMC14.Chemistry.Generation;

public sealed class RMCChemistryResearchUiRefreshSystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCChemistryResearchComponent, AfterAutoHandleStateEvent>(OnResearchState);
        SubscribeLocalEvent<RMCSynthesisSimulatorComponent, AfterAutoHandleStateEvent>(OnSimulatorState);
    }

    private void OnResearchState(Entity<RMCChemistryResearchComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var computers = EntityQueryEnumerator<RMCResearchComputerComponent>();
        while (computers.MoveNext(out var uid, out _))
        {
            _rmcUI.RefreshUIs<RMCResearchComputerBui>(uid);
        }

        var simulators = EntityQueryEnumerator<RMCSynthesisSimulatorComponent>();
        while (simulators.MoveNext(out var uid, out _))
        {
            _rmcUI.RefreshUIs<RMCSynthesisSimulatorBui>(uid);
        }

        var analyzers = EntityQueryEnumerator<RMCBiomassAnalyzerMachineComponent>();
        while (analyzers.MoveNext(out var uid, out _))
        {
            _rmcUI.RefreshUIs<RMCBiomassAnalyzerBui>(uid);
        }
    }

    private void OnSimulatorState(Entity<RMCSynthesisSimulatorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<RMCSynthesisSimulatorBui>(ent.Owner);
    }
}
