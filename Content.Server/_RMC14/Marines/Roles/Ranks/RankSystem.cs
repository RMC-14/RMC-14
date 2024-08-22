using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared.Access.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Marines.Roles.Ranks;

public sealed class RankSystem : SharedRankSystem
{
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RankComponent, TransformSpeakerNameEvent>(OnSpeakerNameTransform);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnSpeakerNameTransform(EntityUid uid, RankComponent component, TransformSpeakerNameEvent args)
    {
        var name = GetSpeakerRankName(uid);
        if (name == null)
            return;

        args.Name = name;
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var uid = ev.Mob;
        var jobId = ev.JobId;

        if (jobId == null)
            return;

        if (_idCardSystem.TryFindIdCard(uid, out var idcard))
        {
            var idCardEntity = idcard.Owner;

            if (jobId == null)
                return;

            foreach (var rankPrototype in _prototypes.EnumeratePrototypes<RankPrototype>())
            {
                foreach (var job in rankPrototype.Jobs)
                {
                    if (job.Id == jobId)
                    {
                        SetRank(idCardEntity, rankPrototype);
                        SetRank(uid, rankPrototype);
                        break;
                    }
                }
            }
        }
    }
}