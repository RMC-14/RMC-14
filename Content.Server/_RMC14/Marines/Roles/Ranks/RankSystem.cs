using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Marines.Roles.Ranks;

namespace Content.Server._RMC14.Marines.Roles.Ranks;

public sealed class RankSystem : SharedRankSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RankComponent, TransformSpeakerNameEvent>(OnSpeakerNameTransform);
    }

    private void OnSpeakerNameTransform(Entity<RankComponent> ent, ref TransformSpeakerNameEvent args)
    {
        var uid = ent.Owner;
        var rank = GetRank(uid);

        if (rank == null)
            return;

        var shortRank = rank.ShortenedName;
        var finalName = shortRank + " " + Name(uid);

        args.Name = finalName;
    }
}
