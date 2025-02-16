// ReSharper disable CheckNamespace
namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingPrototype
{
    // we use this instead of followSkinColor because that isn't implemented
    // and if you implement it every existing marking breaks
    // and somehow no one has noticed this in TWO FUCKING YEARS OF MARKINGS EXISTING!
    // splendid!
    [DataField("RMCFollowSkinColor")]
    public bool RMCFollowSkinColor = true;
}
