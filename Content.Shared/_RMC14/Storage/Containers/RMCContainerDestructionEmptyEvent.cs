namespace Content.Shared._RMC14.Storage.Containers;
/// <summary>
/// Called when an ent with RMCContainerEmptyOnDestructionComponent gets destroyed or deleted.
/// If handled it won't automatically dump everything in the containers. If you need something special to do that.
/// </summary>
/// <param name="Handled"></param>
[ByRefEvent]
public record struct RMCContainerDestructionEmptyEvent(bool Handled = false);
