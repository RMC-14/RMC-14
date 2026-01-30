using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Animations;

public abstract class SharedRMCAnimationSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    /// <summary>
    ///     Plays an animation with a specific key on an entity.
    /// </summary>
    /// <param name="ent">The entity to play the animation on.</param>
    /// <param name="key">
    ///     The ID of the animation stored in <see cref="RMCAnimationComponent.Animations"/>
    /// </param>
    public void Play(Entity<RMCAnimationComponent?> ent, string key)
    {
        if (_net.IsClient) // TODO RMC14
            return;

        if (!TryGetNetEntity(ent, out var netEnt))
            return;

        var ev = new RMCPlayAnimationEvent(netEnt.Value, new RMCAnimationId(key));
        var filter = Filter.Pvs(ent);
        RaiseNetworkEvent(ev, filter);
    }

    /// <summary>
    ///     Plays an animation on an entity once.
    ///     The duration of the animation will be equal to one loop of the state on its RSI.
    /// </summary>
    /// <param name="ent">The entity to play the animation on.</param>
    /// <param name="animationRsi">The RSI state to use for playing the animation.</param>
    /// <param name="defaultRsi">The RSI state to set when the animation ends.</param>
    /// <param name="layer">
    ///     Which layer to play the animation on. If null, it will choose the first
    ///     layer by default.
    /// </param>
    public void Flick(Entity<RMCAnimationComponent?> ent,
        SpriteSpecifier.Rsi animationRsi,
        SpriteSpecifier.Rsi defaultRsi,
        string? layer = null)
    {
        if (_net.IsClient) // TODO RMC14
            return;

        if (!TryGetNetEntity(ent, out var netEnt))
            return;

        var ev = new RMCFlickEvent(netEnt.Value, animationRsi, defaultRsi, layer);
        var filter = Filter.Pvs(ent);
        RaiseNetworkEvent(ev, filter);
    }

    /// <summary>
    ///     Wrapper around <see cref="Flick"/> which only runs if both
    ///     <see cref="animationRsi"/> and <see cref="defaultRsi"/> are not null.
    /// </summary>
    /// <param name="ent">The entity to play the animation on.</param>
    /// <param name="animationRsi">The RSI state to use for playing the animation.</param>
    /// <param name="defaultRsi">The RSI state to set when the animation ends.</param>
    /// <param name="layer">
    ///     Which layer to play the animation on. If null, it will choose the first
    ///     layer by default.
    /// </param>
    public void TryFlick(Entity<RMCAnimationComponent?> ent,
        SpriteSpecifier.Rsi? animationRsi,
        SpriteSpecifier.Rsi? defaultRsi,
        string? layer = null)
    {
        if (animationRsi == null || defaultRsi == null)
            return;

        Flick(ent, animationRsi, defaultRsi, layer);
    }
}
