using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Camera.PhotoCamera;

public abstract class SharedRmcPhotoCameraSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;

    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPhotoComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
    }

    private void AfterUIOpen(Entity<RMCPhotoComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (!_uiSystem.HasUi(ent, RMCPhotoUi.Key))
            return;

        if (ent.Comp.ImageData == null)
            return;

        var state = new PhotoBoundUserInterfaceState(ent.Comp.ImageData, ent.Comp.PhotoName);
        _uiSystem.SetUiState(ent.Owner, RMCPhotoUi.Key, state);
    }

    protected bool TryGetCamera(EntityUid uid, [NotNullWhen(true)] out Entity<RMCPhotoCameraComponent>? camera)
    {
        camera = null;

        var activeItem = Hands.GetActiveItem(uid);
        if (activeItem == null || !TryComp(activeItem, out RMCPhotoCameraComponent? cameraComponent))
            return false;

        camera = (activeItem.Value, cameraComponent);
        return true;
    }
}

[Serializable, NetSerializable]
public sealed class PhotoCaptureEvent : EntityEventArgs
{
    public byte[] ImageData;

    public PhotoCaptureEvent(byte[] imageData)
    {
        ImageData = imageData;
    }
}
