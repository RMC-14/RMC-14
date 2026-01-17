using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Commendations;

public abstract class SharedCommendationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    protected readonly List<RoundCommendationEntry> RoundCommendations = new();

    public int CharacterLimit { get; private set; }
    public int MinCharacterLimit { get; private set; }

    /// <summary>
    /// List of entity prototype IDs for medals that can be awarded.
    /// This is the single source of truth for standard awardable medals.
    /// </summary>
    protected static readonly IReadOnlyList<ProtoId<EntityPrototype>> AwardableMedalIds = new[]
    {
        new ProtoId<EntityPrototype>("RMCMedalGoldExceptionalHeroism"),
        new ProtoId<EntityPrototype>("RMCMedalSilverValor"),
        new ProtoId<EntityPrototype>("RMCMedalBronzeDistinguishedConduct"),
        new ProtoId<EntityPrototype>("RMCMedalBronzeHeart")
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<CommendationReceiverComponent, PlayerAttachedEvent>(OnCommendationReceiverPlayerAttached);

        Subs.CVar(_config, RMCCVars.RMCCommendationMaxLength, v => CharacterLimit = v, true);
        Subs.CVar(_config, RMCCVars.RMCCommendationMinLength, v => MinCharacterLimit = v, true);
    }

    /// <summary>
    /// Gets the list of entity prototype IDs for standard medals that can be awarded.
    /// </summary>
    public IReadOnlyList<ProtoId<EntityPrototype>> GetAwardableMedalIds()
    {
        return AwardableMedalIds;
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
        CommendationType type,
        ProtoId<EntityPrototype>? commendationPrototypeId = null)
    {
    }

    public virtual void GiveCommendationByLastPlayerId(
        Entity<CommendationGiverComponent?, ActorComponent?> giver,
        string lastPlayerId,
        string receiverName,
        string name,
        string text,
        CommendationType type,
        ProtoId<EntityPrototype>? commendationPrototypeId = null)
    {
    }

    public IReadOnlyList<Commendation> GetCommendations()
    {
        return RoundCommendations.Select(e => e.Commendation).ToList();
    }

    public IReadOnlyList<RoundCommendationEntry> GetRoundCommendationEntries()
    {
        return RoundCommendations;
    }
}
