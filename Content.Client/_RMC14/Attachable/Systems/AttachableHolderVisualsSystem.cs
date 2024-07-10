using Content.Client._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Attachable.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.Attachable.Systems;

public sealed class AttachableHolderVisualsSystem : EntitySystem
{
    [Dependency] private readonly AttachableHolderSystem _attachableHolderSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttachableHolderVisualsComponent, EntRemovedFromContainerMessage>(OnDetached);
        SubscribeLocalEvent<AttachableHolderVisualsComponent, AttachableHolderAttachablesAlteredEvent>(OnAttachablesAltered);
        
        SubscribeLocalEvent<AttachableVisualsComponent, AppearanceChangeEvent>(OnAttachableAppearanceChange);
    }

    private void OnDetached(Entity<AttachableHolderVisualsComponent> holder, ref EntRemovedFromContainerMessage args)
    {
        if (!HasComp<AttachableVisualsComponent>(args.Entity) || !_attachableHolderSystem.HasSlot(holder.Owner, args.Container.ID))
            return;

        var holderEv = new AttachableHolderAttachablesAlteredEvent(args.Entity, args.Container.ID, AttachableAlteredType.Detached);
        RaiseLocalEvent(holder, ref holderEv);
    }

    private void OnAttachablesAltered(Entity<AttachableHolderVisualsComponent> holder,
        ref AttachableHolderAttachablesAlteredEvent args)
    {
        if (!TryComp(args.Attachable, out AttachableVisualsComponent? attachableComponent))
            return;

        string suffix = "";
        if (attachableComponent.ShowActive && TryComp(args.Attachable, out AttachableToggleableComponent? toggleableComponent) && toggleableComponent.Active)
            suffix = "-on";

        var attachable = new Entity<AttachableVisualsComponent>(args.Attachable, attachableComponent);
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                SetAttachableOverlay(holder, attachable, args.SlotId, suffix);
                break;

            case AttachableAlteredType.Detached:
                RemoveAttachableOverlay(holder, args.SlotId);
                break;

            case AttachableAlteredType.Activated:
                if (!attachableComponent.ShowActive)
                    break;

                SetAttachableOverlay(holder, attachable, args.SlotId, suffix);
                break;

            case AttachableAlteredType.Deactivated:
                if (!attachableComponent.ShowActive)
                    break;

                SetAttachableOverlay(holder, attachable, args.SlotId, suffix);
                break;

            case AttachableAlteredType.Interrupted:
                if (!attachableComponent.ShowActive)
                    break;

                SetAttachableOverlay(holder, attachable, args.SlotId);
                break;
            
            case AttachableAlteredType.AppearanceChanged:
                SetAttachableOverlay(holder, attachable, args.SlotId, suffix);
                break;
        }
    }

    private void RemoveAttachableOverlay(Entity<AttachableHolderVisualsComponent> holder, string slotId)
    {
        if (!holder.Comp.Offsets.ContainsKey(slotId) || !TryComp(holder, out SpriteComponent? spriteComponent))
            return;

        if (!spriteComponent.LayerMapTryGet(slotId, out var index))
            return;

        spriteComponent.LayerMapRemove(slotId);
        spriteComponent.RemoveLayer(index);
    }

    private void SetAttachableOverlay(Entity<AttachableHolderVisualsComponent> holder,
        Entity<AttachableVisualsComponent> attachable,
        string slotId,
        string suffix = "")
    {
        if (!holder.Comp.Offsets.ContainsKey(slotId) ||
            !TryComp(holder, out SpriteComponent? holderSprite))
        {
            return;
        }

        if (!TryComp(attachable, out SpriteComponent? attachableSprite))
            return;

        var rsi = attachableSprite.LayerGetActualRSI(attachable.Comp.Layer)?.Path;
        var state = attachableSprite.LayerGetState(attachable.Comp.Layer).ToString();
        if (attachable.Comp.Rsi is { } rsiPath)
        {
            rsi = rsiPath;
        }

        if (!string.IsNullOrWhiteSpace(attachable.Comp.Prefix))
            state = attachable.Comp.Prefix;

        if (attachable.Comp.IncludeSlotName)
            state += slotId;

        if (!string.IsNullOrWhiteSpace(attachable.Comp.Suffix))
            state += attachable.Comp.Suffix;

        state += suffix;

        var layerData = new PrototypeLayerData()
        {
            RsiPath = rsi.ToString(),
            State = state,
            Offset = holder.Comp.Offsets[slotId] + attachable.Comp.Offset,
            Visible = true,
        };

        if (holderSprite.LayerMapTryGet(slotId, out var index))
        {
            holderSprite.LayerSetData(index, layerData);
            return;
        }

        holderSprite.LayerMapSet(slotId, holderSprite.AddLayer(layerData));
    }
    
    private void OnAttachableAppearanceChange(Entity<AttachableVisualsComponent> attachable, ref AppearanceChangeEvent args)
    {
        if (!attachable.Comp.RedrawOnAppearanceChange ||
            !_attachableHolderSystem.TryGetHolder(attachable.Owner, out var holderUid) ||
            !_attachableHolderSystem.TryGetSlotId(holderUid.Value, attachable.Owner, out var slotId))
        {
            return;
        }
        
        var holderEvent = new AttachableHolderAttachablesAlteredEvent(attachable.Owner, slotId, AttachableAlteredType.AppearanceChanged);
        RaiseLocalEvent(holderUid.Value, ref holderEvent);
    }
}
