using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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


            var token = "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";
            var github = new GitHubClient(new ProductHeaderValue("MyApp"));
            github.Credentials = new Credentials(token);

            var prCommits = await github.PullRequest.Commits(data.UserName, data.RepoName, data.Number);
            await foreach (var commit in GitHubRepo.MapCommits(github, data, prCommits))
            {
                foreach (var file in commit.Files)
                {
                    var comment = new PullRequestReviewCommentCreate("This is a comment in file", commit.Sha, file.Filename, file.Patch.Position());
                    log.LogInformation($"Added comment {comment.Info()}");
                    await github.PullRequest.ReviewComment.Create(data.UserName, data.RepoName, data.Number, comment);
                }
            }

            return new OkObjectResult(data);
        }
    }
}
