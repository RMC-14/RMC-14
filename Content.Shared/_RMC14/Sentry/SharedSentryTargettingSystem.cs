using System.Linq;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
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

    private const string SentryExcludedFaction = "RMCDumb";
    public static readonly Dictionary<string, EntProtoId<IFFFactionComponent>> SentryFactionToIff = new()
    {
        { "UNMC", "FactionMarine" },
        { "CLF", "FactionCLF" },
        { "SPP", "FactionSPP" },
        { "Halcyon", "FactionHalcyon" },
        { "WeYa", "FactionWeYa" },
        { "Civilian", "FactionSurvivor" },
        { "RoyalMarines", "FactionRoyalMarines" },
        { "Bureau", "FactionBureau" },
        { "TSE", "FactionTSE" }
    };

    public static readonly HashSet<string> SentryAllowedFactions = SentryFactionToIff.Keys.ToHashSet();

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
        if (ent.Comp.FriendlyFactions.Count == 0 && !string.IsNullOrEmpty(ent.Comp.OriginalFaction))
        {
            ent.Comp.FriendlyFactions.Add(ent.Comp.OriginalFaction);
            ent.Comp.HumanoidAdded.Clear();
        }

        if (_net.IsServer)
            ApplyTargeting(ent);
    }

    public void SetFriendlyFactions(Entity<SentryTargetingComponent> ent, HashSet<string> factions)
    {
        var comp = ent.Comp;
        comp.FriendlyFactions.Clear();
        comp.HumanoidAdded.Clear();

        var friendly = factions
            .Where(f => f != SentryExcludedFaction && f != "Humanoid" && SentryAllowedFactions.Contains(f))
            .ToHashSet();

        if (factions.Contains("Humanoid"))
        {
            foreach (var faction in GetHumanoidFactions())
            {
                if (friendly.Add(faction))
                    comp.HumanoidAdded.Add(faction);
            }
        }

        comp.FriendlyFactions.UnionWith(friendly);

        if (_net.IsServer)
            ApplyTargeting(ent);

        Dirty(ent);
    }

    public void ToggleFaction(Entity<SentryTargetingComponent> ent, string faction, bool friendly)
    {
        if (faction == SentryExcludedFaction)
            return;

        if (faction == "Humanoid")
        {
            ToggleHumanoid(ent, friendly);
            if (_net.IsServer)
                ApplyTargeting(ent);
            Dirty(ent);
            return;
        }

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
        comp.HumanoidAdded.Clear();

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
        _faction.SetAdditionalHostiles((ent, factionMember), BuildHostileFactions(ent.Comp.FriendlyFactions), dirty: false);

        var friendlyFactions = ent.Comp.FriendlyFactions
            .Where(f => f != SentryExcludedFaction && SentryAllowedFactions.Contains(f))
            .ToList();

        if (friendlyFactions.Count == 0)
        {
            _faction.ClearFactions((ent, factionMember));
            UpdateSentryIFF(ent);
            return;
        }

        for (var i = 0; i < friendlyFactions.Count; i++)
        {
            var faction = friendlyFactions[i];
            var isLast = i == friendlyFactions.Count - 1;
            _faction.AddFaction((ent, factionMember), faction, dirty: isLast);
        }

        UpdateSentryIFF(ent);
    }

    private HashSet<ProtoId<NpcFactionPrototype>> BuildHostileFactions(HashSet<string> friendlyFactions)
    {
        var hostiles = new HashSet<ProtoId<NpcFactionPrototype>>();
        var allFactions = _faction.GetFactions();

        foreach (var faction in allFactions.Keys)
        {
            if (faction == SentryExcludedFaction)
                continue;

            if (friendlyFactions.Contains(faction))
                continue;

            hostiles.Add(faction);
        }

        return hostiles;
    }

    private void UpdateSentryIFF(Entity<SentryTargetingComponent> ent)
    {
        if (!TryComp<UserIFFComponent>(ent, out var userIFF))
            return;

        _iff.ClearUserFactions((ent, userIFF));

        var factionToIFF = GetFactionToIFFMapping();

        foreach (var faction in ent.Comp.FriendlyFactions)
        {
            if (!factionToIFF.TryGetValue(faction, out var iffFaction))
                continue;

            _iff.AddUserFaction((ent, userIFF), iffFaction);
        }
    }

    private Dictionary<string, EntProtoId<IFFFactionComponent>> GetFactionToIFFMapping()
    {
        return SentryFactionToIff;
    }

    public void ApplyDeployerFactions(EntityUid sentry, EntityUid deployer)
    {
        if (!TryComp<NpcFactionMemberComponent>(deployer, out var deployerFaction))
            return;

        if (deployerFaction.Factions.Count == 0)
            return;

        var targeting = EnsureComp<SentryTargetingComponent>(sentry);
        var newFactions = new HashSet<string>();

        foreach (var faction in deployerFaction.Factions)
        {
            if (faction == SentryExcludedFaction)
                continue;

            if (SentryAllowedFactions.Contains(faction))
                newFactions.Add(faction);
        }

        targeting.OriginalFaction = deployerFaction.Factions.First();

        SetFriendlyFactions((sentry, targeting), newFactions);
    }

    public IEnumerable<string> GetHumanoidFactions()
    {
        return SentryAllowedFactions;
    }

    public bool ContainsAllNonXeno(HashSet<string> friendlyFactions)
    {
        var allowed = GetHumanoidFactions().ToList();
        return allowed.All(friendlyFactions.Contains);
    }

    public void ToggleHumanoid(Entity<SentryTargetingComponent> ent, bool friendly)
    {
        if (friendly)
        {
            foreach (var faction in GetHumanoidFactions())
            {
                if (ent.Comp.FriendlyFactions.Add(faction))
                    ent.Comp.HumanoidAdded.Add(faction);
            }
        }
        else
        {
            foreach (var faction in ent.Comp.HumanoidAdded)
                ent.Comp.FriendlyFactions.Remove(faction);

            ent.Comp.HumanoidAdded.Clear();
        }
    }
}
