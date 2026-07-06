using System.Security.Cryptography;

namespace DistributedObservationSystem.Security;

public sealed class RsaHandshakeService
{
    public (string PublicKeyPem, RSA PrivateKey) GenerateKeyPair()
    {
        var rsa = RSA.Create(4096);
        return (rsa.ExportSubjectPublicKeyInfoPem(), rsa);
    }

    public byte[] DecryptSessionKey(RSA privateKey, byte[] encryptedSessionKey)
        => privateKey.Decrypt(encryptedSessionKey, RSAEncryptionPadding.OaepSHA256);

    public byte[] EncryptSessionKey(string publicKeyPem, byte[] sessionKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return rsa.Encrypt(sessionKey, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] GenerateSessionKey()
        => RandomNumberGenerator.GetBytes(32);
}
