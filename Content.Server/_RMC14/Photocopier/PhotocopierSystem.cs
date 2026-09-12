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
using Content.Shared._RMC14.Photocopier.Events;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Photocopier;


/// <summary>
/// This handles the Photocopier systems for copying and printing
/// </summary>
public sealed class PhotocopierSystem : EntitySystem
{
    private const string PaperSlotId = "Paper";
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotocopierComponent,EntRemovedFromContainerMessage>(OnPaperRemovedFromPhotocopier);

        SubscribeLocalEvent<PhotocopierComponent, CopiedPaperEvent>(OnPaperCopied);
        SubscribeLocalEvent<PhotocopierComponent, EjectPaperEvent>(OnPaperEjected);
    }
    private void OnPaperRemovedFromPhotocopier(EntityUid photocopierId, PhotocopierComponent photocopierComp, EntRemovedFromContainerMessage args)
    {
        var state = new PhotocopierUiState("", false, false);
        _uiSystem.SetUiState(photocopierId, PhotocopierUIKey.Key, state);
    }
    private void OnPaperCopied (EntityUid photocopierId, PhotocopierComponent photocopierComp, CopiedPaperEvent args)
    {
        if(!_container.TryGetContainer(photocopierId, photocopierComp.PaperSlotId, out var container)||
           container.ContainedEntities.Count == 0)
        {
            return;
        }
        var copyEntity=container.ContainedEntities[0];

        if (!TryComp(copyEntity, out MetaDataComponent? metadata) ||
            !TryComp<PaperComponent>(copyEntity, out var paper))
            return;

        TryComp<LabelComponent>(copyEntity, out var labelComponent);
        TryComp<NameModifierComponent>(copyEntity, out var nameMod);

        // TODO: See comment in 'Send()' about not being able to copy whole entities
        var printout = new CopyPrintout(paper.Content,
                                       nameMod?.BaseName ?? metadata.EntityName,
                                       labelComponent?.CurrentLabel,
                                       paper.StampState,
                                       paper.StampedBy,
                                       paper.EditingDisabled);
        photocopierComp.Printout = printout;
        photocopierComp.PrintingCount = args.CopyCount;
        photocopierComp.NextPrintAt = _timing.CurTime + photocopierComp.PrintingTime;

        var paperName = "";

        EnsureComp<MetaDataComponent>(copyEntity, out var metaData);
        paperName = metaData.EntityName;
        if (TryComp<LabelComponent>(copyEntity, out var label) && label.CurrentLabel is string currentLabel)
        {
            paperName = currentLabel;
        }

        var state = new PhotocopierUiState(paperName, true, false);
        _uiSystem.SetUiState(photocopierId, PhotocopierUIKey.Key, state);
    }
    private void OnPaperEjected (EntityUid photocopierId, PhotocopierComponent photocopierComp, EjectPaperEvent args)
    {
        if(!_container.TryGetContainer(photocopierId, photocopierComp.PaperSlotId, out var container)||
           container.ContainedEntities.Count == 0)
        {
            return;
        }
        var sendEntity=container.ContainedEntities[0];
        _container.Remove(sendEntity, container);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PhotocopierComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var photocopier, out var powerReceiverComponent))
        {
            if (!powerReceiverComponent.Powered)
            {
                continue;
            }
            ProcessPrintingAnimation(uid, photocopier);
        }
    }
    private void ProcessPrintingAnimation(EntityUid uid, PhotocopierComponent photocopier)
    {
        if (_timing.CurTime > photocopier.NextPrintAt)
        {
            PrintCopy(uid, photocopier);
            photocopier.PrintingCount--;
            if (photocopier.PrintingCount <= 0)
            {
                var paperName = "";
                var isPaperInserted = false;

                if(_container.TryGetContainer(uid, photocopier.PaperSlotId, out var container)&&
                 container.ContainedEntities.Count != 0)
                {
                    isPaperInserted = true;
                    var paper = container.ContainedEntities[0];
                    EnsureComp<MetaDataComponent>(paper, out var metaData);
                    paperName = metaData.EntityName;
                    if (TryComp<LabelComponent>(paper, out var label) && label.CurrentLabel is string currentLabel)
                    {
                        paperName = currentLabel;
                    }
                }
                var state = new PhotocopierUiState(paperName, isPaperInserted, true);
                _uiSystem.SetUiState(uid, PhotocopierUIKey.Key, state);
                return;
            }

            photocopier.NextPrintAt = _timing.CurTime + photocopier.PrintingTime;
        }
    }
    private void PrintCopy(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component)|| component.PrintingCount == 0 || component.Printout is not CopyPrintout printout)
            return;

        var entityToSpawn = component.PrintPaperId;
        var printed = Spawn(entityToSpawn, Transform(uid).Coordinates);

        if (TryComp<PaperComponent>(printed, out var paper))
        {
            _paperSystem.SetContent((printed, paper), printout.Content);

            // Apply stamps
            if (printout.StampState != null)
            {
                foreach (var stamp in printout.StampedBy)
                {
                    _paperSystem.TryStamp((printed, paper), stamp, printout.StampState);
                }
            }

            paper.EditingDisabled = printout.Locked;
        }
    }
}
