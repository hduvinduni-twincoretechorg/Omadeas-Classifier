using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Omadeas.Classifier.Core.Utils
{
    public static class RsaKeyLoader
    {
        public static RsaSecurityKey LoadPrivateKey(IConfiguration cfg)
        {
            string? privateKeyPem = Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY_PEM");
            if (string.IsNullOrWhiteSpace(privateKeyPem))
            {
                string? pemPath = cfg["Jwt:PrivateKeyPemPath"];
                if (!string.IsNullOrWhiteSpace(pemPath) && File.Exists(pemPath))
                {
                    privateKeyPem = File.ReadAllText(pemPath);
                }
            }
            if (string.IsNullOrWhiteSpace(privateKeyPem))
                throw new InvalidOperationException("No private key found for JWT signing. Set JWT_PRIVATE_KEY_PEM env var or provide a local PEM file.");
            RSA rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem.ToCharArray());
            return new RsaSecurityKey(rsa);
        }

        public static RsaSecurityKey LoadPublicKey(IConfiguration cfg)
        {
            string? publicKeyPem = Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY_PEM");
            if (string.IsNullOrWhiteSpace(publicKeyPem))
            {
                string? publicKeyPemPath = cfg["Jwt:PublicKeyPemPath"];
                if (!string.IsNullOrWhiteSpace(publicKeyPemPath) && File.Exists(publicKeyPemPath))
                {
                    publicKeyPem = File.ReadAllText(publicKeyPemPath);
                }
            }
            if (string.IsNullOrWhiteSpace(publicKeyPem))
                throw new InvalidOperationException("No public key found for JWT validation. Set JWT_PUBLIC_KEY_PEM env var or provide a local PEM file.");
            RSA rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem.ToCharArray());
            return new RsaSecurityKey(rsa);
        }
    }
}
