using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Emplacements;
using Robust.Client.Player;

namespace Content.Client._RMC14.Emplacements;

public sealed partial class RMCWeaponControllerSystem : RMCSharedWeaponControllerSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public bool TryGetControllingWeapon([NotNullWhen(true)] out EntityUid? weapon)
    {
        var user = _playerManager.LocalEntity;
        weapon = null;

        if (user == null)
            return false;

        return TryGetControlledWeapon(user.Value, out weapon, out _);
    }
}
