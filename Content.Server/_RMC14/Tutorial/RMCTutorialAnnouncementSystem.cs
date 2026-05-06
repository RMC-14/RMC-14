using System.Linq;
using Content.Server._RMC14.Marines;
using Content.Shared._RMC14.Bioscan;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared.NPC.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Tutorial;

public sealed partial class RMCTutorialAnnouncementSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounceSystem = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCTutorialAnnouncementComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<RMCTutorialAnnouncementComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<RMCTutorialAnnouncementComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.RemoveComponents != null)
            EntityManager.RemoveComponents(ent.Owner, ent.Comp.RemoveComponents);
    }

    private void OnCollide(EntityUid uid, RMCTutorialAnnouncementComponent component, ref StartCollideEvent args)
    {
        // Prevent repeat triggering.
        if (component.HasTriggered)
            return;
        var subject = args.OtherEntity;
        // Ensure entity has a faction.
        if (!TryComp<NpcFactionMemberComponent>(subject, out var factionComp))
            return;
        // Ensures triggering entity is a member of the wanted faction
        if (!factionComp.Factions.Any(faction => component.Factions.Contains(faction)))
            return;

        // Marine style signed announcement
        if (TryComp<MarineComponent>(subject, out var marineComp))
        {
            // Format like a signed command announcement with signature and all that crap.
            var formatted = Loc.GetString("rmc-announcement-message-signed", ("author", Loc.GetString("rmc-announcement-author")), ("message", component.Text), ("name", component.Sender));

            // Actually trigger a private announcement to the user.
            _marineAnnounceSystem.AnnounceToMarines(
                message: formatted,
                sound: null,
                filter: Filter.Entities(subject)
            );
            component.HasTriggered = true;
            return;
        }
        // Xeno style Queen announcement
        if (TryComp<XenoComponent>(subject, out var xenoComp))
        {
            // Format like a Words of the Queen announcement (because for some reason that's not a preset)
            var headerText = Loc.GetString("rmc-xeno-words-of-the-queen-header");
            var formatted = FormattedMessage.EscapeText(component.Text);
            var header = $"{_xenoAnnounceSystem.WrapHive(headerText)}";
            var wrapped = $"{header}[color=red][font size=14][bold]{formatted}[/bold][/font][/color]";

            // Actually trigger a private announcement to the user.
            _xenoAnnounceSystem.Announce(
                source: default,
                filter: Filter.Entities(subject),
                message: component.Text,
                wrapped: wrapped,
                sound: new BioscanComponent().XenoSound
            );
            component.HasTriggered = true;
            return;
        }
    }
}
