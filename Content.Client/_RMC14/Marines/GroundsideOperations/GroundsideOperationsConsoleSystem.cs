using Content.Shared._RMC14.Marines.GroundsideOperations;
using Content.Shared.UserInterface;
using Robust.Client.UserInterface;
using Robust.Shared.GameStates;

namespace Content.Client._RMC14.Marines.GroundsideOperations;

public sealed class GroundsideOperationsConsoleSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<GroundsideOperationsConsoleComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnState<T>(Entity<T> ent, ref AfterAutoHandleStateEvent args) where T : IComponent?
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is GroundsideOperationsConsoleBui groundside)
                    groundside.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(GroundsideOperationsConsoleBui)}\n{e}");
        }
    }
}
