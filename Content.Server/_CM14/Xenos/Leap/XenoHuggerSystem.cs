using Content.Server.GameTicking;
using Content.Shared._CM14.Xenos.Hugger;
using Robust.Shared.Player;

namespace Content.Server._CM14.Xenos.Leap;

public sealed class XenoHuggerSystem : SharedXenoHuggerSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    protected override void HuggerLeapHit(Entity<XenoHuggerComponent> hugger)
    {
        if (TryComp(hugger, out ActorComponent? actor))
            _gameTicker.SpawnObserver(actor.PlayerSession);
    }
}
