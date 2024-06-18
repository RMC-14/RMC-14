using System.Linq;
using Content.Server.Administration;
using Content.Shared._CM14.Marines.Skills;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._CM14.SkillsCommand;

[AdminCommand(AdminFlags.Debug)]
public sealed class SkillsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "skills";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            shell.WriteError(Help);
            return;
        }

        var playerName = args[0];
        if (!_players.TryGetSessionByUsername(playerName, out var player) ||
            player.AttachedEntity is not { } entity)
        {
            shell.WriteError(Loc.GetString("cmd-skills-player-does-not-exist", ("player", playerName)));
            return;
        }

        var skill = args[1].ToLowerInvariant();
        var skillsComp = _entities.GetComponentOrNull<SkillsComponent>(entity);
        if (args.Length == 2)
        {
            if (skillsComp == null)
            {
                shell.WriteError(Loc.GetString("cmd-skills-no-skills", ("player", entity)));
                return;
            }

            var skills = skillsComp.Skills;
            int? level = skill switch
            {
                "antagonist" => skills.Antagonist,
                "construction" => skills.Construction,
                "cqc" => skills.Cqc,
                "domestics" => skills.Domestics,
                "endurance" => skills.Endurance,
                "engineer" => skills.Engineer,
                "execution" => skills.Execution,
                "firearms" => skills.Firearms,
                "fireman" => skills.Fireman,
                "intel" => skills.Intel,
                "jtac" => skills.Jtac,
                "leadership" => skills.Leadership,
                "medical" => skills.Medical,
                "meleeweapons" => skills.MeleeWeapons,
                "navigations" => skills.Navigations,
                "overwatch" => skills.Overwatch,
                "pilot" => skills.Pilot,
                "police" => skills.Police,
                "powerloader" => skills.PowerLoader,
                "research" => skills.Research,
                "smartgun" => skills.Smartgun,
                "specialistweapons" => skills.SpecialistWeapons,
                "surgery" => skills.Surgery,
                "vehicles" => skills.Vehicles,
                _ => null
            };

            if (level == null)
            {
                shell.WriteError(Loc.GetString("cmd-skills-skill-not-found", ("skill", skill)));
                shell.WriteError(Help);
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-skills-level-get",
                ("player", player.Name),
                ("skill", skill),
                ("level", level)));
        }
        else
        {
            skillsComp = _entities.EnsureComponent<SkillsComponent>(entity);
            var skills = skillsComp.Skills;
            var level = int.Parse(args[2]);
            switch (skill)
            {
                case "antagonist":
                    skills = skills with { Antagonist = level };
                    break;
                case "construction":
                    skills = skills with { Construction = level };
                    break;
                case "cqc":
                    skills = skills with { Cqc = level };
                    break;
                case "domestics":
                    skills = skills with { Domestics = level };
                    break;
                case "endurance":
                    skills = skills with { Endurance = level };
                    break;
                case "engineer":
                    skills = skills with { Engineer = level };
                    break;
                case "execution":
                    skills = skills with { Execution = level };
                    break;
                case "firearms":
                    skills = skills with { Firearms = level };
                    break;
                case "fireman":
                    skills = skills with { Fireman = level };
                    break;
                case "intel":
                    skills = skills with { Intel = level };
                    break;
                case "jtac":
                    skills = skills with { Jtac = level };
                    break;
                case "leadership":
                    skills = skills with { Leadership = level };
                    break;
                case "medical":
                    skills = skills with { Medical = level };
                    break;
                case "meleeweapons":
                    skills = skills with { MeleeWeapons = level };
                    break;
                case "navigations":
                    skills = skills with { Navigations = level };
                    break;
                case "overwatch":
                    skills = skills with { Overwatch = level };
                    break;
                case "pilot":
                    skills = skills with { Pilot = level };
                    break;
                case "police":
                    skills = skills with { Police = level };
                    break;
                case "powerloader":
                    skills = skills with { PowerLoader = level };
                    break;
                case "research":
                    skills = skills with { Research = level };
                    break;
                case "smartgun":
                    skills = skills with { Smartgun = level };
                    break;
                case "specialistweapons":
                    skills = skills with { SpecialistWeapons = level };
                    break;
                case "surgery":
                    skills = skills with { Surgery = level };
                    break;
                case "vehicles":
                    skills = skills with { Vehicles = level };
                    break;
                default:
                    shell.WriteError(Loc.GetString("cmd-skills-skill-not-found", ("skill", skill)));
                    shell.WriteError(Help);
                    return;
            }

            var system = _entities.System<SkillsSystem>();
            system.SetSkills((entity, skillsComp), skills);
            shell.WriteLine(Loc.GetString("cmd-skills-level-set",
                ("player", player.Name),
                ("skill", skill),
                ("level", level)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var players = _players.Sessions
                .Select(s => s.Name)
                .OrderBy(n => n);
            return CompletionResult.FromHintOptions(players,
                Loc.GetString("damage-command-arg-type"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                [
                    "antagonist", "construction", "cqc", "domestics", "endurance", "engineer",
                    "execution", "firearms", "fireman", "intel", "jtac", "leadership", "medical",
                    "meleeweapons", "navigations", "overwatch", "pilot", "police", "powerloader",
                    "researcher", "smartgun", "specialistweapons", "surgery", "vehicles"
                ],
                Loc.GetString("cmd-skills-hint-skill"));
        }

        if (args.Length == 3)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-skills-hint-level"));
        }

        return CompletionResult.Empty;
    }
}
