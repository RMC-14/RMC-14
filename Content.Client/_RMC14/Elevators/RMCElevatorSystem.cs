using Content.Shared._RMC14.Elevators;
using Content.Shared.Interaction;
using Robust.Client.ViewVariables;

namespace Content.Client._RMC14.Elevators;

public sealed partial class RMCElevatorSystem : SharedRMCElevatorSystem
{
    [Dependency] private IClientViewVariablesManager _cvvm = default!;

    public readonly List<ElevatorPanelBui> Uis = new();

    public override void FrameUpdate(float frameTime)
    {
        foreach (var ui in Uis)
        {
            ui.Update();
        }
    }

    protected override void OnTryLink(Entity<RMCElevatorLinkingComponent> tool, ref AfterInteractEvent args)
    {
        base.OnTryLink(tool, ref args);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (HasComp<RMCElevatorDestinationComponent>(args.Target))
        {
            args.Handled = true;
            var net = GetNetEntity(args.Target);
            if (net != null)
                _cvvm.OpenVV(net);
        }
    }
}
