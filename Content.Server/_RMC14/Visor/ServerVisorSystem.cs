using System.Linq;
using Content.Server.PowerCell;
using Content.Shared._RMC14.Visor;
using Content.Shared.Examine;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;

namespace Content.Server._RMC14.Visor;

public sealed class ServerVisorSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CycleableVisorComponent, ExaminedEvent>(OnCycleableVisorExamined);
    }

    private void OnCycleableVisorExamined(Entity<CycleableVisorComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CycleableVisorComponent)))
        {
            if (ent.Comp.CurrentVisor != null)
            {
                if (ent.Comp.CurrentVisor.Value < 0 || ent.Comp.CurrentVisor.Value >= ent.Comp.Containers.Count)
                    return;

                var currentId = ent.Comp.Containers[ent.Comp.CurrentVisor.Value];

                if (!_container.TryGetContainer(ent, currentId, out var currentContainer))
                    return;

                var visorEntity = currentContainer.ContainedEntities.FirstOrDefault();
                args.PushMarkup(Loc.GetString("rmc-visor-down", ("visor", visorEntity)));

                // Show power cell charge if the visor has one
                if (visorEntity != null && HasComp<PowerCellSlotComponent>(visorEntity))
                {
                    if (_powerCell.TryGetBatteryFromSlot(visorEntity, out var battery))
                    {
                        var charge = battery.CurrentCharge / battery.MaxCharge * 100;
                        args.PushMarkup(Loc.GetString("power-cell-component-examine-details", ("currentCharge", $"{charge:F0}")));
                    }
                    else
                    {
                        args.PushMarkup(Loc.GetString("power-cell-component-examine-details-no-battery"));
                    }
                }
            }

            args.PushMarkup("Use a [color=cyan]screwdriver[/color] on this to take out any visors!");
        }
    }
}
