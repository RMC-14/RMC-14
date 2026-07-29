using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Warps;

namespace Content.Server.Warps;

public sealed class WarpPointSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarpPointComponent, ExaminedEvent>(OnWarpPointExamine);
    }

    private void OnWarpPointExamine(EntityUid uid, WarpPointComponent component, ExaminedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner))
            return;

        var loc = component.Location == null ? "<null>" : $"'{component.Location}'";
        args.PushText(Loc.GetString("warp-point-component-on-examine-success", ("location", loc)));
    }

    public void SetLocation(Entity<WarpPointComponent?> entity, string? location)
    {
        if (!Resolve(entity, ref entity.Comp, false) ||
            entity.Comp.Location == location)
        {
            return;
        }

        var oldLocation = entity.Comp.Location;
        entity.Comp.Location = location;

        var ev = new WarpPointLocationChangedEvent(oldLocation, location);
        RaiseLocalEvent(entity, ev);
    }
}
