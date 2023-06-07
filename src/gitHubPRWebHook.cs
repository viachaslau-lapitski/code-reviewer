#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

using Octokit;
using System.Collections.Generic;

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

            var secrets = await SecretsService.GetSecrets(data.InstallationId);
            var github = new GitHubClient(new ProductHeaderValue("Code-Reviewer"));
            github.Credentials = new Credentials(secrets.repo);
            var repo = new GitHubRepo(github, log, secrets.bot, data);

            var prCommits = await github.PullRequest.Commits(data.UserName, data.RepoName, data.Number);
            var tasks = prCommits?.Select(commit => repo.ProcessCommit(commit)) ?? new List<Task>();
            await Task.WhenAll(tasks);

            return new OkObjectResult(data);
        }
    }
}
