using Content.Shared._RMC14.Body.Zones;
using Content.Shared._RMC14.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Body.Zones;

public sealed class RMCTargetZoneInputSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly RMCTargetZoneSystem _targetZone = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCTargetHead, Handler(zone => zone.CycleHead()))
            .Bind(CMKeyFunctions.RMCTargetRightArm, Handler(zone => zone.CycleLimb(RMCTargetZone.RightArm, RMCTargetZone.RightHand)))
            .Bind(CMKeyFunctions.RMCTargetChest, Handler(_ => RMCTargetZone.Chest))
            .Bind(CMKeyFunctions.RMCTargetLeftArm, Handler(zone => zone.CycleLimb(RMCTargetZone.LeftArm, RMCTargetZone.LeftHand)))
            .Bind(CMKeyFunctions.RMCTargetRightLeg, Handler(zone => zone.CycleLimb(RMCTargetZone.RightLeg, RMCTargetZone.RightFoot)))
            .Bind(CMKeyFunctions.RMCTargetGroin, Handler(_ => RMCTargetZone.Groin))
            .Bind(CMKeyFunctions.RMCTargetLeftLeg, Handler(zone => zone.CycleLimb(RMCTargetZone.LeftLeg, RMCTargetZone.LeftFoot)))
            .Bind(CMKeyFunctions.RMCTargetNext, Handler(zone => zone.Next()))
            .Bind(CMKeyFunctions.RMCTargetPrevious, Handler(zone => zone.Previous()))
            .Register<RMCTargetZoneInputSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<RMCTargetZoneInputSystem>();
    }

    private InputCmdHandler Handler(Func<RMCTargetZone, RMCTargetZone> select)
    {
        return InputCmdHandler.FromDelegate(_ =>
            {
                // input handlers run again every time the client re-predicts, only send the first time
                if (!_timing.IsFirstTimePredicted ||
                    _player.LocalEntity is not { } user ||
                    !HasComp<RMCTargetZoneComponent>(user))
                {
                    return;
                }

                _targetZone.RequestZone(select(_targetZone.GetZone(user)));
            },
            handle: false);
    }
}
