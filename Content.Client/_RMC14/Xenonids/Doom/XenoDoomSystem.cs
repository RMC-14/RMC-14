using Content.Shared._RMC14.Xenonids.Doom;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Xenonids.Doom;
public sealed partial class XenoDoomSystem : SharedXenoDoomSystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    protected override void OnDoomedAdded(Entity<MobDoomedComponent> ent, ref ComponentStartup args)
    {
        _overlay.AddOverlay(new DoomOverlay());
    }

    protected override void OnDoomedRemoved(Entity<MobDoomedComponent> ent, ref ComponentShutdown args)
    {
        _overlay.RemoveOverlay<DoomOverlay>();
    }
}
