using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Basketball;

public sealed class RMCBasketballSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<CourtKey, CourtState> _courts = new();
    private readonly Dictionary<EntityUid, CourtKey> _registrations = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<RMCBasketballHoopComponent, MapInitEvent>(OnHoopMapInit);
        SubscribeLocalEvent<RMCBasketballHoopComponent, ComponentShutdown>(OnHoopShutdown);
        SubscribeLocalEvent<RMCBasketballHoopComponent, StartCollideEvent>(OnHoopStartCollide);
        SubscribeLocalEvent<RMCBasketballHoopComponent, InteractUsingEvent>(OnHoopInteractUsing);

        SubscribeLocalEvent<RMCBasketballScoreboardComponent, MapInitEvent>(OnScoreboardMapInit);
        SubscribeLocalEvent<RMCBasketballScoreboardComponent, ComponentShutdown>(OnScoreboardShutdown);

        SubscribeLocalEvent<RMCBasketballResetComponent, MapInitEvent>(OnResetMapInit);
        SubscribeLocalEvent<RMCBasketballResetComponent, ComponentShutdown>(OnResetShutdown);
        SubscribeLocalEvent<RMCBasketballResetComponent, InteractHandEvent>(OnResetInteractHand);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _courts.Clear();
        _registrations.Clear();
    }

    private void OnHoopMapInit(Entity<RMCBasketballHoopComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient ||
            !TryRegister(ent, ent.Comp.CourtId, out var state))
        {
            return;
        }

        state.Hoops.Add(ent);
    }

    private void OnHoopShutdown(Entity<RMCBasketballHoopComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient)
            return;

        Unregister(ent, state => state.Hoops.Remove(ent));
    }

    private void OnHoopStartCollide(Entity<RMCBasketballHoopComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsClient ||
            args.OurFixtureId != ent.Comp.SensorFixtureId)
        {
            return;
        }

        TryScoreThrownItem(ent, args.OtherEntity);
    }

    private void OnHoopInteractUsing(Entity<RMCBasketballHoopComponent> ent, ref InteractUsingEvent args)
    {
        if (_net.IsClient ||
            args.Handled ||
            !HasComp<ItemComponent>(args.Used) ||
            HasComp<ProjectileComponent>(args.Used))
        {
            return;
        }

        args.Handled = true;

        if (!_hands.TryDrop(args.User, args.Used, Transform(ent).Coordinates))
        {
            return;
        }

        TryDunkItem(ent, args.Used);
    }

    private void OnScoreboardMapInit(Entity<RMCBasketballScoreboardComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient ||
            !TryRegister(ent, ent.Comp.CourtId, out var state))
        {
            return;
        }

        state.Scoreboards.Add(ent);

        var maxScore = Math.Clamp(ent.Comp.MaxScore, 1, 99);
        if (!state.MaxScoreConfigured)
        {
            state.MaxScore = maxScore;
            state.MaxScoreConfigured = true;
        }
        else if (state.MaxScore != maxScore)
        {
            Log.Error(
                $"Basketball scoreboard {ToPrettyString(ent)} has maxScore {maxScore}, " +
                $"but court '{ent.Comp.CourtId}' uses {state.MaxScore}.");
        }

        state.LeftScore = Math.Min(state.LeftScore, state.MaxScore);
        state.RightScore = Math.Min(state.RightScore, state.MaxScore);
        SynchronizeScoreboards(state);
    }

    private void OnScoreboardShutdown(Entity<RMCBasketballScoreboardComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient)
            return;

        Unregister(ent, state => state.Scoreboards.Remove(ent));
    }

    private void OnResetMapInit(Entity<RMCBasketballResetComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient ||
            !TryRegister(ent, ent.Comp.CourtId, out var state))
        {
            return;
        }

        state.ResetButtons.Add(ent);
    }

    private void OnResetShutdown(Entity<RMCBasketballResetComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient)
            return;

        Unregister(ent, state => state.ResetButtons.Remove(ent));
    }

    private void OnResetInteractHand(Entity<RMCBasketballResetComponent> ent, ref InteractHandEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        args.Handled = true;

        if (_timing.CurTime < ent.Comp.NextResetAt)
        {
            _popup.PopupEntity(Loc.GetString("rmc-basketball-reset-cooldown"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (!TryResetCourt(ent))
            return;

        _popup.PopupEntity(Loc.GetString("rmc-basketball-reset"), ent, args.User);
    }

    public bool TryScoreThrownItem(Entity<RMCBasketballHoopComponent> hoop, EntityUid item)
    {
        if (_net.IsClient ||
            !HasComp<ItemComponent>(item) ||
            !TryComp(item, out ThrownItemComponent? thrown) ||
            HasComp<ProjectileComponent>(item))
        {
            return false;
        }

        var attempt = EnsureComp<RMCBasketballShotAttemptComponent>(item);
        if (attempt.Attempted && attempt.ThrownTime == thrown.ThrownTime)
            return false;

        attempt.Attempted = true;
        attempt.ThrownTime = thrown.ThrownTime;

        if (!_random.Prob(Math.Clamp(hoop.Comp.ShotChance, 0, 1)))
        {
            _popup.PopupEntity(Loc.GetString("rmc-basketball-miss"), hoop, PopupType.MediumCaution);
            return false;
        }

        if (!TryAddScore(hoop, hoop.Comp.Side, hoop.Comp.ShotPoints))
            return false;

        ShowScorePopup(hoop, hoop.Comp.Side, hoop.Comp.ShotPoints, false);
        return true;
    }

    public bool TryDunkItem(Entity<RMCBasketballHoopComponent> hoop, EntityUid item)
    {
        if (_net.IsClient ||
            !HasComp<ItemComponent>(item) ||
            HasComp<ProjectileComponent>(item) ||
            !TryAddScore(hoop, hoop.Comp.Side, hoop.Comp.DunkPoints))
        {
            return false;
        }

        ShowScorePopup(hoop, hoop.Comp.Side, hoop.Comp.DunkPoints, true);
        return true;
    }

    public bool TryAddScore(Entity<RMCBasketballHoopComponent> hoop, RMCBasketballTeam side, int points)
    {
        if (_net.IsClient ||
            points <= 0 ||
            !TryGetCourt(hoop, out var state))
        {
            return false;
        }

        switch (side)
        {
            case RMCBasketballTeam.Left:
            {
                var score = Math.Min(state.LeftScore + points, state.MaxScore);
                if (score == state.LeftScore)
                    return false;

                state.LeftScore = score;
                break;
            }
            case RMCBasketballTeam.Right:
            {
                var score = Math.Min(state.RightScore + points, state.MaxScore);
                if (score == state.RightScore)
                    return false;

                state.RightScore = score;
                break;
            }
            default:
                return false;
        }

        SynchronizeScoreboards(state);
        return true;
    }

    public bool TryResetCourt(Entity<RMCBasketballResetComponent> reset)
    {
        if (_net.IsClient ||
            _timing.CurTime < reset.Comp.NextResetAt ||
            !TryGetCourt(reset, out var state))
        {
            return false;
        }

        state.LeftScore = 0;
        state.RightScore = 0;
        SynchronizeScoreboards(state);

        reset.Comp.NextResetAt = _timing.CurTime + reset.Comp.ResetCooldown;
        reset.Comp.Pressed = true;
        Dirty(reset);

        var resetUid = reset.Owner;
        Timer.Spawn(reset.Comp.PressedDuration, () =>
        {
            if (!TryComp(resetUid, out RMCBasketballResetComponent? resetComp))
                return;

            resetComp.Pressed = false;
            Dirty(resetUid, resetComp);
        });

        return true;
    }

    private void ShowScorePopup(
        Entity<RMCBasketballHoopComponent> hoop,
        RMCBasketballTeam side,
        int points,
        bool dunk)
    {
        var sideName = Loc.GetString(side == RMCBasketballTeam.Left
            ? "rmc-basketball-side-left"
            : "rmc-basketball-side-right");
        var message = Loc.GetString(dunk ? "rmc-basketball-dunk" : "rmc-basketball-shot",
            ("side", sideName),
            ("points", points));

        _popup.PopupEntity(message, hoop, PopupType.Medium);
    }

    private bool TryRegister(EntityUid uid, string courtId, out CourtState state)
    {
        state = default!;

        if (!TryGetCourtKey(uid, courtId, out var key))
            return false;

        _registrations[uid] = key;
        state = _courts.GetOrNew(key);
        return true;
    }

    private bool TryGetCourt(EntityUid uid, out CourtState state)
    {
        state = default!;
        if (!_registrations.TryGetValue(uid, out var key) ||
            !_courts.TryGetValue(key, out var found))
        {
            return false;
        }

        state = found;
        return true;
    }

    private bool TryGetCourtKey(EntityUid uid, string courtId, out CourtKey key)
    {
        key = default;
        if (string.IsNullOrWhiteSpace(courtId))
        {
            Log.Error($"Basketball entity {ToPrettyString(uid)} has an empty courtId.");
            return false;
        }

        var xform = Transform(uid);
        var root = xform.GridUid;
        var parent = xform.ParentUid;
        while (root == null && parent.IsValid())
        {
            if (HasComp<MapGridComponent>(parent) || HasComp<MapComponent>(parent))
            {
                root = parent;
                break;
            }

            parent = Transform(parent).ParentUid;
        }

        root ??= xform.MapUid;
        if (root == null)
            return false;

        key = new CourtKey(root.Value, courtId);
        return true;
    }

    private void Unregister(EntityUid uid, Action<CourtState> remove)
    {
        if (!_registrations.Remove(uid, out var key) ||
            !_courts.TryGetValue(key, out var state))
        {
            return;
        }

        remove(state);
        if (state.Hoops.Count == 0 &&
            state.Scoreboards.Count == 0 &&
            state.ResetButtons.Count == 0)
        {
            _courts.Remove(key);
        }
    }

    private void SynchronizeScoreboards(CourtState state)
    {
        foreach (var uid in state.Scoreboards)
        {
            if (!TryComp(uid, out RMCBasketballScoreboardComponent? scoreboard))
                continue;

            scoreboard.LeftScore = state.LeftScore;
            scoreboard.RightScore = state.RightScore;
            Dirty(uid, scoreboard);
        }
    }

    private readonly record struct CourtKey(EntityUid Root, string Id);

    private sealed class CourtState
    {
        public readonly HashSet<EntityUid> Hoops = new();
        public readonly HashSet<EntityUid> Scoreboards = new();
        public readonly HashSet<EntityUid> ResetButtons = new();

        public int LeftScore;
        public int RightScore;
        public int MaxScore = 99;
        public bool MaxScoreConfigured;
    }
}
