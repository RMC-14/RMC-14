using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Camera.PhotoCamera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPhotoComponent : Component
{
    /// <summary>
    ///     The name of the photo, displayed in the title of the UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PhotoName = "Photo";

    /// <summary>
    ///     The examine text, used by the client to prevent having to wait for the server's identity response.
    /// </summary>
    [DataField]
    public List<string> ExamineText = new ();

    /// <summary>
    ///     The entities visible in the photo.
    /// </summary>
    [DataField]
    public List<EntityInPhoto> EntitiesInPhoto = new ();

    /// <summary>
    ///     The image data, this is not networked and instead requested by a client through the <see cref="RequestStoredPhotoEvent"/> when interacting with a photo.
    /// </summary>
    [DataField]
    public byte[]? ImageData;

    /// <summary>
    ///     The id of the whitelisted player that sent the image, not the player that requested the picture.
    /// </summary>
    [DataField]
    public Guid? RenderedBy;
}
