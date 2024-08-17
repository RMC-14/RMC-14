using Content.Server.Light.ExpendableLightSystem;
using Content.Shared.Tag;
using Content.Server.Light.EntitySystems.ExpendableLightSystem;

namespace Content.Server.Light.EntitySystems

public sealed class FlareLitSystem : EntitySystems
{
  public override void Initialize()
  {
    if (ExpendableLightState.Lit) 
    {
      _tagSystem.AddTag(ent, "Trash");
    }
  }
}
/// ExpendableLightSystem Already marks Fading/Spent as Trash, this is just to add it to Lit Flares
