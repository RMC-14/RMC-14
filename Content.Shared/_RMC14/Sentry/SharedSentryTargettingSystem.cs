using System.Linq;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Sentry;

public abstract class SharedSentryTargetingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly GunIFFSystem _iff = default!;

    private const string SentryHostileToAllFaction = "RMCDumb";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SentryTargetingComponent, MapInitEvent>(OnTargetingMapInit);
        SubscribeLocalEvent<SentryTargetingComponent, ComponentStartup>(OnTargetingStartup);
    }

    private void OnTargetingMapInit(Entity<SentryTargetingComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<NpcFactionMemberComponent>(ent, out var factionMember) && factionMember.Factions.Count > 0)
        {
            ent.Comp.OriginalFaction = factionMember.Factions.First();
            Dirty(ent);
        }

        if (!HasComp<GunIFFComponent>(ent) && HasComp<GunComponent>(ent))
            _iff.EnableIntrinsicIFF(ent);
    }

    private void OnTargetingStartup(Entity<SentryTargetingComponent> ent, ref ComponentStartup args)
    {
        if (_net.IsServer)
            ApplyTargeting(ent);
    }

    public void SetFriendlyFactions(Entity<SentryTargetingComponent> ent, HashSet<string> factions)
    {
        var comp = ent.Comp;
        comp.FriendlyFactions.Clear();

        foreach (var faction in factions)
        {
            if (faction == SentryHostileToAllFaction)
                continue;

            comp.FriendlyFactions.Add(faction);
        }

        if (_net.IsServer)
            ApplyTargeting(ent);

        Dirty(ent);
    }

    public void ToggleFaction(Entity<SentryTargetingComponent> ent, string faction, bool friendly)
    {
        if (faction == SentryHostileToAllFaction)
            return;

        if (friendly)
            ent.Comp.FriendlyFactions.Add(faction);
        else
            ent.Comp.FriendlyFactions.Remove(faction);

        if (_net.IsServer)
            ApplyTargeting(ent);

        Dirty(ent);
    }

    public void ResetToDefault(Entity<SentryTargetingComponent> ent)
    {
        var comp = ent.Comp;
        comp.FriendlyFactions.Clear();

        var originalFaction = ent.Comp.OriginalFaction;
        if (!string.IsNullOrEmpty(originalFaction))
            comp.FriendlyFactions.Add(originalFaction);

        if (_net.IsServer)
            ApplyTargeting(ent);

        Dirty(ent);
    }

    public bool IsValidTarget(Entity<SentryTargetingComponent> sentry, EntityUid target)
    {
        if (!TryComp<NpcFactionMemberComponent>(target, out var targetFaction))
            return false;

        foreach (var faction in targetFaction.Factions)
        {
            if (sentry.Comp.FriendlyFactions.Contains(faction))
                return false;
        }

        return true;
    }

    private void ApplyTargeting(Entity<SentryTargetingComponent> ent)
    {
        if (!TryComp<NpcFactionMemberComponent>(ent, out var factionMember))
            return;

        _faction.ClearFactions((ent, factionMember), dirty: false);
        _faction.AddFaction((ent, factionMember), SentryHostileToAllFaction, dirty: false);

        var friendlyFactions = ent.Comp.FriendlyFactions.ToList();
        var allFactions = GetAllNpcFactions();

        var count = 0;

        foreach (var faction in allFactions)
        {
            if (!ent.Comp.FriendlyFactions.Contains(faction))
                continue;

            count++;
            var isLast = count == friendlyFactions.Count;
            _faction.AddFaction((ent, factionMember), faction, dirty: isLast);
        }

        if (count == 0)
            Dirty(ent, factionMember);

        UpdateSentryIFF(ent);
    }

    private HashSet<string> GetAllNpcFactions()
    {
        var factions = new HashSet<string>();
        var query = AllEntityQuery<NpcFactionMemberComponent>();

        while (query.MoveNext(out var comp))
        {
            foreach (var faction in comp.Factions)
                factions.Add(faction);
        }

        return factions;
    }

    private void UpdateSentryIFF(Entity<SentryTargetingComponent> ent)
    {
        if (!TryComp<UserIFFComponent>(ent, out var userIFF))
            return;

        _iff.ClearUserFactions((ent, userIFF));

        var factionToIFF = GetFactionToIFFMapping();
        var addedCount = 0;

        foreach (var faction in ent.Comp.FriendlyFactions)
        {
            if (faction == SentryHostileToAllFaction)
                continue;

            if (!factionToIFF.TryGetValue(faction, out var iffFaction))
                continue;

            _iff.AddUserFaction((ent, userIFF), iffFaction);
            addedCount++;
        }
    }

    private Dictionary<string, EntProtoId<IFFFactionComponent>> GetFactionToIFFMapping()
    {
        return new Dictionary<string, EntProtoId<IFFFactionComponent>>
        {
            { "UNMC", "FactionMarine" },
            { "RMCXeno", "FactionXeno" },
            { "CLF", "FactionCLF" },
            { "SPP", "FactionSPP" },
            { "Halcyon", "FactionHalcyon" },
            { "WeYa", "FactionWeYa" },
            { "Civilian", "FactionSurvivor" },
            { "RoyalMarines", "FactionRoyalMarines" },
            { "Bureau", "FactionBureau" },
            { "TSE", "FactionTSE" }
        };
    }

    public void ApplyDeployerFactions(EntityUid sentry, EntityUid deployer)
    {
        if (!TryComp<NpcFactionMemberComponent>(deployer, out var deployerFaction))
            return;

        if (deployerFaction.Factions.Count == 0)
            return;

        var targeting = EnsureComp<SentryTargetingComponent>(sentry);

        var newFactions = new HashSet<string>(targeting.FriendlyFactions);

        foreach (var faction in deployerFaction.Factions)
        {
            if (faction == SentryHostileToAllFaction)
                continue;

            newFactions.Add(faction);
        }

        targeting.OriginalFaction = deployerFaction.Factions.First();

        SetFriendlyFactions((sentry, targeting), newFactions);
    }
}
