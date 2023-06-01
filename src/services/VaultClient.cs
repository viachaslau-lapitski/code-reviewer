using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace vsl
{
    public class VaultClient
    {
        private async static Task<string> GetSecret(string name)
        {
            var client = new SecretClient(
                new Uri("https://key-vault-vsl-001.vault.azure.net/"),
                new DefaultAzureCredential());
            KeyVaultSecret secret = await client.GetSecretAsync(name);
            return secret.Value;
        }

        public async static Task<(string repo, string bot)> GetSecrets(string key)
        {
            var res = await Task.WhenAll(GetSecret(key), GetSecret("OpenAiKey"));
            return (res[0], res[1]);
        }
    }
}