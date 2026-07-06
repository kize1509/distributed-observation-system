using System.Security.Cryptography;

namespace DistributedObservationSystem.Security;

public sealed class AesGcmService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public byte[] Encrypt(byte[] key, byte[] plaintext, byte[] aad)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, NonceSize);
        tag.CopyTo(result, NonceSize + ciphertext.Length);
        return result;
    }

    public byte[] Decrypt(byte[] key, byte[] payload, byte[] aad)
    {
        var nonce = payload[..NonceSize];
        var tag = payload[^TagSize..];
        var ciphertext = payload[NonceSize..^TagSize];
        var plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);
        return plaintext;
    }
}
