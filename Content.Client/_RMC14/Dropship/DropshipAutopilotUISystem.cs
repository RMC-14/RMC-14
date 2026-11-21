using Content.Shared.UserInterface;
using Content.Shared._RMC14.Dropship;
using Robust.Shared.GameStates;

namespace Content.Client._RMC14.Dropship;

public sealed class DropshipAutopilotUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipNavigationComputerComponent, AfterAutoHandleStateEvent>(OnNavigationState);
        SubscribeLocalEvent<DropshipRemoteControlConsoleComponent, AfterAutoHandleStateEvent>(OnRemoteControlState);
    }

    private void OnNavigationState(Entity<DropshipNavigationComputerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh(ent.Owner);
    }

    private void OnRemoteControlState(Entity<DropshipRemoteControlConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh(ent.Owner);
    }

    private void Refresh(EntityUid owner)
    {
        try
        {
            if (!TryComp(owner, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                switch (bui)
                {
                    case DropshipNavigationBui navigation:
                        navigation.Refresh();
                        break;
                    case DropshipRemoteControlBui remote:
                        remote.Refresh();
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing dropship controls on {ToPrettyString(owner)}: {e}");
        }
    }
}
