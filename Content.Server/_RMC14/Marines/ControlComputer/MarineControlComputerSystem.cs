using Content.Server.Body.Components;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Survivor;

namespace Content.Server._RMC14.Marines.ControlComputer;

public sealed class MarineControlComputerSystem : SharedMarineControlComputerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineComponent, BeingGibbedEvent>(OnMarineGibbed);
    }

    private void OnMarineGibbed(EntityUid uid, MarineComponent component, ref BeingGibbedEvent ev)
    {
        if (HasComp<RMCSurvivorComponent>(uid))
        {
            return;
        }
        // The entity being gibbed is the one that raised the event (uid)
        if (!TryComp(uid, out CommendationReceiverComponent? receiver) ||
            receiver.LastPlayerId == null)
        {
            return;
        }

        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var computerId, out var computer))
        {
            var info = new GibbedMarineInfo
            {
                Name = Name(uid),
                Rank = string.Empty,
                Squad = string.Empty,
                LastPlayerId = receiver.LastPlayerId
            };

            computer.GibbedMarines[receiver.LastPlayerId] = info;
            Dirty(computerId, computer);
        }
    }
}
