using Content.Server.Fax;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Fax.Systems;
using Content.Shared._RMC14.Fax;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Labels.Components;
using Content.Shared.NameModifier;

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
            CopyMultipleWithLoop(uid, component, args.Copies);
        }
    }

    private void CopyMultipleWithLoop(EntityUid uid, FaxMachineComponent component, int copies)
    {
        // Clamped to only allow 1 to 10 to prevent aboose
        copies = Math.Clamp(copies, 1, 10);

        var originalTimeout = component.SendTimeoutRemaining;
        
        component.SendTimeoutRemaining = 0;
        
        for (int i = 0; i < copies; i++)
        {
            _faxSystem.Copy(uid, component, new FaxCopyMessage());
            
            if (i < copies - 1)
                component.SendTimeoutRemaining = 0;
        }
        
    }
}
