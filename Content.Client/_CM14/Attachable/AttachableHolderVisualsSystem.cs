using Content.Client._CM14.Attachable.Components;
using Content.Shared._CM14.Attachable.Events;
using Robust.Client.GameObjects;

namespace Content.Client._CM14.Attachable;

public sealed class AttachableHolderVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttachableHolderVisualsComponent, AttachableHolderAttachablesAlteredEvent>(OnAttachablesAltered);
    }

    private void OnAttachablesAltered(Entity<AttachableHolderVisualsComponent> holder,
        ref AttachableHolderAttachablesAlteredEvent args)
    {
        if (!TryComp(args.Attachable, out AttachableVisualsComponent? attachableComponent))
            return;

        var attachable = new Entity<AttachableVisualsComponent>(args.Attachable, attachableComponent);
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                SetAttachableOverlay(holder, attachable, args.SlotId);
                break;

            case AttachableAlteredType.Detached:
                RemoveAttachableOverlay(holder, args.SlotId);
                break;

            case AttachableAlteredType.Activated:
                if (!attachableComponent.ShowActive)
                    break;

                SetAttachableOverlay(holder, attachable, args.SlotId, "-on");
                break;

            case AttachableAlteredType.Deactivated:
                if (!attachableComponent.ShowActive)
                    break;

                SetAttachableOverlay(holder, attachable, args.SlotId);
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

            if (attachable.Comp.Prefix == null)
                state = slotId;
        }

        if (!string.IsNullOrWhiteSpace(attachable.Comp.Prefix))
            state = attachable.Comp.Prefix + state;

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
}
