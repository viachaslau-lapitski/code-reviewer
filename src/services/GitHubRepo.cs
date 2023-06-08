#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Octokit;

namespace vsl
{
    public class GitHubRepo
    {
        private GitHubClient _github;
        private ILogger _log;
        private ChatBot _bot;
        private InputPR _inputPR;

        public GitHubRepo(GitHubClient github, ILogger log, string botKey, InputPR inputPR)
        {
            _github = github;
            _log = log;
            _bot = new ChatBot(botKey); ;
            _inputPR = inputPR;
        }

        public async static Task<InputPR> getPRInfo(HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var obj = JObject.Parse(requestBody);
            return new InputPR()
            {
                Number = int.TryParse(obj.SelectToken("$.number")?.ToString(), out int parsedNumber) ? parsedNumber : 0,
                UserName = obj.SelectToken("$.pull_request.user.login")?.ToString(),
                RepoName = obj.SelectToken("$.pull_request.head.repo.name")?.ToString(),
                InstallationId = int.TryParse(obj.SelectToken("$.installation.id")?.ToString(), out int parsedId) ? parsedId : 0,
            };
        }

        public async static Task<string> getInstallationToken(string jwt, int installationId)
        {
            var github = new GitHubClient(new ProductHeaderValue("MyApp"));
            github.Credentials = new Credentials(jwt, AuthenticationType.Bearer);

            var app = await github.GitHubApps.GetCurrent();
            var response = await github.GitHubApps.CreateInstallationToken(38264142);
            return response.Token;
        }

        public async Task ProcessCommit(PullRequestCommit prCommit)
        {
            _log.LogInformation($"processing commit {prCommit.Sha} for {_inputPR.UserName}:{_inputPR.RepoName}");
            var commit = await _github.Repository.Commit.Get(_inputPR.UserName, _inputPR.RepoName, prCommit.Sha);
            var tasks = commit.Files.Where(f => f.Patch.Length < ChatBot.InputTokens).Select(x => ProcessCommitFile(x, prCommit.Sha));
            await Task.WhenAll(tasks);
        }

        private async Task ProcessCommitFile(GitHubCommitFile file, string commitId)
        {
            _log.LogInformation($"processing commit file sha: {file.Sha} filename: {file.Filename} for commit {commitId}");
            var reviews = await _bot.getReviews(file.Patch);
            foreach (var review in reviews)
            {
                var comment = new PullRequestReviewCommentCreate(review.Comment, commitId, file.Filename, review.Position);
                _log.LogInformation($"Added comment {comment.Info()}");
                try
                {
                    await _github.PullRequest.ReviewComment.Create(_inputPR.UserName, _inputPR.RepoName, _inputPR.Number, comment);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, $"Error amid add comment for sha: {file.Sha}, filename: {file.Filename}, commit {commitId}");
                }
            }
        }
    }
}