using Content.Server.Fax;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using Content.Shared._RMC14.Fax;
using Content.Shared.Mobs.Components;

namespace Content.Server._RMC14.Fax;


public sealed class RMCFaxSystem : EntitySystem
{
    [Dependency] private readonly FaxSystem _faxSystem = default!;
    [Dependency] private readonly FaxecuteSystem _faxecute = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<FaxMachineComponent, RMCFaxCopyMultipleMessage>(OnCopyMultipleButtonPressed);
    }

    private void OnCopyMultipleButtonPressed(EntityUid uid, FaxMachineComponent component, RMCFaxCopyMultipleMessage args)
    {
        if (HasComp<MobStateComponent>(component.PaperSlot.Item))
        {
            _faxecute.Faxecute(uid, component);
        }
        else
        {
            // Call the existing Copy method multiple times instead of reimplementing the logic
            for (int i = 0; i < args.Copies; i++)
            {
                _faxSystem.Copy(uid, component, new FaxCopyMessage());
            }
        }
    }


}
