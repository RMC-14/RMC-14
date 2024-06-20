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

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                SetAttachableOverlay(holder.Owner, holder.Comp, attachableComponent, args.SlotId);
                break;

            case AttachableAlteredType.Detached:
                RemoveAttachableOverlay(holder.Owner, holder.Comp, args.SlotId);
                break;

            case AttachableAlteredType.Activated:
                if (!attachableComponent.ShowActive)
                    break;

                SetAttachableOverlay(holder.Owner, holder.Comp, attachableComponent, args.SlotId, "-active");
                break;

            case AttachableAlteredType.Deactivated:
                if (!attachableComponent.ShowActive)
                    break;

                SetAttachableOverlay(holder.Owner, holder.Comp, attachableComponent, args.SlotId);
                break;
        }
    }

    private void RemoveAttachableOverlay(EntityUid holderUid,
        AttachableHolderVisualsComponent holderComponent,
        string slotId)
    {
        if (!holderComponent.Offsets.ContainsKey(slotId) || !TryComp(holderUid, out SpriteComponent? spriteComponent))
            return;

        if (!spriteComponent.LayerMapTryGet(slotId, out var index))
            return;

        spriteComponent.LayerMapRemove(slotId);
        spriteComponent.RemoveLayer(index);
    }

    private void SetAttachableOverlay(EntityUid holderUid,
        AttachableHolderVisualsComponent holderComponent,
        AttachableVisualsComponent attachableComponent,
        string slotId,
        string suffix = "")
    {
        if (!holderComponent.Offsets.ContainsKey(slotId) || !TryComp(holderUid, out SpriteComponent? spriteComponent))
            return;

        if (string.IsNullOrWhiteSpace(attachableComponent.Rsi) || string.IsNullOrWhiteSpace(attachableComponent.Prefix))
            return;

        var layerData = new PrototypeLayerData()
        {
            RsiPath = attachableComponent.Rsi,
            State = attachableComponent.Prefix + slotId + suffix,
            Offset = holderComponent.Offsets[slotId],
            Visible = true
        };

        if (spriteComponent.LayerMapTryGet(slotId, out var index))
        {
            spriteComponent.LayerSetData(index, layerData);
            return;
        }

        spriteComponent.LayerMapSet(slotId, spriteComponent.AddLayer(layerData));
    }
}
