using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Survivor;
using Content.Shared.Body.Events;

namespace Content.Server._RMC14.Marines.ControlComputer;

public sealed class MarineControlComputerSystem : SharedMarineControlComputerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineComponent, BeingGibbedEvent>(OnMarineGibbed);
    }

    private void OnMarineGibbed(Entity<MarineComponent> ent, ref BeingGibbedEvent ev)
    {
        if (HasComp<RMCSurvivorComponent>(ent))
        {
            return;
        }
        // The entity being gibbed is the one that raised the event (ent)
        if (!TryComp(ent, out CommendationReceiverComponent? receiver) ||
            receiver.LastPlayerId == null)
        {
            return;
        }

        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var computerId, out var computer))
        {
            var info = new GibbedMarineInfo
            {
                Name = Name(ent),
                LastPlayerId = receiver.LastPlayerId
            };

            computer.GibbedMarines[receiver.LastPlayerId] = info;
            Dirty(computerId, computer);
        }
    }
}
