using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Network;
using System.Linq;

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

        Logger.Info($"[SENTRY TARGET] OnTargetingMapInit for {ent.Owner}");
        Logger.Info($"[SENTRY TARGET] SentryTargetingComponent.FriendlyFactions hashcode: {ent.Comp.FriendlyFactions.GetHashCode()}");

        if (TryComp<NpcFactionMemberComponent>(ent, out var npcFaction))
        {
            Logger.Info($"[SENTRY TARGET] NpcFactionMemberComponent.FriendlyFactions hashcode: {npcFaction.FriendlyFactions.GetHashCode()}");
            Logger.Info($"[SENTRY TARGET] Are they the same object? {ReferenceEquals(ent.Comp.FriendlyFactions, npcFaction.FriendlyFactions)}");
        }

        if (!HasComp<GunIFFComponent>(ent) && HasComp<GunComponent>(ent))
        {
            _iff.EnableIntrinsicIFF(ent);
        }
    }

    private void OnTargetingStartup(Entity<SentryTargetingComponent> ent, ref ComponentStartup args)
    {
        if (_net.IsServer)
            ApplyTargeting(ent);
    }

    public void SetFriendlyFactions(Entity<SentryTargetingComponent> ent, HashSet<string> factions)
    {
        Logger.Info($"[SENTRY TARGET] SetFriendlyFactions called for sentry {ent.Owner} with {factions.Count} factions:");
        foreach (var f in factions)
        {
            Logger.Info($"[SENTRY TARGET]   - Setting: {f}");
        }

        var comp = ent.Comp;
        comp.FriendlyFactions.Clear();
        foreach (var faction in factions)
        {
            if (faction == SentryHostileToAllFaction)
            {
                Logger.Info($"[SENTRY TARGET] Filtering out {SentryHostileToAllFaction} from friendly factions");
                continue;
            }
            comp.FriendlyFactions.Add(faction);
        }

        Logger.Info($"[SENTRY TARGET] After setting, SentryTargetingComponent.FriendlyFactions has {comp.FriendlyFactions.Count} factions");

        if (_net.IsServer)
            ApplyTargeting(ent);

        Dirty(ent);
    }

    public void ToggleFaction(Entity<SentryTargetingComponent> ent, string faction, bool friendly)
    {
        Logger.Info($"[SENTRY TARGET] ToggleFaction called for sentry {ent.Owner}: faction={faction}, friendly={friendly}");

        if (faction == SentryHostileToAllFaction)
        {
            Logger.Info($"[SENTRY TARGET] Rejecting attempt to toggle {SentryHostileToAllFaction} - this should never be friendly");
            return;
        }

        if (friendly)
            ent.Comp.FriendlyFactions.Add(faction);
        else
            ent.Comp.FriendlyFactions.Remove(faction);

        Logger.Info($"[SENTRY TARGET] Sentry {ent.Owner} now has {ent.Comp.FriendlyFactions.Count} friendly factions");

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
        {
            comp.FriendlyFactions.Add(originalFaction);
        }

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
        Logger.Info($"[SENTRY TARGET] ApplyTargeting called for sentry {ent.Owner}");

        if (!TryComp<NpcFactionMemberComponent>(ent, out var factionMember))
        {
            Logger.Info($"[SENTRY TARGET] Sentry {ent.Owner} has no NpcFactionMemberComponent");
            return;
        }

        Logger.Info($"[SENTRY TARGET] BEFORE CLEAR - SentryTargetingComponent.FriendlyFactions has {ent.Comp.FriendlyFactions.Count} factions:");
        foreach (var f in ent.Comp.FriendlyFactions)
        {
            Logger.Info($"[SENTRY TARGET]   - {f}");
        }

        _faction.ClearFactions((ent, factionMember), dirty: false);
        _faction.AddFaction((ent, factionMember), SentryHostileToAllFaction, dirty: false);

        var friendlyFactions = ent.Comp.FriendlyFactions.ToList();
        Logger.Info($"[SENTRY TARGET] Sentry {ent.Owner} has {friendlyFactions.Count} friendly factions to add");

        var allFactions = GetAllNpcFactions();
        Logger.Info($"[SENTRY TARGET] Found {allFactions.Count} total NPC factions in game");

        var count = 0;
        foreach (var faction in allFactions)
        {
            if (ent.Comp.FriendlyFactions.Contains(faction))
            {
                count++;
                var isLast = count == friendlyFactions.Count;
                _faction.AddFaction((ent, factionMember), faction, dirty: isLast);
                Logger.Info($"[SENTRY TARGET] Added NPC faction {faction} to sentry {ent.Owner} (dirty={isLast})");
            }
        }

        if (count == 0)
        {
            Logger.Info($"[SENTRY TARGET] No friendly factions added, calling Dirty manually");
            Dirty(ent, factionMember);
        }

        Logger.Info($"[SENTRY TARGET] Calling UpdateSentryIFF for sentry {ent.Owner}");
        UpdateSentryIFF(ent);
    }

    private HashSet<string> GetAllNpcFactions()
    {
        var factions = new HashSet<string>();

        var query = AllEntityQuery<NpcFactionMemberComponent>();
        while (query.MoveNext(out var comp))
        {
            foreach (var faction in comp.Factions)
            {
                factions.Add(faction);
            }
        }

        return factions;
    }

    private void UpdateSentryIFF(Entity<SentryTargetingComponent> ent)
    {
        Logger.Info($"[SENTRY IFF] UpdateSentryIFF called for sentry {ent.Owner}");

        if (!TryComp<UserIFFComponent>(ent, out var userIFF))
        {
            Logger.Info($"[SENTRY IFF] Sentry {ent.Owner} has no UserIFFComponent, cannot update IFF");
            return;
        }

        Logger.Info($"[SENTRY IFF] Sentry {ent.Owner} has {ent.Comp.FriendlyFactions.Count} friendly factions");
        foreach (var faction in ent.Comp.FriendlyFactions)
        {
            Logger.Info($"[SENTRY IFF] Friendly faction: {faction}");
        }

        _iff.ClearUserFactions((ent, userIFF));

        var factionToIFF = GetFactionToIFFMapping();

        var addedCount = 0;
        foreach (var faction in ent.Comp.FriendlyFactions)
        {
            if (faction == SentryHostileToAllFaction)
            {
                Logger.Info($"[SENTRY IFF] Skipping {SentryHostileToAllFaction} - should never be in IFF");
                continue;
            }

            if (factionToIFF.TryGetValue(faction, out var iffFaction))
            {
                Logger.Info($"[SENTRY IFF] Mapping NPC faction {faction} to IFF faction {iffFaction}");
                _iff.AddUserFaction((ent, userIFF), iffFaction);
                addedCount++;
            }
            else
            {
                Logger.Info($"[SENTRY IFF] No IFF mapping found for NPC faction {faction}");
            }
        }

        Logger.Info($"[SENTRY IFF] UpdateSentryIFF complete for sentry {ent.Owner}, added {addedCount} IFF factions");
    }

    private Dictionary<string, string> GetFactionToIFFMapping()
    {
        return new Dictionary<string, string>
        {
            { "UNMC", "FactionMarine" },
            { "RMCXeno", "FactionXeno" },
            { "CLF", "FactionCLF" },
            { "SPP", "FactionSPP" },
            { "Halcyon", "FactionHalcyon" },
            { "WeYa", "FactionWeYa" },
            { "Civilian", "FactionCivilian" },
            { "RoyalMarines", "FactionRoyalMarines" },
            { "Bureau", "FactionBureau" },
            { "TSE", "FactionTSE" }
        };
    }
}
