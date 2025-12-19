using Content.Server.Power.Components;
using Content.Shared.Database;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.NameModifier.Components;
using Content.Shared.Paper;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Content.Shared._RMC14.Photocopier;

namespace Content.Server._RMC14.Photocopier;


/// <summary>
/// This handles the Photocopier systems for copying and printing
/// </summary>
public sealed class PhotocopierSystem : EntitySystem
{
    private const string PaperSlotId = "Paper";
    public override void Initialize()
    {
        base.Initialize();

        // Hooks
        SubscribeLocalEvent<PhotocopierComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PhotocopierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PhotocopierComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<PhotocopierComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<PhotocopierComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<PhotocopierComponent, PowerChangedEvent>(OnPowerChanged);

        // Interaction
        SubscribeLocalEvent<PhotocopierComponent, InteractUsingEvent>(OnInteractUsing);

        // UI
        SubscribeLocalEvent<PhotocopierComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<PhotocopierComponent, FaxFileMessage>(OnFileButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, FaxCopyMessage>(OnCopyButtonPressed);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PhotocopierComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var fax, out var receiver))
        {
            if (!receiver.Powered)
                continue;

            ProcessPrintingAnimation(uid, frameTime, fax);
            ProcessSendingTimeout(uid, frameTime, fax);
        }
    }

    private void ProcessPrintingAnimation(EntityUid uid, float frameTime, PhotocopierComponent comp)
    {
        if (comp.PrintingTimeRemaining > 0)
        {
            comp.PrintingTimeRemaining -= frameTime;
            UpdateAppearance(uid, comp);

            var isAnimationEnd = comp.PrintingTimeRemaining <= 0;
            if (isAnimationEnd)
            {
                SpawnPaperFromQueue(uid, comp);
                UpdateUserInterface(uid, comp);
            }

            return;
        }

        if (comp.PrintingQueue.Count > 0)
        {
            comp.PrintingTimeRemaining = comp.PrintingTime;
            _audioSystem.PlayPvs(comp.PrintSound, uid);
        }
    }
 private void OnComponentInit(EntityUid uid, FaxMachineComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, PaperSlotId, component.PaperSlot);
        UpdateAppearance(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, FaxMachineComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PaperSlot);
    }

    private void OnMapInit(EntityUid uid, FaxMachineComponent component, MapInitEvent args)
    {
        // Load all faxes on map in cache each other to prevent taking same name by user created fax
        Refresh(uid, component);
    }

    private void OnItemSlotChanged(EntityUid uid, FaxMachineComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.PaperSlot.ID)
            return;

        var isPaperInserted = component.PaperSlot.Item.HasValue;
        if (isPaperInserted)
        {
            component.InsertingTimeRemaining = component.InsertionTime;
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, true);
        }

        UpdateUserInterface(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, PhotocopierComponent component, ref PowerChangedEvent args)
    {
        var isInsertInterrupted = !args.Powered && component.InsertingTimeRemaining > 0;
        if (isInsertInterrupted)
        {
            component.InsertingTimeRemaining = 0f; // Reset animation

            // Drop from slot because animation did not play completely
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, false);
            _itemSlotsSystem.TryEject(uid, component.PaperSlot, null, out var _, true);
        }

        var isPrintInterrupted = !args.Powered && component.PrintingTimeRemaining > 0;
        if (isPrintInterrupted)
        {
            component.PrintingTimeRemaining = 0f; // Reset animation
        }

        if (isInsertInterrupted || isPrintInterrupted)
            UpdateAppearance(uid, component);

        _itemSlotsSystem.SetLock(uid, component.PaperSlot, !args.Powered); // Lock slot when power is off
    }

    private void OnToggleInterface(EntityUid uid, FaxMachineComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnFileButtonPressed(EntityUid uid, FaxMachineComponent component, FaxFileMessage args)
    {
        args.Label = args.Label?[..Math.Min(args.Label.Length, FaxFileMessageValidation.MaxLabelSize)];
        args.Content = args.Content[..Math.Min(args.Content.Length, FaxFileMessageValidation.MaxContentSize)];
        PrintFile(uid, component, args);
    }

    private void OnCopyButtonPressed(EntityUid uid, FaxMachineComponent component, FaxCopyMessage args)
    {
        if (HasComp<MobStateComponent>(component.PaperSlot.Item))
            _faxecute.Faxecute(uid, component); // when button pressed it will hurt the mob.
        else
            Copy(uid, component, args);
    }
    private void UpdateAppearance(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (TryComp<FaxableObjectComponent>(component.PaperSlot.Item, out var faxable))
            component.InsertingState = faxable.InsertingState;


        if (component.InsertingTimeRemaining > 0)
        {
            _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Inserting);
            Dirty(uid, component);
        }
        else if (component.PrintingTimeRemaining > 0)
            _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Printing);
        else
            _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Normal);
    }
    private void UpdateUserInterface(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var isPaperInserted = component.PaperSlot.Item != null; ;
        var canCopy = isPaperInserted &&
                      component.SendTimeoutRemaining <= 0 &&
                      component.InsertingTimeRemaining <= 0;
        var state = new FaxUiState( canCopy, isPaperInserted );
        _userInterface.SetUiState(uid, FaxUiKey.Key, state);
    }
    public void PrintFile(EntityUid uid, PhotocopierComponent component, FaxFileMessage args)
    {
        var prototype = args.OfficePaper ? component.PrintOfficePaperId : component.PrintPaperId;

        var name = Loc.GetString("fax-machine-printed-paper-name");

        var printout = new FaxPrintout(args.Content, name, args.Label, prototype);
        component.PrintingQueue.Enqueue(printout);
        component.SendTimeoutRemaining += component.SendTimeout;

        UpdateUserInterface(uid, component);

        // Unfortunately, since a paper entity does not yet exist, we have to emulate what LabelSystem will do.
        var nameWithLabel = (args.Label is { } label) ? $"{name} ({label})" : name;
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} " +
            $"added print job to \"{component.FaxName}\" {ToPrettyString(uid):tool} " +
            $"of {nameWithLabel}: {args.Content}");
    }
    /// <summary>
    ///     Copies the paper in the fax. A timeout is set after copying,
    /// </summary>
    public void Copy(EntityUid uid,PhotocopierComponent? component, FaxCopyMessage args)
    {
        if (!Resolve(uid, ref component))
            return;

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

        // TODO: See comment in 'Send()' about not being able to copy whole entities
        var printout = new FaxPrintout(paper.Content,
                                       nameMod?.BaseName ?? metadata.EntityName,
                                       labelComponent?.CurrentLabel,
                                       metadata.EntityPrototype?.ID ?? component.PrintPaperId,
                                       paper.StampState,
                                       paper.StampedBy,
                                       paper.EditingDisabled);

        component.PrintingQueue.Enqueue(printout);
        component.SendTimeoutRemaining += component.SendTimeout;

        // Don't play component.SendSound - it clashes with the printing sound, which
        // will start immediately.

        UpdateUserInterface(uid, component);

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} " +
            $"added copy job to \"{component.FaxName}\" {ToPrettyString(uid):tool} " +
            $"of {ToPrettyString(sendEntity):subject}: {printout.Content}");
    }
}
