using Content.Server.Power.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Fax;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;

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
        SubscribeLocalEvent<PhotocopierComponent, DeviceNetworkPacketEvent>(OnPacketReceived);

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
            ProcessInsertingAnimation(uid, frameTime, fax);
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
}
