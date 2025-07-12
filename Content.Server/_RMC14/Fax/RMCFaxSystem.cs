using Content.Server.Fax;
using Content.Server.Administration.Logs;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using Content.Shared._RMC14.Fax;
using Content.Shared._RMC14.Fax.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Labels.Components;
using Content.Shared.NameModifier.Components;
using Content.Shared.Database;

namespace Content.Server._RMC14.Fax;

/// <summary>
/// RMC-specific fax system that handles faxecuting mobs when they're in fax machines.
/// This system extends the base fax functionality with RMC-specific behavior.
/// </summary>
public sealed class RMCFaxSystem : EntitySystem
{
    [Dependency] private readonly RMCFaxecuteSystem _faxecute = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly FaxSystem _faxSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to fax copy and send events to handle RMC-specific logic
        SubscribeLocalEvent<FaxMachineComponent, FaxCopyMessage>(OnCopyButtonPressed, before: new[] { typeof(FaxSystem) });
        SubscribeLocalEvent<FaxMachineComponent, RMCFaxCopyMultipleMessage>(OnCopyMultipleButtonPressed, before: new[] { typeof(FaxSystem) });
        SubscribeLocalEvent<FaxMachineComponent, FaxSendMessage>(OnSendButtonPressed, before: new[] { typeof(FaxSystem) });
    }

    private void OnCopyButtonPressed(EntityUid uid, FaxMachineComponent component, FaxCopyMessage args)
    {
        if (HasComp<MobStateComponent>(component.PaperSlot.Item))
        {
            _faxecute.Faxecute(uid, component); // when button pressed it will hurt the mob.
            args.Handled = true;
        }
    }

    private void OnCopyMultipleButtonPressed(EntityUid uid, FaxMachineComponent component, RMCFaxCopyMultipleMessage args)
    {
        if (HasComp<MobStateComponent>(component.PaperSlot.Item))
        {
            _faxecute.Faxecute(uid, component); // when button pressed it will hurt the mob.
            args.Handled = true;
        }
        else
        {
            CopyMultiple(uid, component, args);
            args.Handled = true;
        }
    }

    /// <summary>
    /// RMC-specific implementation: Copies the paper in the fax multiple times.
    /// A timeout is set after copying, which is shared by the send button.
    /// </summary>
    private void CopyMultiple(EntityUid uid, FaxMachineComponent component, RMCFaxCopyMultipleMessage args)
    {
        if (component.SendTimeoutRemaining > 0)
            return;

        var sendEntity = component.PaperSlot.Item;
        if (sendEntity == null)
            return;

        if (!TryComp(sendEntity, out MetaDataComponent? metadata) ||
            !TryComp<PaperComponent>(sendEntity, out var paper))
            return;

        TryComp<LabelComponent>(sendEntity, out var labelComponent);
        TryComp<NameModifierComponent>(sendEntity, out var nameMod);

        // TODO: See comment in upstream 'Send()' about not being able to copy whole entities
        var printout = new FaxPrintout(paper.Content,
                                       nameMod?.BaseName ?? metadata.EntityName,
                                       labelComponent?.CurrentLabel,
                                       metadata.EntityPrototype?.ID ?? component.PrintPaperId,
                                       paper.StampState,
                                       paper.StampedBy,
                                       paper.EditingDisabled);

        // Add the specified number of copies to the queue
        for (int i = 0; i < args.Copies; i++)
        {
            component.PrintingQueue.Enqueue(printout);
        }
        component.SendTimeoutRemaining += component.SendTimeout;

        // Don't play component.SendSound - it clashes with the printing sound, which
        // will start immediately.

        // Update UI using reflection to access private method
        var updateMethod = typeof(FaxSystem).GetMethod("UpdateUserInterface", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        updateMethod?.Invoke(_faxSystem, new object[] { uid, component });

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"RMC user added copy x{args.Copies} job to \"{component.FaxName}\" {ToPrettyString(uid):tool} " +
            $"of {ToPrettyString(sendEntity):subject}: {printout.Content}");
    }

    private void OnSendButtonPressed(EntityUid uid, FaxMachineComponent component, FaxSendMessage args)
    {
        if (HasComp<MobStateComponent>(component.PaperSlot.Item))
        {
            _faxecute.Faxecute(uid, component); // when button pressed it will hurt the mob.
            args.Handled = true;
        }
    }
}
