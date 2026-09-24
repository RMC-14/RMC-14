using System.Text.RegularExpressions;
using Content.Server._RMC14.Speech.Components;
using Robust.Shared.Random;
using Content.Server.Speech;

namespace Content.Server._RMC14.Speech.EntitySystems;

public sealed class VulpkaninAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VulpkaninAccentComponent, AccentGetEvent>(OnAccent);
    }
    
    private void OnAccent(EntityUid uid, VulpkaninAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;
        
        message = Regex.Replace(message, "r+", _random.Pick(new List<string> { "rr", "rrr" }));
        message = Regex.Replace(message, "R+", _random.Pick(new List<string> { "RR", "RRR" }));
        
        args.Message = message;
    }
}