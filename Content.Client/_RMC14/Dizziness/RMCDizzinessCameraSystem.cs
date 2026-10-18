using Content.Shared._RMC14.Dizziness;
using Content.Shared.Movement.Systems;
using Robust.Client.Player;

namespace Content.Client._RMC14.Dizziness;

public sealed class RMCDizzinessCameraSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private EntityUid? _wobbling;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var player = _player.LocalEntity;
        var dizzy = TryComp(player, out RMCDizzinessComponent? dizziness) && dizziness.Dizzy;

        if (_wobbling is { } previous && (previous != player || !dizzy))
        {
            _wobbling = null;
            if (TryComp(previous, out EyeComponent? previousEye))
                _contentEye.UpdateEyeOffset((previous, previousEye));
        }

        if (!dizzy || player is not { } uid || !TryComp(uid, out EyeComponent? eye))
            return;

        _wobbling = uid;
        _contentEye.UpdateEyeOffset((uid, eye));
    }
}
