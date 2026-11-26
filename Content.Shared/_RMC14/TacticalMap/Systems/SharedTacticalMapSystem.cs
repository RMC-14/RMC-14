using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Inventory;
using Content.Shared.NPC.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

public abstract class SharedTacticalMapSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly XenoEvolutionSystem _evolution = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    public int LineLimit { get; private set; }
    public int LabelLimit { get; private set; }
    public int LabelMaxLength { get; private set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacticalMapActionEvent>(OnUserOpenAction);
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacMapAlertEvent>(OnUserOpenAlert);
        SubscribeLocalEvent<TacticalMapUserComponent, TacticalMapCollectAccessEvent>(OnUserCollectAccess);

        SubscribeLocalEvent<TacticalMapComputerComponent, TacticalMapCollectAccessEvent>(OnComputerCollectAccess);

        SubscribeLocalEvent<InventoryComponent, TacticalMapCollectAccessEvent>(OnInventoryCollectAccess);
        SubscribeLocalEvent<TacticalMapAccessComponent, InventoryRelayedEvent<TacticalMapCollectAccessEvent>>(OnAccessItemCollectAccess);

        SubscribeLocalEvent<TacticalMapComponent, ComponentGetState>(OnMapGetState);
        SubscribeLocalEvent<TacticalMapLiveFeedComponent, ComponentGetState>(OnLiveFeedGetState);

        Subs.CVar(_config, RMCCVars.RMCTacticalMapLineLimit, v => LineLimit = v, true);
        Subs.CVar(_config, RMCCVars.RMCTacticalMapLabelLimit, v => LabelLimit = v, true);
        Subs.CVar(_config, RMCCVars.RMCTacticalMapLabelMaxLength, v => LabelMaxLength = v, true);
    }

    private void OnUserOpenAction(Entity<TacticalMapUserComponent> ent, ref OpenTacticalMapActionEvent args)
    {
        ToggleUserMap(ent);
    }

    private void OnUserOpenAlert(Entity<TacticalMapUserComponent> ent, ref OpenTacMapAlertEvent args)
    {
        ToggleUserMap(ent);
    }

    private void OnUserCollectAccess(Entity<TacticalMapUserComponent> ent, ref TacticalMapCollectAccessEvent args)
    {
        if (ent.Comp.AllChannels)
            args.AllChannels = true;

        if (ent.Comp.LiveUpdate || HasComp<TacMapLiveUpdateComponent>(ent))
            args.Live = true;

        if (TryComp(ent, out TacticalMapLiveUpdateOnOviComponent? ovi) && ovi.Enabled && _evolution.HasOvipositor())
            args.Live = true;

        if (TryComp(ent, out NpcFactionMemberComponent? factions))
            args.Factions.UnionWith(factions.Factions);

        if (TryComp(ent, out HiveMemberComponent? hive) && hive.Hive is { } hiveId)
            args.Hives.Add(hiveId);

        if (_squad.TryGetMemberSquad(ent.Owner, out var squad))
            args.Squads.Add(squad.Owner);
    }

    private void OnComputerCollectAccess(Entity<TacticalMapComputerComponent> ent, ref TacticalMapCollectAccessEvent args)
    {
        args.Factions.UnionWith(ent.Comp.Factions);
        args.SquadGroups.UnionWith(ent.Comp.SquadGroups);
        args.AllSquads |= ent.Comp.AllSquads;
        args.Live |= ent.Comp.LiveUpdate;
    }

    private void OnInventoryCollectAccess(Entity<InventoryComponent> ent, ref TacticalMapCollectAccessEvent args)
    {
        _inventory.RelayEvent(ent, ref args);
    }

    private void OnAccessItemCollectAccess(Entity<TacticalMapAccessComponent> ent, ref InventoryRelayedEvent<TacticalMapCollectAccessEvent> args)
    {
        args.Args.Factions.UnionWith(ent.Comp.Factions);
    }

    private void OnMapGetState(Entity<TacticalMapComponent> ent, ref ComponentGetState args)
    {
        var comp = ent.Comp;
        if (args.FromTick <= comp.CreationTick)
        {
            var chunks = new Dictionary<Vector2i, TacticalMapChunkData>(comp.Chunks.Count);
            foreach (var (index, chunk) in comp.Chunks)
            {
                chunks[index] = ToData(chunk);
            }

            args.State = new TacticalMapComponentState(
                comp.DisplayName,
                chunks,
                new Dictionary<Vector2i, string>(comp.AreaLabels),
                comp.HasBounds,
                comp.BoundsMin,
                comp.BoundsMax);
            return;
        }

        var modified = new Dictionary<Vector2i, TacticalMapChunkData>();
        foreach (var (index, chunk) in comp.Chunks)
        {
            if (chunk.LastUpdate < args.FromTick)
                continue;

            modified[index] = ToData(chunk);
        }

        args.State = new TacticalMapComponentDeltaState(
            comp.DisplayName,
            modified,
            new HashSet<Vector2i>(comp.Chunks.Keys),
            new Dictionary<Vector2i, string>(comp.AreaLabels),
            comp.HasBounds,
            comp.BoundsMin,
            comp.BoundsMax);
    }

    private void OnLiveFeedGetState(Entity<TacticalMapLiveFeedComponent> ent, ref ComponentGetState args)
    {
        var comp = ent.Comp;
        var channel = GetNetEntity(comp.Channel) ?? NetEntity.Invalid;
        if (args.FromTick <= comp.CreationTick)
        {
            args.State = new TacticalMapLiveFeedState(channel, new Dictionary<NetEntity, TacticalMapBlip>(comp.Blips));
            return;
        }

        var modified = new Dictionary<NetEntity, TacticalMapBlip>();
        foreach (var (key, tick) in comp.Modified)
        {
            if (tick < args.FromTick || !comp.Blips.TryGetValue(key, out var blip))
                continue;

            modified[key] = blip;
        }

        args.State = new TacticalMapLiveFeedDeltaState(channel, modified, new HashSet<NetEntity>(comp.Blips.Keys));
    }

    private static TacticalMapChunkData ToData(TacticalMapChunk chunk)
    {
        return new TacticalMapChunkData((uint[]) chunk.Colors.Clone(), (uint[]) chunk.Flags.Clone());
    }

    public TacticalMapCollectAccessEvent CollectAccess(EntityUid viewer)
    {
        var ev = new TacticalMapCollectAccessEvent(SlotFlags.IDCARD);
        RaiseLocalEvent(viewer, ref ev);
        return ev;
    }

    public bool TryGetViewer(EntityUid viewer, out IReadOnlyList<EntityUid> channels, out bool live)
    {
        if (TryComp(viewer, out TacticalMapComputerComponent? computer))
        {
            channels = computer.Channels;
            live = computer.Live;
            return true;
        }

        if (TryComp(viewer, out TacticalMapUserComponent? user))
        {
            channels = user.Channels;
            live = user.Live;
            return true;
        }

        channels = Array.Empty<EntityUid>();
        live = false;
        return false;
    }

    public bool CanDraw(EntityUid actor, EntityUid viewer, EntityUid channel)
    {
        if (!HasComp<TacticalMapCanvasComponent>(channel))
            return false;

        if (!TryGetViewer(viewer, out var channels, out _) || !channels.Contains(channel))
            return false;

        if (TryComp(viewer, out TacticalMapComputerComponent? computer))
        {
            if (!computer.CanDraw || !_skills.HasSkill(actor, computer.Skill, computer.SkillLevel))
                return false;
        }
        else if (!TryComp(viewer, out TacticalMapUserComponent? user) || !user.CanDraw || viewer != actor)
        {
            return false;
        }

        var ev = new TacticalMapCanDrawEvent(actor, channel);
        RaiseLocalEvent(viewer, ref ev);
        return !ev.Cancelled;
    }

    public void OpenComputerMap(Entity<TacticalMapComputerComponent?> computer, EntityUid user)
    {
        if (!Resolve(computer, ref computer.Comp, false))
            return;

        UI.TryOpenUi(computer.Owner, TacticalMapComputerUi.Key, user);
    }

    public virtual void OpenViewer(EntityUid viewer, EntityUid actor)
    {
    }

    public virtual void CloseViewer(EntityUid viewer, EntityUid actor)
    {
    }

    private void ToggleUserMap(Entity<TacticalMapUserComponent> user)
    {
        if (UI.IsUiOpen(user.Owner, TacticalMapUserUi.Key, user))
        {
            UI.CloseUi(user.Owner, TacticalMapUserUi.Key, user);
            return;
        }

        UI.TryOpenUi(user.Owner, TacticalMapUserUi.Key, user);
    }

    public static bool IsRevealed(TacticalMapLiveFeedComponent feed, NetEntity blip)
    {
        return feed.Revealed.Contains(blip);
    }

    public static uint PackColor(Color color)
    {
        return unchecked((uint) color.ToArgb());
    }

    public static Color UnpackColor(uint packed)
    {
        return new Color(
            (byte) ((packed >> 16) & 0xFF),
            (byte) ((packed >> 8) & 0xFF),
            (byte) (packed & 0xFF),
            (byte) ((packed >> 24) & 0xFF));
    }
}
