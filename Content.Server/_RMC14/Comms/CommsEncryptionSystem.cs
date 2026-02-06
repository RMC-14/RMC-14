using Content.Server._RMC14.Rules;
using Content.Shared._RMC14.Comms;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Comms;

public sealed class CommsEncryptionSystem : SharedCommsEncryptionSystem
{
    [Dependency] private readonly CMDistressSignalRuleSystem _distressSignal = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CommsEncryptionComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<CommsEncryptionComponent> ent, ref ComponentStartup args)
    {
        // Check if we're on a planet (groundside)
        ent.Comp.IsGroundside = _distressSignal.SelectedPlanetMapName != null;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;

        if (_accumulator < 30f) // Check every 30 seconds
            return;

        _accumulator = 0f;

        var query = EntityQueryEnumerator<CommsEncryptionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsGroundside)
                continue;

            // Check grace period
            if (comp.HasGracePeriod && _timing.CurTime < comp.GracePeriodEnd)
                continue;

            comp.HasGracePeriod = false;

            // Degrade clarity
            if (comp.Clarity > comp.MinClarity)
            {
                comp.Clarity = Math.Max(comp.Clarity - comp.DegradationAmount, comp.MinClarity);
                Dirty(uid, comp);
            }
        }
    }
}
