using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Hallucination;
using Content.Shared._RMC14.Hallucination.Events;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _announce = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly StatusEffectsSystem _oldStatus = default!;
    [Dependency] private readonly RMCPlanetSystem _planet = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;

    private static readonly EntProtoId HallucinationStatus = "RMCStatusEffectHallucination";
    private static readonly EntProtoId BloodVisual = "RMCHallucinationBlood";

    private const int MaxEventRolls = 10;
    private const float BlockCheckRadius = 0.3f;

    private readonly HashSet<Entity<HumanoidAppearanceComponent>> _humanoids = new();
    private readonly Dictionary<EntProtoId, EntityUid> _singletons = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCHallucinationStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusApplied);
        SubscribeLocalEvent<RMCHallucinationStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusRemoved);

        SubscribeLocalEvent<RMCHallucinatingComponent, ComponentShutdown>(OnHallucinatingShutdown);
        SubscribeLocalEvent<RMCHallucinatingComponent, EntityUnpausedEvent>(OnHallucinatingUnpaused);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<RMCHallucinationStrangeSoundComponent, RMCHallucinateEvent>(OnStrangeSound);

        InitializeAttacker();
        InitializeBodies();
        InitializeComms();
        InitializeDanger();
        InitializeHud();
        InitializeXeno();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _singletons.Clear();
    }

    private void OnStrangeSound(Entity<RMCHallucinationStrangeSoundComponent> ent, ref RMCHallucinateEvent args)
    {
        PlayStrangeSound(args.Victim);
        args.Handled = true;
    }

    private void OnStatusApplied(Entity<RMCHallucinationStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        var hallucinating = EnsureComp<RMCHallucinatingComponent>(args.Target);
        hallucinating.NextTick = _timing.CurTime + hallucinating.TickEvery;
    }

    private void OnStatusRemoved(Entity<RMCHallucinationStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!TryComp(args.Target, out RMCHallucinatingComponent? hallucinating))
            return;

        hallucinating.NextEvent = null;
        hallucinating.NextSound = null;
        if (IsIdle((args.Target, hallucinating)))
            RemCompDeferred<RMCHallucinatingComponent>(args.Target);
    }

    private void OnHallucinatingUnpaused(Entity<RMCHallucinatingComponent> ent, ref EntityUnpausedEvent args)
    {
        var comp = ent.Comp;
        var paused = args.PausedTime;
        comp.NextTick += paused;
        comp.NextEvent += paused;
        comp.NextSound += paused;
        comp.FakeHudEnd += paused;
        comp.ItemEnd += paused;
        comp.ImageEnd += paused;
        comp.NextQueenStep += paused;
        comp.StrikeEnd += paused;
        comp.LurkerEnd += paused;
        comp.WeedsEnd += paused;
        comp.LightsEnd += paused;
        comp.FakeBlipsEnd += paused;
        comp.DisguiseEnd += paused;
        comp.FakeInfectionStart += paused;
        comp.FakeInfectionEnd += paused;
        comp.NextFakeSymptom += paused;
        comp.Parasite?.Offset(paused);

        foreach (var attacker in comp.Attackers)
        {
            attacker.End += paused;
            attacker.NextProcess += paused;
            attacker.NextSync += paused;
            attacker.BackOffUntil += paused;
            attacker.RemoveAt += paused;
            attacker.NextSpeak += paused;
        }

        foreach (var body in comp.FakeBodies)
        {
            body.End += paused;
            body.NextSpeak += paused;
        }

        foreach (var step in comp.Steps)
        {
            step.At += paused;
        }

        foreach (ref var blood in CollectionsMarshal.AsSpan(comp.FakeBlood))
        {
            blood.End += paused;
        }
    }

    private void OnHallucinatingShutdown(Entity<RMCHallucinatingComponent> ent, ref ComponentShutdown args)
    {
        foreach (var attacker in ent.Comp.Attackers.ToArray())
        {
            RemoveAttacker(ent, attacker);
        }

        ent.Comp.FakeHud = RMCFakeHud.None;
        if (!TerminatingOrDeleted(ent))
            RefreshFakeHud(ent);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCHallucinatingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (uid, comp);

            for (var i = comp.Steps.Count - 1; i >= 0; i--)
            {
                var step = comp.Steps[i];
                if (time < step.At)
                    continue;

                comp.Steps.RemoveAt(i);
                RunStep(ent, step);
            }

            if (comp.FakeHud != RMCFakeHud.None && time >= comp.FakeHudEnd)
            {
                comp.FakeHud = RMCFakeHud.None;
                RefreshFakeHud(ent);
            }

            foreach (var attacker in comp.Attackers.ToArray())
            {
                MoveAttacker(ent, attacker, frameTime);

                if (time >= attacker.NextProcess)
                {
                    attacker.NextProcess = time + AttackerProcessEvery;
                    ProcessAttacker(ent, attacker);
                }
            }

            if (comp.NextEvent is { } next && time >= next)
            {
                comp.NextEvent = null;
                DoHallucination(ent);
            }

            if (comp.NextSound is { } nextSound && time >= nextSound)
            {
                comp.NextSound = null;
                if (HasComp<ActorComponent>(uid))
                    PlayStrangeSound(ent);
            }

            UpdateFakeInfection(ent);
            UpdateFakeBodies(ent);
            UpdateFakeParasite(ent, frameTime);
            UpdateFakeQueen(ent);

            if (time >= comp.NextTick)
            {
                comp.NextTick = time + comp.TickEvery;
                Tick(ent);
            }
        }
    }

    private void Tick(Entity<RMCHallucinatingComponent> ent)
    {
        var remaining = GetRemaining(ent, out var status);
        if (remaining <= 0 && IsIdle(ent))
        {
            RemCompDeferred<RMCHallucinatingComponent>(ent);
            return;
        }

        if (_mobState.IsDead(ent) || status == null || remaining < ent.Comp.EventThreshold)
            return;

        if (remaining >= status.FakeAttackMinRemaining && _random.Prob(status.FakeAttackChance))
            FakeAttack(ent);

        ent.Comp.NextEvent ??= _timing.CurTime + GetEventDelay(ent, remaining);
        ent.Comp.NextSound ??= _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(20, 50) / (remaining / 25));
    }

    private TimeSpan GetEventDelay(Entity<RMCHallucinatingComponent> ent, float remaining)
    {
        var intensity = MathF.Pow(remaining / ent.Comp.EventDelayScale, ent.Comp.EventDelayExponent);
        var delay = RandomTime(ent.Comp.EventDelayMin, ent.Comp.EventDelayMax) / intensity;
        if (delay < ent.Comp.EventDelayFloor)
            return ent.Comp.EventDelayFloor;

        return delay > ent.Comp.EventDelayCeiling ? ent.Comp.EventDelayCeiling : delay;
    }

    private bool IsIdle(Entity<RMCHallucinatingComponent> ent)
    {
        var comp = ent.Comp;
        var time = _timing.CurTime;
        return comp.FakeHud == RMCFakeHud.None &&
               comp.Attackers.Count == 0 &&
               comp.FakeBodies.Count == 0 &&
               comp.Steps.Count == 0 &&
               comp.Parasite == null &&
               comp.NextQueenStep == null &&
               comp.NextFakeSymptom == null &&
               time >= comp.FakeBlipsEnd;
    }

    private float GetRemaining(EntityUid uid, out RMCHallucinationStatusEffectComponent? status)
    {
        status = null;
        if (!TryComp(uid, out StatusEffectContainerComponent? container) ||
            !_status.TryGetTime(uid, HallucinationStatus, out var time, container))
        {
            return 0;
        }

        TryComp(time.EffectEnt, out status);
        if (time.EndEffectTime is not { } end)
            return float.MaxValue;

        return (float) (end - _timing.CurTime).TotalSeconds;
    }

    private void DoHallucination(Entity<RMCHallucinatingComponent> ent)
    {
        if (!HasComp<ActorComponent>(ent))
            return;

        var remaining = GetRemaining(ent, out var status);
        if (status == null)
            return;

        for (var i = 0; i < MaxEventRolls; i++)
        {
            if (PickEvent(ent, status.Events, remaining) is not { } hallucination)
                return;

            if (!TryHallucinationEvent(ent, hallucination))
                continue;

            var recent = ent.Comp.RecentEvents;
            recent.Insert(0, hallucination);
            if (recent.Count > ent.Comp.RecentEventMemory)
                recent.RemoveRange(ent.Comp.RecentEventMemory, recent.Count - ent.Comp.RecentEventMemory);

            return;
        }
    }

    private EntProtoId? PickEvent(Entity<RMCHallucinatingComponent> ent, List<RMCHallucinationEventWeight> events, float remaining)
    {
        var total = 0f;
        foreach (var entry in events)
        {
            total += GetEventWeight(ent, entry, remaining);
        }

        if (total <= 0)
            return null;

        var roll = _random.NextFloat(total);
        foreach (var entry in events)
        {
            var weight = GetEventWeight(ent, entry, remaining);
            if (weight <= 0)
                continue;

            roll -= weight;
            if (roll < 0)
                return entry.Event;
        }

        return null;
    }

    private float GetEventWeight(Entity<RMCHallucinatingComponent> ent, RMCHallucinationEventWeight entry, float remaining)
    {
        if (remaining < entry.MinRemaining)
            return 0;

        var weight = (float) entry.Weight;
        foreach (var recent in ent.Comp.RecentEvents)
        {
            if (recent == entry.Event)
                weight *= ent.Comp.RepeatWeightMultiplier;
        }

        return weight;
    }

    public bool ForceEvent(EntityUid uid, EntProtoId hallucination)
    {
        var comp = EnsureComp<RMCHallucinatingComponent>(uid);
        comp.ItemEnd = TimeSpan.Zero;
        comp.ImageEnd = TimeSpan.Zero;
        comp.FakeBlipsEnd = TimeSpan.Zero;
        comp.DisguiseEnd = TimeSpan.Zero;
        comp.FakeInfectionEnd = TimeSpan.Zero;
        comp.StrikeEnd = TimeSpan.Zero;
        comp.LurkerEnd = TimeSpan.Zero;
        comp.WeedsEnd = TimeSpan.Zero;
        comp.LightsEnd = TimeSpan.Zero;
        return TryHallucinationEvent((uid, comp), hallucination);
    }

    private bool TryHallucinationEvent(Entity<RMCHallucinatingComponent> ent, EntProtoId hallucination)
    {
        if (GetSingleton(hallucination) is not { } singleton)
            return false;

        var ev = new RMCHallucinateEvent(ent);
        RaiseLocalEvent(singleton, ref ev);
        return ev.Handled;
    }

    private EntityUid? GetSingleton(EntProtoId hallucination)
    {
        if (!_prototypes.HasIndex(hallucination))
            return null;

        if (!_singletons.TryGetValue(hallucination, out var singleton) || TerminatingOrDeleted(singleton))
        {
            singleton = Spawn(hallucination, MapCoordinates.Nullspace);
            _singletons[hallucination] = singleton;
        }

        return singleton;
    }

    private void PlayStrangeSound(Entity<RMCHallucinatingComponent> ent)
    {
        var sound = _random.Pick(ent.Comp.Sounds);
        var coords = OffsetFromVictim(ent, ent.Comp.SoundDistanceMin, ent.Comp.SoundDistanceMax);
        PlayToVictim(ent, sound.Sound, coords);

        if (!sound.Twice)
            return;

        var delay = RandomTime(ent.Comp.SecondSoundMin, ent.Comp.SecondSoundMax);
        AddStep(ent, delay, new RMCHallucinationStep { Sound = sound.Sound, Coordinates = coords });
    }

    private void AddStep(Entity<RMCHallucinatingComponent> ent, TimeSpan delay, RMCHallucinationStep step)
    {
        step.At = _timing.CurTime + delay;
        ent.Comp.Steps.Add(step);
    }

    private void RunStep(Entity<RMCHallucinatingComponent> ent, RMCHallucinationStep step)
    {
        var coords = step.Coordinates is { } stepCoords && stepCoords.IsValid(EntityManager) ? stepCoords : (EntityCoordinates?) null;
        if (step.Coordinates != null && coords == null)
            return;

        if (step.Sound != null)
            PlayToVictim(ent, step.Sound, coords);

        if (step.Message != null && TryComp(ent, out ActorComponent? actor))
            _rmcChat.ChatMessageToOne(ChatChannel.Radio, step.Message, step.Message, default, false, actor.PlayerSession.Channel);

        if (coords is not { } at)
            return;

        if (step.Warning is { } warning)
            FakeWarning(ent, at, warning, step.WarningAbove ?? warning, step.WarningChat);

        if (step.Visual is { } visual)
            SendToVictim(ent, new RMCHallucinationVisualEvent(visual, null, GetNetCoordinates(at), step.VisualDuration));

        if (step.Explosion)
            FakeExplosion(ent, at);
    }

    private void FakeWarning(EntityUid victim, EntityCoordinates coordinates, LocId warning, LocId warningAbove, bool chat)
    {
        var distanceVec = _transform.ToMapCoordinates(coordinates).Position - _transform.GetMapCoordinates(victim).Position;
        var msg = distanceVec.Length() < 1
            ? Loc.GetString(warningAbove)
            : Loc.GetString(warning, ("direction", distanceVec.GetDir().ToString().ToUpperInvariant()));

        _popup.PopupEntity(msg, victim, victim, PopupType.LargeCaution);

        if (!chat || !TryComp(victim, out ActorComponent? actor))
            return;

        msg = $"[bold][font size=24][color=red]\n{msg}\n[/color][/font][/bold]";
        _rmcChat.ChatMessageToOne(ChatChannel.Radio, msg, msg, default, false, actor.PlayerSession.Channel);
    }

    private EntityCoordinates OffsetFromVictim(EntityUid victim, float minDistance, float maxDistance)
    {
        var offset = _random.NextAngle().ToVec() * _random.NextFloat(minDistance, maxDistance);
        return _transform.GetMoverCoordinates(victim).Offset(offset);
    }

    private void PlayToVictim(EntityUid victim, SoundSpecifier sound, EntityCoordinates? coordinates)
    {
        if (coordinates is { } coords && coords.IsValid(EntityManager))
            _audio.PlayStatic(sound, victim, coords);
        else
            _audio.PlayGlobal(sound, victim);
    }

    private bool TryPickFloorInView(EntityUid uid, out EntityCoordinates coords, float minDistance = 0)
    {
        coords = default;
        var xform = Transform(uid);
        if (xform.GridUid is not { } grid ||
            !TryComp(grid, out MapGridComponent? gridComp) ||
            !TryGetLocalPosition(uid, grid, out var victimPosition))
        {
            return false;
        }

        var range = TryComp(uid, out RMCHallucinatingComponent? comp) ? comp.ViewRange : 7;
        var center = _transform.GetWorldPosition(xform);
        var box = Box2.CenteredAround(center, new Vector2(range * 2 + 1, range * 2 + 1));

        var candidates = new List<EntityCoordinates>();
        foreach (var tile in _map.GetTilesIntersecting(grid, gridComp, box))
        {
            if (_turf.IsSpace(tile) || _turf.IsTileBlocked(tile, CollisionGroup.Impassable))
                continue;

            var tileCoords = _map.GridTileToLocal(grid, gridComp, tile.GridIndices);
            if (minDistance > 0 && (tileCoords.Position - victimPosition).Length() < minDistance)
                continue;

            if (!_examine.InRangeUnOccluded(uid, tileCoords, range + 1))
                continue;

            candidates.Add(tileCoords);
        }

        if (candidates.Count == 0)
            return false;

        coords = _random.Pick(candidates);
        return true;
    }

    private bool IsBlocked(Entity<MapGridComponent> grid, Vector2 position, Vector2 delta)
    {
        var edge = position + delta + Vector2.Normalize(delta) * BlockCheckRadius;
        var indices = _map.TileIndicesFor(grid, grid.Comp, new EntityCoordinates(grid, edge));
        return !_map.TryGetTileRef(grid, grid.Comp, indices, out var tileRef) ||
               _turf.IsSpace(tileRef) ||
               _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable);
    }

    private bool TryGetLocalPosition(EntityUid uid, EntityUid grid, out Vector2 position)
    {
        position = default;
        if (Transform(uid).GridUid != grid)
            return false;

        position = Vector2.Transform(_transform.GetWorldPosition(uid), _transform.GetInvWorldMatrix(grid));
        return true;
    }

    private static float ChebyshevDistance(Vector2 a, Vector2 b)
    {
        return MathF.Max(MathF.Abs(a.X - b.X), MathF.Abs(a.Y - b.Y));
    }

    private TimeSpan RandomTime(float min, float max)
    {
        return TimeSpan.FromSeconds(_random.NextFloat(min, max));
    }

    private TimeSpan RandomTime(TimeSpan min, TimeSpan max)
    {
        return RandomTime((float) min.TotalSeconds, (float) max.TotalSeconds);
    }

    private void ShowMessage(EntityUid victim, string message)
    {
        _rmcChat.ChatMessageToOne($"[color=red][bold]{message}[/bold][/color]", victim);
    }

    private void SendToVictim(EntityUid victim, EntityEventArgs ev)
    {
        if (TryComp(victim, out ActorComponent? actor))
            RaiseNetworkEvent(ev, actor.PlayerSession);
    }
}
