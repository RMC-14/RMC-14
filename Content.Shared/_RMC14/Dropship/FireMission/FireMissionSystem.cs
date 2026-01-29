using System.Numerics;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Mortar;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Dropship.FireMission;

public sealed class FireMissionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDropshipWeaponSystem _dropshipWeapon = default!;
    [Dependency] private readonly SharedMortarSystem _mortar = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    ///     Checks if the passed entity is currently performing a fire mission.
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <param name="mission">The <see cref="ActiveFireMissionComponent"/> of the passed entity.</param>
    /// <returns>True if the passed entity is performing a fire mission</returns>
    public bool HasActiveFireMission(EntityUid uid, ActiveFireMissionComponent? mission = null)
    {
        if (!Resolve(uid, ref mission, false))
            return false;

        return mission.CurrentPhase != FireMissionPhase.Cooldown && mission.CurrentPhase != FireMissionPhase.Preparing;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var fireMissionQuery = EntityQueryEnumerator<ActiveFireMissionComponent>();
        while (fireMissionQuery.MoveNext(out var uid, out var fireMission))
        {
            // Mission has ended
            if (fireMission.CurrentPhase == FireMissionPhase.Cooldown)
            {
                if (fireMission.MissionEye != null)
                {
                    if (fireMission.WatchingTerminal is { } watchingTerminal)
                        _dropshipWeapon.TrySetCameraTarget(watchingTerminal, null);

                    Del(fireMission.MissionEye);
                    fireMission.MissionEye = null;
                    Dirty(uid, fireMission);
                }

                if (fireMission.StartTime + fireMission.MissionCooldown < _timing.CurTime)
                {
                    Del(fireMission.TargetCoordinates.EntityId);
                    RemComp<ActiveFireMissionComponent>(uid);
                }

                continue;
            }

            var activeStartTime = fireMission.StartTime + fireMission.StartDelay;

            // Fire mission is still "preparing".
            if (_timing.CurTime < activeStartTime || fireMission.FireMissionData == null)
                continue;

            var startCoordinates = fireMission.TargetCoordinates.Offset(fireMission.Offset);

            // The dropship is approaching the target location, play approaching sound.
            if (fireMission.CurrentPhase == FireMissionPhase.Preparing)
            {
                fireMission.CurrentPhase = FireMissionPhase.Approaching;
                Dirty(uid, fireMission);

                if (fireMission is { WatchingTerminal: { } watchingTerminal, MissionEye: { } missionEye })
                    _dropshipWeapon.TryUpdateCameraTarget(watchingTerminal, missionEye, true);

                _audio.PlayPvs(fireMission.StartSound, startCoordinates);
            }

            // Don't shoot yet. Instead, display warnings to entities near the target location.
            if (_timing.CurTime < activeStartTime + fireMission.WarningDuration)
            {
                var elapsed = _timing.CurTime - activeStartTime;

                foreach (var warning in fireMission.Warnings)
                {
                    if (warning.MessageSent || elapsed < warning.Offset)
                        continue;

                    warning.MessageSent = true;
                    _mortar.PopupWarning(_transform.ToMapCoordinates(startCoordinates), warning.MessageRange, warning.Message, warning.Message + "-above", true);
                }

                continue;
            }

            // Dropship has arrived at the target location.
            if (fireMission.CurrentPhase == FireMissionPhase.Approaching)
            {
                fireMission.CurrentPhase = FireMissionPhase.Firing;
                Dirty(uid, fireMission);
            }

            // Start shooting weapons that have an offset defined at the current step.
            if (_timing.CurTime > activeStartTime + fireMission.WarningDuration + fireMission.StepDelay * fireMission.CurrentStep)
            {
                var strikeVector = fireMission.StrikeVector.ToIntVec();
                var stepOffset = new Vector2(strikeVector.X, strikeVector.Y) * fireMission.CurrentStep;
                var travelOffset = stepOffset + fireMission.Offset;

                if (fireMission.MissionEye is { } missionEye)
                    _transform.SetCoordinates(missionEye, fireMission.TargetCoordinates.Offset(travelOffset));

                foreach (var weaponOffset in fireMission.FireMissionData.Value.WeaponOffsets)
                {
                    if (weaponOffset.Step != fireMission.CurrentStep || weaponOffset.Offset == null)
                        continue;

                    var weaponOffsetRotated = new Vector2(strikeVector.Y, -strikeVector.X) * weaponOffset.Offset.Value;

                    var totalOffset = travelOffset + weaponOffsetRotated;
                    var adjustedCoordinates = fireMission.TargetCoordinates.Offset(totalOffset);

                    _dropshipWeapon.TryFireWeapon(GetEntity(weaponOffset.WeaponId), adjustedCoordinates, DropshipWeaponStrikeType.FireMission);
                }

                // The dropship has finished the fire mission.
                if (fireMission.CurrentStep >= fireMission.MaxSteps)
                    fireMission.CurrentPhase = FireMissionPhase.Cooldown;

                fireMission.CurrentStep++;
                Dirty(uid, fireMission);
            }
        }
    }
}
