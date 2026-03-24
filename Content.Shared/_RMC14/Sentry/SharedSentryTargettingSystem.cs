using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Inventory;
using Content.Shared.NPC.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Sentry;

public abstract class SharedSentryTargetingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GunIFFSystem _iff = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

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

    private readonly HashSet<EntProtoId<IFFFactionComponent>> _friendlyIffBuffer = new();
    private readonly HashSet<EntProtoId<IFFFactionComponent>> _targetIffBuffer = new();
    private readonly HashSet<Entity<NpcFactionMemberComponent>> _factionLookupBuffer = new();
    private readonly HashSet<Entity<UserIFFComponent>> _userIffLookupBuffer = new();
    private readonly HashSet<EntityUid> _candidateLookupBuffer = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<SentryTargetingComponent, MapInitEvent>(OnTargetingMapInit);
        SubscribeLocalEvent<SentryTargetingComponent, ComponentStartup>(OnTargetingStartup);
    }

    private void OnTargetingMapInit(Entity<SentryTargetingComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<NpcFactionMemberComponent>(ent.Owner, out var factionMember) && factionMember.Factions.Count > 0)
            ent.Comp.OriginalFaction = factionMember.Factions.First();

        if (!HasComp<GunIFFComponent>(ent.Owner) && HasComp<GunComponent>(ent.Owner))
            _iff.EnableIntrinsicIFF(ent);
    }

    private void OnTargetingStartup(Entity<SentryTargetingComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.FriendlyFactions.Count == 0 && !string.IsNullOrEmpty(ent.Comp.OriginalFaction))
            ent.Comp.FriendlyFactions.Add(ent.Comp.OriginalFaction);

        if (_net.IsServer)
            ApplyTargeting(ent);
    }

    public void ApplyDeployerFactions(EntityUid sentry, EntityUid deployer)
    {
        var targeting = EnsureComp<SentryTargetingComponent>(sentry);
        targeting.FriendlyFactions.Clear();
        targeting.HumanoidAdded.Clear();

        var iffFactions = new HashSet<EntProtoId<IFFFactionComponent>>();
        var ev = new GetIFFFactionEvent(SlotFlags.IDCARD | SlotFlags.BELT | SlotFlags.POCKET, iffFactions);
        RaiseLocalEvent(deployer, ref ev);

        if (iffFactions.Count > 0)
        {
            foreach (var (sentryFaction, iffFaction) in SentryFactionToIff)
            {
                if (iffFactions.Contains(iffFaction))
                    targeting.FriendlyFactions.Add(sentryFaction);
            }
        }
        else if (TryComp<NpcFactionMemberComponent>(deployer, out var npcFaction))
        {
            foreach (var faction in npcFaction.Factions)
            {
                if (faction != SentryExcludedFaction && SentryAllowedFactions.Contains(faction))
                    targeting.FriendlyFactions.Add(faction);
            }

            if (npcFaction.Factions.Count > 0)
                targeting.OriginalFaction = npcFaction.Factions.First();
        }

        targeting.DeployedFriendlyFactions.Clear();
        targeting.DeployedFriendlyFactions.UnionWith(targeting.FriendlyFactions);

        if (_net.IsServer)
            ApplyTargeting((sentry, targeting));

        Dirty(sentry, targeting);
    }

    public void SetFriendlyFactions(Entity<SentryTargetingComponent> ent, HashSet<string> factions)
    {
        ent.Comp.FriendlyFactions.Clear();
        ent.Comp.HumanoidAdded.Clear();

        var friendly = factions
            .Where(f => f != SentryExcludedFaction && f != "Humanoid" && SentryAllowedFactions.Contains(f))
            .ToHashSet();

        if (factions.Contains("Humanoid"))
        {
            foreach (var faction in GetHumanoidFactions())
            {
                if (friendly.Add(faction))
                    ent.Comp.HumanoidAdded.Add(faction);
            }
        }

        ent.Comp.FriendlyFactions.UnionWith(friendly);

        if (_net.IsServer)
            ApplyTargeting(ent);

        Dirty(ent.Owner, ent.Comp);
    }

    public void ResetToDefault(Entity<SentryTargetingComponent> ent)
    {
        ent.Comp.FriendlyFactions.Clear();
        ent.Comp.HumanoidAdded.Clear();

        if (ent.Comp.DeployedFriendlyFactions.Count > 0)
            ent.Comp.FriendlyFactions.UnionWith(ent.Comp.DeployedFriendlyFactions);

        if (_net.IsServer)
            ApplyTargeting(ent);

        Dirty(ent.Owner, ent.Comp);
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
            Dirty(ent.Owner, ent.Comp);
            return;
        }

        if (friendly)
            ent.Comp.FriendlyFactions.Add(faction);
        else
            ent.Comp.FriendlyFactions.Remove(faction);

        if (_net.IsServer)
            ApplyTargeting(ent);

        Dirty(ent.Owner, ent.Comp);
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

    private void BuildFriendlyIff(SentryTargetingComponent comp)
    {
        _friendlyIffBuffer.Clear();

        foreach (var faction in comp.FriendlyFactions)
        {
            if (SentryFactionToIff.TryGetValue(faction, out var iff))
                _friendlyIffBuffer.Add(iff);
        }
    }

    private bool IsFriendlyByIff(EntityUid target)
    {
        _targetIffBuffer.Clear();
        var ev = new GetIFFFactionEvent(SlotFlags.IDCARD, _targetIffBuffer);
        RaiseLocalEvent(target, ref ev);

        foreach (var faction in _targetIffBuffer)
        {
            if (_friendlyIffBuffer.Contains(faction))
                return true;
        }

        return false;
    }

    public bool IsValidTarget(Entity<SentryTargetingComponent> sentry, EntityUid target)
    {
        if (!HasComp<UserIFFComponent>(target) && !HasComp<NpcFactionMemberComponent>(target))
            return false;

        BuildFriendlyIff(sentry.Comp);
        var friendly = IsFriendlyByIff(target);
        _friendlyIffBuffer.Clear();
        _targetIffBuffer.Clear();
        return !friendly;
    }

    public IEnumerable<EntityUid> GetNearbyIffHostiles(Entity<SentryTargetingComponent> ent, float range)
    {
        BuildFriendlyIff(ent.Comp);

        var coords = _xform.GetMapCoordinates(ent);

        _candidateLookupBuffer.Clear();
        _lookup.GetEntitiesInRange(coords, range, _userIffLookupBuffer);
        foreach (var target in _userIffLookupBuffer)
            _candidateLookupBuffer.Add(target.Owner);

        _lookup.GetEntitiesInRange(coords, range, _factionLookupBuffer);
        foreach (var target in _factionLookupBuffer)
            _candidateLookupBuffer.Add(target.Owner);

        foreach (var target in _candidateLookupBuffer)
        {
            if (target == ent.Owner)
                continue;

            if (!IsFriendlyByIff(target))
                yield return target;
        }

        _candidateLookupBuffer.Clear();
        _userIffLookupBuffer.Clear();
        _factionLookupBuffer.Clear();
        _friendlyIffBuffer.Clear();
        _targetIffBuffer.Clear();
    }

    private void ApplyTargeting(Entity<SentryTargetingComponent> ent)
    {
        UpdateSentryIFF(ent);
    }

    private void UpdateSentryIFF(Entity<SentryTargetingComponent> ent)
    {
        if (!TryComp<UserIFFComponent>(ent.Owner, out var userIff))
            return;

        _iff.ClearUserFactions((ent.Owner, userIff));

        foreach (var faction in ent.Comp.FriendlyFactions)
        {
            if (SentryFactionToIff.TryGetValue(faction, out var iff))
                _iff.AddUserFaction((ent.Owner, userIff), iff);
        }
    }

    public IEnumerable<string> GetHumanoidFactions()
    {
        return SentryAllowedFactions;
    }

    public bool ContainsAllNonXeno(HashSet<string> friendlyFactions)
    {
        return GetHumanoidFactions().All(friendlyFactions.Contains);
    }
}
