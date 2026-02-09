using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

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

    /// <param name="uid">The entity to get a pulse value from.</param>
    /// <param name="byMachine">True for machine readings (more accurate), false for hand readings (less accurate).</param>
    /// <returns>The pulse value in bpm, or 0 if dead/no pulse.</returns>
    public int GetPulseValue(EntityUid uid, bool byMachine)
    {
        if (!TryComp<RMCPulseComponent>(uid, out var pulse))
            return 0;

        UpdatePulseState((uid, pulse));

        return GetPulseValueFromState(pulse.State, byMachine);
    }

    private void UpdatePulseState(Entity<RMCPulseComponent> ent)
    {
        var newState = CalculatePulseState(ent);
        if (ent.Comp.State != newState)
        {
            ent.Comp.State = newState;
            ent.Comp.LastPulseValue = GetPulseValueFromState(newState, true);
            Dirty(ent);
        }
    }

    private PulseState CalculatePulseState(EntityUid uid)
    {
        if (_mobState.IsDead(uid))
            return PulseState.None;

        if (TryComp<BloodstreamComponent>(uid, out var blood) &&
            _solution.TryGetSolution(uid, blood.BloodSolutionName, out _, out var bloodSol))
        {
            var bloodPercent = bloodSol.MaxVolume > 0 ? bloodSol.Volume / bloodSol.MaxVolume : 0f;
            if (bloodPercent <= 0.4f) // BLOOD_VOLUME_BAD 224
                return PulseState.Thready;
        }

        return PulseState.Normal;
    }

    private int GetPulseValueFromState(PulseState state, bool byMachine)
    {
        var variation = byMachine ? 0 : _random.Next(-10, 10);
        return state switch
        {
            PulseState.None => 0,
            PulseState.Slow => _random.Next(40, 60) + variation,
            PulseState.Normal => _random.Next(60, 90) + variation,
            PulseState.Fast => _random.Next(90, 120) + variation,
            PulseState.VeryFast => _random.Next(120, 160) + variation,
            PulseState.Thready => 250, // ">250"
            _ => 0
        };
    }
}
