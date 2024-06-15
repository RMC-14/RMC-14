using Content.Client.Movement.Systems;
using Content.Shared._CM14.Scoping;
using Robust.Client.GameObjects;

namespace Content.Client._CM14.Scoping;

public sealed class ScopeSystem : SharedScopeSystem
{
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly EyeSystem _eye = default!;

    protected override void StartScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
    }

    protected override void StopScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        _contentEye.ResetZoom(user);
        _eye.SetTarget(user, null);
    }
}
