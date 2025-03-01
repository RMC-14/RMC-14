using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;

namespace Content.Server.Ghost.Roles;

public sealed class RMCGhostRoleSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostRoleRaffleComponent, GhostRoleRaffleEvent>(OnGhostRoleRaffle);
    }

    /// <summary>
    ///     Subtract the current round duration from the time requirement and set that as the raffle countdown.
    /// </summary>
    private void OnGhostRoleRaffle(Entity<GhostRoleRaffleComponent> ent, ref GhostRoleRaffleEvent args)
    {
        var timeUntilRequirement = Math.Clamp(args.RoundTimeRequirement - _gameTicker.RoundDuration().TotalSeconds, args.CountDown.TotalSeconds, args.RoundTimeRequirement);
        args.CountDown = args.CountDown += TimeSpan.FromSeconds(timeUntilRequirement);
        args.Handled = true;
    }
}

/// <summary>
///     Raised when a raffle is being initiated
/// </summary>
[ByRefEvent]
public record struct GhostRoleRaffleEvent(TimeSpan CountDown, float RoundTimeRequirement, bool Handled = false);
