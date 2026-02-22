using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
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
    /// Gets the pulse value for an entity that has RMCPulseComponent.
    /// </summary>
    /// <param name="uid">The entity to get a pulse value from.</param>
    /// <param name="byMachine">True for machine readings (more accurate), false for hand readings (less accurate with ±10 variation).</param>
    /// <returns>Returns the pulse value in bpm, 0 if dead/no pulse, and <see cref="ThreadyPulseThreshold"/> for thready pulse.</returns>
    public int GetPulseValue(EntityUid uid, bool byMachine)
    {
        if (!TryComp<RMCPulseComponent>(uid, out var pulse))
            return 0;

        UpdatePulseState((uid, pulse));

        return GeneratePulseValue(pulse.State, byMachine);
    }

    /// <summary>
    /// Converts the pulse value into a localized display string.
    /// Use this method instead of manually formatting pulse values.
    /// </summary>
    /// <param name="pulseValue">The pulse value from <see cref="GetPulseValue"/>.</param>
    /// <param name="byMachine">True for machine readings (shows ">250"), false for hand readings (shows descriptive text).</param>
    /// <returns>A localized string representing the pulse reading.</returns>
    public static string GetPulseLocalizedDisplayString(int pulseValue, bool byMachine)
    {
        return pulseValue switch
        {
            0 => Robust.Shared.Localization.Loc.GetString("rmc-pulse-bpm", ("value", 0)),
            >= ThreadyPulseThreshold => byMachine
                ? Robust.Shared.Localization.Loc.GetString("rmc-pulse-thready-machine")
                : Robust.Shared.Localization.Loc.GetString("rmc-pulse-thready-hand"),
            _ => Robust.Shared.Localization.Loc.GetString("rmc-pulse-bpm", ("value", pulseValue))
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
        if (_mobState.IsDead(uid))
            return PulseState.None;

        if (!TryComp<BloodstreamComponent>(uid, out var blood) ||
            !_solution.TryGetSolution(uid, blood.BloodSolutionName, out _, out var bloodSol) ||
            bloodSol.MaxVolume <= 0)
        {
            return PulseState.Normal;
        }

        var bloodPercent = bloodSol.Volume / bloodSol.MaxVolume;
        return bloodPercent <= ThreadyBloodThreshold ? PulseState.Thready : PulseState.Normal;
    }

    private int GeneratePulseValue(PulseState state, bool byMachine)
    {
        var variation = byMachine ? 0 : _random.Next(-10, 11);
        return state switch
        {
            PulseState.None => 0,
            PulseState.Slow => _random.Next(40, 60) + variation,
            PulseState.Normal => _random.Next(60, 90) + variation,
            PulseState.Fast => _random.Next(90, 120) + variation,
            PulseState.VeryFast => _random.Next(120, 160) + variation,
            PulseState.Thready => ThreadyPulseThreshold, // Always shows ">250"
            _ => 0
        };
    }
}
