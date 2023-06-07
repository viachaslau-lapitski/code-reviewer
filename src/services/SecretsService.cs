using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace vsl
{
    public class SecretsService
    {
        private async static Task<string> GetSecret(string name)
        {
            var client = new SecretClient(
                new Uri("https://key-vault-vsl-001.vault.azure.net/"),
                new DefaultAzureCredential());
            KeyVaultSecret secret = await client.GetSecretAsync(name);
            return secret.Value;
        }

        private static string CreateGithubJwt(string pemFile, int githubAppId)
        {
            var reader = new StringReader(pemFile);
            var pemReader = new PemReader(reader);
            var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
            var privateKey = keyPair.Private as RsaPrivateCrtKeyParameters;

            var rsa = RSA.Create(new RSAParameters
            {
                Modulus = privateKey.Modulus.ToByteArrayUnsigned(),
                Exponent = privateKey.PublicExponent.ToByteArrayUnsigned(),
                P = privateKey.P.ToByteArrayUnsigned(),
                Q = privateKey.Q.ToByteArrayUnsigned(),
                DP = privateKey.DP.ToByteArrayUnsigned(),
                DQ = privateKey.DQ.ToByteArrayUnsigned(),
                InverseQ = privateKey.QInv.ToByteArrayUnsigned(),
                D = privateKey.Exponent.ToByteArrayUnsigned()
            });

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = githubAppId.ToString(),
                Expires = DateTime.UtcNow.AddMinutes(10),
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
            };

            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(securityToken);
            return jwt;
        }

        public async static Task<(string repo, string bot)> GetSecrets(int installationId)
        {
            var res = await Task.WhenAll(GetSecret("GithubAppsPK"), GetSecret("OpenAiKey"));
            var appId = 342818;
            var jwt = CreateGithubJwt(res[0], appId);
            var installationToken = await GitHubRepo.getInstallationToken(jwt, installationId);
            return (installationToken, res[1]);
        }
    }
}