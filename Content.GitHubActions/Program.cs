using Octokit;

var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
var repo = Environment.GetEnvironmentVariable("REPOSITORY");
var prNumberValid = int.TryParse(Environment.GetEnvironmentVariable("PR_NUMBER"), out var prNumber);
var reviewer = Environment.GetEnvironmentVariable("REVIEWER");
var team = Environment.GetEnvironmentVariable("REQUESTED_TEAM");
var tagRem = Environment.GetEnvironmentVariable("TAG_REM");
var tagAdd = Environment.GetEnvironmentVariable("TAG_ADD");
const string owner = "RMC-14";

Console.WriteLine(repo);

var client = new GitHubClient(new ProductHeaderValue("RMC14GITHUBACTIONS"))
{
    Credentials = new Credentials(token),
};

var reviewerTeams = await GetUserTeamsInOrg(owner, reviewer);

if (TargetCheck())
{
    await RemoveLabel(client, owner, repo, prNumber, tagRem, tagAdd);
}
else
{
    Console.WriteLine(
        "Requested Person is not a target, " +
        "Requested Person is not in a target group, " +
        "Requested group is not a target, skipping.");
}

return;

bool TargetCheck()
{
    var targetUsers = new HashSet<string>()
    {
        "aurallianz",
    };

    var targetTeams = new HashSet<string>()
    {
        "maintainers",
    };


    return reviewer != null && targetUsers.Contains(reviewer.ToLower()) ||
           team != null && targetTeams.Contains(team.ToLower()) ||
           reviewerTeams != null && reviewerTeams.Contains("maintainers");
}

static async Task RemoveLabel(GitHubClient client,
    string owner,
    string? repo,
    int prNumber,
    string? tagRem,
    string? tagAdd)
{
    try
    {
        await client.Issue.Labels.RemoveFromIssue(owner, repo, prNumber, tagRem);
        Console.WriteLine($"Removed label {tagRem}, from PR {prNumber}");
    }
    catch (NotFoundException)
    {
        Console.WriteLine($"Label {tagRem}, not found on PR {prNumber}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error removing label: {e}");
        throw;
    }

    if (tagAdd == null)
    {
        Console.WriteLine($"Label to add is null, skipping.");
        return;
    }

    string[] str = [tagAdd];

    try
    {
        await client.Issue.Labels.AddToIssue(owner, repo, prNumber, str);
        Console.WriteLine($"Removed label {tagAdd}, from PR {prNumber}");
    }
    catch (NotFoundException)
    {
        Console.WriteLine($"Label {tagAdd}, not found on PR {prNumber}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error removing label: {e}");
        throw;
    }
}

async Task<HashSet<string>?> GetUserTeamsInOrg(string org, string? userName)
{
    if (userName == null)
    {
        Console.WriteLine($"No user, ignoring team check.");
        return null;
    }

    try
    {
        var allTeams = await client.Organization.Team.GetAll(owner);

        var userTeams = new HashSet<string>();
        foreach (var team in allTeams)
        {
            try
            {
                var isMember = await client.Organization.Team.GetMembershipDetails(team.Id, userName);
                if (isMember != null)
                    userTeams.Add(team.Name);
            }
            catch (NotFoundException nfe)
            {
                Console.WriteLine(nfe);
                continue;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return userTeams;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
