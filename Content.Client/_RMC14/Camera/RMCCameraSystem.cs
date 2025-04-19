using Content.Shared._RMC14.Camera;

namespace Content.Client._RMC14.Camera;

public sealed class RMCCameraSystem : SharedRMCCameraSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCCameraComputerComponent, AfterAutoHandleStateEvent>(OnComputerState);
    }

    private void OnComputerState(Entity<RMCCameraComputerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is RMCCameraBui cameraBui)
                    cameraBui.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(RMCCameraBui)}\n{e}");
        }
    }
}
