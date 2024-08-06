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

    // public override void Update(float frameTime)
    // {
    //     var query = EntityQueryEnumerator<RMCActiveCameraComponent>();
    //     while (query.MoveNext(out var uid, out var camera))
    //     {
    //         if (camera.Computer is not { } computer ||
    //             TerminatingOrDeleted(computer))
    //         {
    //             RemCompDeferred<RMCActiveCameraComponent>(uid);
    //             continue;
    //         }
    //
    //         if (!TryComp(computer, out UserInterfaceComponent? ui))
    //             return;
    //
    //         foreach (var bui in ui.ClientOpenInterfaces.Values)
    //         {
    //             if (bui is RMCCameraBui evolutionBui)
    //                 evolutionBui.RefreshCamera(uid);
    //         }
    //     }
    // }
}
