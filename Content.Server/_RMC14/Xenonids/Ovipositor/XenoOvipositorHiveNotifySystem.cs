using Content.Server._RMC14.Announce;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Egg;

namespace Content.Server._RMC14.Xenonids.Ovipositor;

public sealed class XenoOvipositorHiveNotifySystem : EntitySystem
{
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoOvipositorChangedEvent>(OnQueenChangedOvi);
    }

    private void OnQueenChangedOvi(Entity<XenoComponent> queen, ref XenoOvipositorChangedEvent args)
    {
        if (args.Attached)
        {
            _xenoAnnounce.AnnounceSameHive(queen.Owner, Loc.GetString("cm-xeno-queen-attach-ovipositor"));
        }
        if (!args.Attached)
        {
            _xenoAnnounce.AnnounceSameHive(queen.Owner, Loc.GetString("cm-xeno-queen-shed-ovipositor"));
        }
    }
}
