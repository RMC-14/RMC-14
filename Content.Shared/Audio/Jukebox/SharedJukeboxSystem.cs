using Robust.Shared.Audio.Systems;

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    [Dependency] protected SharedAudioSystem Audio = default!;
}
