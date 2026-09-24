using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using static Content.Shared._RMC14.Mobs.RMCPulseComponent;

namespace Content.Shared._RMC14.Mobs;

public sealed class RMCPulseSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCPulseComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<RMCPulseComponent> ent, ref MobStateChangedEvent args)
    {
        UpdatePulseState(ent);
    }

    /// <summary>
    ///     Gets the raw pulse value in beats per minute for an entity that has RMCPulseComponent.
    /// </summary>
    /// <param name="uid">The entity to get a pulse value from.</param>
    /// <param name="byMachine">True for machine readings (more accurate), false for hand readings (less accurate with ±10 variation).</param>
    /// <returns>Returns the pulse value in bpm, 0 if dead/no pulse.</returns>
    public int TryGetPulseValue(EntityUid uid, bool byMachine)
    {
        if (!TryComp<RMCPulseComponent>(uid, out var pulse))
            return 0;

        UpdatePulseState((uid, pulse));

        return GeneratePulseValue(pulse.State, byMachine);
    }

    /// <summary>
    ///     Gets a localized pulse reading for display. The return value is the display string (e.g. "72 bpm").
    ///     Use <paramref name="bpm"/> when you also need the raw numeric value, or discard it with <c>out _</c> if not needed.
    /// </summary>
    /// <param name="uid">The entity to get a pulse reading from.</param>
    /// <param name="byMachine">True for machine readings (shows numeric bpm), false for hand readings (shows descriptive text).</param>
    /// <param name="bpm">The raw pulse value in beats per minute used to generate the display string. Zero if dead or no pulse.</param>
    /// <returns>A localized string representing the pulse reading.</returns>
    public string TryGetPulseReading(EntityUid uid, bool byMachine, out int bpm)
    {
        bpm = TryGetPulseValue(uid, byMachine);
        return bpm switch
        {
            0 => Loc.GetString("rmc-pulse-bpm", ("value", 0)),
            >= ThreadyPulseThreshold => byMachine
                ? Loc.GetString("rmc-pulse-thready-machine")
                : Loc.GetString("rmc-pulse-thready-hand"),
            _ => Loc.GetString("rmc-pulse-bpm", ("value", bpm))
        };
    }

    private void UpdatePulseState(Entity<RMCPulseComponent> ent)
    {
        var newState = CalculatePulseState(ent);
        if (ent.Comp.State == newState)
            return;

        ent.Comp.State = newState;
        Dirty(ent);
    }

    private PulseState CalculatePulseState(EntityUid uid)
    {
        if (_mobState.IsDead(uid) ||
            !TryComp<BloodstreamComponent>(uid, out var blood) ||
            !_solution.TryGetSolution(uid, blood.BloodSolutionName, out _, out var bloodSol))
        {
            return PulseState.None;
        }

        // Guard against division by zero for uninitialized solutions.
        if (bloodSol.MaxVolume <= FixedPoint2.Zero)
            return PulseState.Normal;

        var bloodPercent = bloodSol.Volume / bloodSol.MaxVolume;
        return bloodPercent <= ThreadyBloodThreshold ? PulseState.Thready : PulseState.Normal;
    }

    private int GeneratePulseValue(PulseState state, bool byMachine)
    {
        var basePulse = state switch
        {
            PulseState.None => 0,
            PulseState.Slow => _random.Next(40, 60),
            PulseState.Normal => _random.Next(60, 90),
            PulseState.Fast => _random.Next(90, 120),
            PulseState.VeryFast => _random.Next(120, 160),
            PulseState.Thready => ThreadyPulseThreshold, // Always shows ">250"
            _ => 0
        };

        var variation = byMachine ? 0 : _random.Next(-10, 11);
        return state is PulseState.None or PulseState.Thready
            ? basePulse
            : Math.Max(0, basePulse + variation);
    }
}
