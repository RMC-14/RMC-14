using Content.Shared._RMC14.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Commendations;

public abstract class SharedCommendationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    protected readonly List<Commendation> RoundCommendations = new();

    public int CharacterLimit { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<CommendationReceiverComponent, PlayerAttachedEvent>(OnCommendationReceiverPlayerAttached);

        Subs.CVar(_config, RMCCVars.RMCCommendationMaxLength, v => CharacterLimit = v, true);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        RoundCommendations.Clear();
    }

    private void OnCommendationReceiverPlayerAttached(Entity<CommendationReceiverComponent> ent, ref PlayerAttachedEvent args)
    {
        ent.Comp.LastPlayerId = args.Player.UserId.UserId.ToString();
    }

    public bool ValidCommendation(
        Entity<CommendationGiverComponent?, ActorComponent?> giver,
        Entity<CommendationReceiverComponent?> receiver,
        string text)
    {
        if (!Resolve(giver, ref giver.Comp1, ref giver.Comp2, false) ||
            !Resolve(receiver, ref receiver.Comp, false) ||
            receiver.Comp.LastPlayerId == null)
        {
            return false;
        }

        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return true;
    }

    public virtual void GiveCommendation(
        Entity<CommendationGiverComponent?, ActorComponent?> giver,
        Entity<CommendationReceiverComponent?> receiver,
        string name,
        string text,
        CommendationType type)
    {
    }

    public virtual void GiveCommendationByLastPlayerId(
        Entity<CommendationGiverComponent?, ActorComponent?> giver,
        string lastPlayerId,
        string receiverName,
        string name,
        string text,
        CommendationType type)
    {
    }

    public IReadOnlyList<Commendation> GetCommendations()
    {
        return RoundCommendations;
    }
}
