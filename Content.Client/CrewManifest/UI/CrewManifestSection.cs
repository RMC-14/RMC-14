using Content.Shared.CrewManifest;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Roles;
using Robust.Shared.Utility; // RMC14

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestSection : BoxContainer
{
    // RMC14 start
    private static readonly SpriteSpecifier.Rsi SquadBackgroundRsi = new(new ResPath("_RMC14/Interface/cm_job_icons.rsi"), "hudsquad");

    public CrewManifestSection(
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        string sectionName,
        List<CrewManifestEntry> entries)
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        AddChild(new Label()
        {
            StyleClasses = { "LabelBig" },
            Text = Loc.GetString(sectionName),
        });

        var gridContainer = new GridContainer()
        {
            HorizontalExpand = true,
            Columns = 2,
        };

        AddChild(gridContainer);

        foreach (var entry in entries)
        {
            var name = new RichTextLabel()
            {
                HorizontalExpand = true,
            };
            name.SetMessage(entry.Name);

            var titleContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            var title = new RichTextLabel();
            title.SetMessage(entry.JobTitle);

            if (prototypeManager.TryIndex<JobIconPrototype>(entry.JobIcon, out var jobIcon))
            {
                var icon = new TextureRect()
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center,
                    Texture = spriteSystem.Frame0(jobIcon.Icon),
                    Margin = new Thickness(0, 0, 4, 0)
                };

                // Add icon as a child of the background if squad color is applicable
                // Otherwise, simply add the icon to the title container
                if (entry.SquadColor != null)
                {
                    var background = new TextureRect()
                    {
                        TextureScale = new Vector2(2, 2),
                        VerticalAlignment = VAlignment.Center,
                        Texture = spriteSystem.Frame0(SquadBackgroundRsi),
                        Margin = new Thickness(0, 0, 0, 0)
                    };

                    background.ModulateSelfOverride = entry.SquadColor;
                    titleContainer.AddChild(background);
                    background.AddChild(icon);
                }
                else
                {
                    titleContainer.AddChild(icon);
                }

                titleContainer.AddChild(title);
            }
            else
            {
                titleContainer.AddChild(title);
            }

            gridContainer.AddChild(name);
            gridContainer.AddChild(titleContainer);
        }
    }
    // RMC14 end

    public CrewManifestSection(
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        DepartmentPrototype section,
        List<CrewManifestEntry> entries)
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        AddChild(new Label()
        {
            StyleClasses = { "LabelBig" },
            Text = Loc.GetString(section.Name)
        });

        var gridContainer = new GridContainer()
        {
            HorizontalExpand = true,
            Columns = 2
        };

        AddChild(gridContainer);

        foreach (var entry in entries)
        {
            var name = new RichTextLabel()
            {
                HorizontalExpand = true,
            };
            name.SetMessage(entry.Name);

            var titleContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            var title = new RichTextLabel();
            title.SetMessage(entry.JobTitle);


            if (prototypeManager.TryIndex<JobIconPrototype>(entry.JobIcon, out var jobIcon))
            {
                var icon = new TextureRect()
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center,
                    Texture = spriteSystem.Frame0(jobIcon.Icon),
                    Margin = new Thickness(0, 0, 4, 0)
                };

                titleContainer.AddChild(icon);
                titleContainer.AddChild(title);
            }
            else
            {
                titleContainer.AddChild(title);
            }

            gridContainer.AddChild(name);
            gridContainer.AddChild(titleContainer);
        }
    }
}
