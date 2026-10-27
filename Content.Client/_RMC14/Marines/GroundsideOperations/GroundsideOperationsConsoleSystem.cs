using Content.Shared._RMC14.Marines.GroundsideOperations;
using Content.Shared.UserInterface;
using Robust.Client.UserInterface;
using Robust.Shared.GameStates;

namespace Content.Client._RMC14.Marines.GroundsideOperations;

public sealed class GroundsideOperationsConsoleSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<GroundsideOperationsConsoleComponent, AfterAutoHandleStateEvent>(OnOwnerState);
    }

    private void OnOwnerState(Entity<GroundsideOperationsConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh(ent.Owner);
    }

    private void Refresh(EntityUid owner)
    {
        try
        {
            if (!TryComp(owner, out UserInterfaceComponent? ui))
                return;

            Refresh((owner, ui));
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(GroundsideOperationsConsoleBui)}\n{e}");
        }
    }

    private static void Refresh(Entity<UserInterfaceComponent> ent)
    {
        foreach (var bui in ent.Comp.ClientOpenInterfaces.Values)
        {
            if (bui is GroundsideOperationsConsoleBui groundside)
                groundside.Refresh();
        }
    }
}
