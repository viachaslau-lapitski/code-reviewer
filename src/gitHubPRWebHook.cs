#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

using Octokit;

namespace vsl
{
    public static class gitHubPRWebHook
    {
        [FunctionName("gitHubPRWebHook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var data = await GitHubRepo.getPRInfo(req);
            if (!data.IsValid())
            {
                return new BadRequestResult();
            }

            log.LogInformation($"Incoming request {data}");

            var secrets = await VaultClient.GetSecrets($"{data.UserName}-{data.RepoName}");
            var github = new GitHubClient(new ProductHeaderValue("Code-Reviewer"));
            github.Credentials = new Credentials(secrets.repo);

            var prCommits = await github.PullRequest.Commits(data.UserName, data.RepoName, data.Number);
            var bot = new ChatBot(secrets.bot);
            await foreach (var commit in GitHubRepo.MapCommits(github, data, prCommits))
            {
                foreach (var file in commit.Files.Where(f => f.Patch.Length < ChatBot.InputTokens))
                {
                    var review = await bot.getReview(file.Patch);
                    if (review == null)
                    {
                        continue;
                    }

                    var comment = new PullRequestReviewCommentCreate(review, commit.Sha, file.Filename, file.Patch.Position());
                    log.LogInformation($"Added comment {comment.Info()}");
                    await github.PullRequest.ReviewComment.Create(data.UserName, data.RepoName, data.Number, comment);
                }
            }

            return new OkObjectResult(data);
        }
    }
}
