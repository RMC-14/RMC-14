using Content.Server.Administration;
using Content.Shared._RMC14.Animations;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Animation;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class AnimationTestCommand : ToolshedCommand
{
    [CommandImplementation("setlayerstate")]
    public void SetLayerState([PipedArgument] EntityUid ent, [CommandArgument] string layer, [CommandArgument] string state)
    {
        const string key = "rmc_toolshed_animation_test";
        var comp = EnsureComp<RMCAnimationComponent>(ent);
#pragma warning disable RA0002
        comp.Animations[new RMCAnimationId(key)] = new RMCAnimation(TimeSpan.FromSeconds(3),
#pragma warning restore RA0002
        [
            new RMCAnimationTrack(layer,
            [
                new RMCKeyFrame(state, 0),
            ]),
        ]);

        EntityManager.Dirty(ent, comp);
        Sys<RMCAnimationSystem>().Play((ent, comp), key);
    }

    [CommandImplementation("flick")]
    public void Flick([PipedArgument] EntityUid ent,
        [CommandArgument] string animationRsiPath,
        [CommandArgument] string animationState,
        [CommandArgument] string defaultRsiPath,
        [CommandArgument] string defaultState)
    {
        var animationRsi = new SpriteSpecifier.Rsi(new ResPath(animationRsiPath), animationState);
        var defaultRsi = new SpriteSpecifier.Rsi(new ResPath(defaultRsiPath), defaultState);
        Sys<RMCAnimationSystem>().Flick(ent, animationRsi, defaultRsi);
    }
}
