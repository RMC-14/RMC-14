using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Xenonids.Announce;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class TacticalMapAnnounceSystem : EntitySystem
{
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapMarineAnnounceComponent, TacticalMapCanvasSubmittedEvent>(OnMarineSubmitted);
        SubscribeLocalEvent<TacticalMapXenoAnnounceComponent, TacticalMapCanvasSubmittedEvent>(OnXenoSubmitted);
    }

    private void OnMarineSubmitted(Entity<TacticalMapMarineAnnounceComponent> ent, ref TacticalMapCanvasSubmittedEvent args)
    {
        RMCAnnouncementOverlay? overlay = ent.Comp.Overlay is { } preset
            ? new RMCAnnouncementOverlay(preset, ent.Comp.Message)
            : null;

        _marineAnnounce.AnnounceARESStaging(args.Actor, ent.Comp.Message, overlay: overlay);
    }

    private void OnXenoSubmitted(Entity<TacticalMapXenoAnnounceComponent> ent, ref TacticalMapCanvasSubmittedEvent args)
    {
        _xenoAnnounce.AnnounceSameHive(args.Actor, ent.Comp.Message, ent.Comp.Sound);
    }
}
