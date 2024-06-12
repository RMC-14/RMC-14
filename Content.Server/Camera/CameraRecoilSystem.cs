using System.Numerics;
using Content.Shared.Camera;

namespace Content.Server.Camera;

public sealed class CameraRecoilSystem : SharedCameraRecoilSystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void KickCamera(EntityUid euid, Vector2 kickback, CameraRecoilComponent? component = null)
    {
        if (!Resolve(euid, ref component, false))
            return;

        RaiseNetworkEvent(new CameraKickEvent(GetNetEntity(euid), kickback), euid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<EyeComponent, CameraRecoilComponent>();
        while (query.MoveNext(out var uid, out var eye, out var recoil))
        {
            // apparently this is required for aim to work properly
            _eye.SetOffset(uid, recoil.BaseOffset, eye);
        }
    }
}
