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

    private void BuildFriendlyIff(SentryTargetingComponent comp)
    {
        _friendlyIffBuffer.Clear();

        foreach (var faction in comp.FriendlyFactions)
        {
            if (SentryFactionToIff.TryGetValue(faction, out var iffFaction))
                _friendlyIffBuffer.Add(iffFaction);
        }
    }

    private bool IsFriendlyByIff(EntityUid target)
    {
        _targetIffBuffer.Clear();
        var ev = new GetIFFFactionEvent(null, SlotFlags.IDCARD, _targetIffBuffer);
        RaiseLocalEvent(target, ref ev);

        if (ev.Faction is { } factionEvent && _friendlyIffBuffer.Contains(factionEvent))
            return true;

        foreach (var targetFaction in _targetIffBuffer)
        {
            if (_friendlyIffBuffer.Contains(targetFaction))
                return true;
        }

        return false;
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
        var hostiles = new List<EntityUid>();

        _candidateLookupBuffer.Clear();
        _lookup.GetEntitiesInRange(coords, range, _userIffLookupBuffer);
        foreach (var target in _userIffLookupBuffer)
        {
            _candidateLookupBuffer.Add(target.Owner);
        }

        _lookup.GetEntitiesInRange(coords, range, _factionLookupBuffer);
        foreach (var target in _factionLookupBuffer)
        {
            _candidateLookupBuffer.Add(target.Owner);
        }

        foreach (var target in _candidateLookupBuffer)
        {
            if (target == ent.Owner)
                continue;

            if (IsFriendlyByIff(target))
                continue;

            hostiles.Add(target);
        }

        _candidateLookupBuffer.Clear();
        _userIffLookupBuffer.Clear();
        _factionLookupBuffer.Clear();
        _friendlyIffBuffer.Clear();
        _targetIffBuffer.Clear();

        return hostiles;
    }

    private void ApplyTargeting(Entity<SentryTargetingComponent> ent)
    {
        UpdateSentryIFF(ent);
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
