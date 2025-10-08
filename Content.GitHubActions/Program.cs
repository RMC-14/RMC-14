using System.Security.Cryptography.X509Certificates;
using Octokit;

var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
var repo = Environment.GetEnvironmentVariable("REPOSITORY");
var prNumberValid = int.TryParse(Environment.GetEnvironmentVariable("PR_NUMBER"), out var prNumber);
var reviewer = Environment.GetEnvironmentVariable("REQUESTED_REVIEWER");
var team =  Environment.GetEnvironmentVariable("REQUESTED_TEAM");
var tag =  Environment.GetEnvironmentVariable("TAG_TO_REMOVE");
const string owner = "RMC-14";


var client = new GitHubClient(new ProductHeaderValue("RMC14GITHUBACTIONS"))
{
    Credentials = new Credentials(token),
};

if (TargetCheck(reviewer, team))
{
    await RemoveLabel(client, owner, repo, prNumber, tag);
}
else
{
    Console.WriteLine("Requested Person is not in the target group/is not a target, skipping.");
}

return;

static bool TargetCheck(string? requestedReviewer, string? requestedTeam)
{
    var targetUsers = new HashSet<string>()
    {
        "aurallianz",
    };

    var targetTeams = new HashSet<string>()
    {
        "maintainers",
    };

    return requestedReviewer != null && targetUsers.Contains(requestedReviewer.ToLower()) || requestedTeam != null && targetTeams.Contains(requestedTeam.ToLower());
}

static async Task RemoveLabel(GitHubClient client, string owner, string? repo, int prNumber, string? tag)
{
    try
    {
        await client.Issue.Labels.RemoveFromIssue(owner, repo, prNumber, tag);
        Console.WriteLine($"Removed label {tag}, from PR {prNumber}");
    }
    catch (NotFoundException)
    {
        Console.WriteLine($"Label {tag}, not found on PR {prNumber}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error removing label: {e}");
        throw;
    }
}
