using RHToolkit.Models.UISettings;
using System.Security.Cryptography;

namespace RHToolkit.Models.RH;

public class RHCryptor
{
    private readonly Aes _aes = Aes.Create();

    /// <summary>
    /// Initializes a new instance of the <see cref="RHCryptor"/> class.
    /// </summary>
    public RHCryptor()
    {
        var keyString = RegistrySettingsHelper.GetTableEncryptKey();

        byte[] bytes = Encoding.UTF8.GetBytes(keyString);
        byte[] key = new byte[bytes.Length + 1];
        Buffer.BlockCopy(bytes, 0, key, 0, bytes.Length);

        _aes.Key = key;
        _aes.IV = [0xdb, 15, 0x49, 0x40, 0xdb, 15, 0x49, 0x40, 0xdb, 15, 0x49, 0x40, 0xdb, 15, 0x49, 0x40];
        _aes.Mode = CipherMode.ECB;
        _aes.Padding = PaddingMode.Zeros;
    }

    /// <summary>
    /// Decrypts the specified byte array.
    /// </summary>
    /// <param name="toByte">The byte array to decrypt.</param>
    /// <returns>The decrypted byte array.</returns>
    /// <exception cref="CryptographicException">Thrown when decryption fails.</exception>
    /// <exception cref="Exception">Thrown when a general error occurs during decryption.</exception>
    public byte[] Decrypt(byte[] toByte)
    {
        try
        {
            byte[] decryptedBytes;
            using (Aes aesInstance = Aes.Create())
            {
                aesInstance.Key = _aes.Key;
                aesInstance.IV = _aes.IV;
                aesInstance.Mode = CipherMode.ECB;
                aesInstance.Padding = PaddingMode.Zeros;

                decryptedBytes = aesInstance.CreateDecryptor(aesInstance.Key, aesInstance.IV)
                    .TransformFinalBlock(toByte, 0, toByte.Length);
            }

            return decryptedBytes;
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Decryption failed: " + ex.Message, ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"{Resources.Error}: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Encrypts the specified byte array.
    /// </summary>
    /// <param name="toByte">The byte array to encrypt.</param>
    /// <returns>The encrypted byte array.</returns>
    /// <exception cref="CryptographicException">Thrown when encryption fails.</exception>
    /// <exception cref="Exception">Thrown when a general error occurs during encryption.</exception>
    public byte[] Encrypt(byte[] toByte)
    {
        try
        {
            byte[] encryptedBytes;
            using (Aes aesInstance = Aes.Create())
            {
                aesInstance.Key = _aes.Key;
                aesInstance.IV = _aes.IV;
                aesInstance.Mode = CipherMode.ECB;
                aesInstance.Padding = PaddingMode.Zeros;

                encryptedBytes = aesInstance.CreateEncryptor(aesInstance.Key, aesInstance.IV)
                    .TransformFinalBlock(toByte, 0, toByte.Length);
            }

            return encryptedBytes;
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Encryption failed: " + ex.Message, ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"{Resources.Error}: " + ex.Message, ex);
        }
    }
}