#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;

namespace vsl
{
    public class GitHubRepo
    {
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

        public async static IAsyncEnumerable<GitHubCommit> MapCommits(GitHubClient client, InputPR data, IReadOnlyList<PullRequestCommit>? list)
        {
            if (list == null)
            {
                yield break;
            }

            foreach (var commit in list)
            {
                yield return await client.Repository.Commit.Get(data.UserName, data.RepoName, commit.Sha);
            }
        }
    }
}