namespace Content.Shared.Weapons.Ranged.Events;

[ByRefEvent]
public readonly record struct UpdateClientAmmoEvent(int AritifialIncrease = 0); //RMC14, added the parameter to update ammo count, when ammo is taken because of something happening serverside
