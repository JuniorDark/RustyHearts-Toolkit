using System.Security.Cryptography;

namespace RHToolkit.Models.RH;

public class RHCryptor
{
    private readonly Aes _aes = Aes.Create();

    public RHCryptor()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("gkw3iurpamv;kj20984;asdkfjat1af");
        byte[] key = new byte[bytes.Length + 1];
        Buffer.BlockCopy(bytes, 0, key, 0, bytes.Length);

        _aes.Key = key;
        _aes.IV = [0xdb, 15, 0x49, 0x40, 0xdb, 15, 0x49, 0x40, 0xdb, 15, 0x49, 0x40, 0xdb, 15, 0x49, 0x40];
        _aes.Mode = CipherMode.ECB;
        _aes.Padding = PaddingMode.Zeros;
    }

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
            throw new Exception("Error: " + ex.Message, ex);
        }
    }

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

                int x = toByte.Length % 16;
                if (x > 0)
                {
                    x = 16 - x;
                    byte[] newBytes = new byte[x + toByte.Length];
                    Buffer.BlockCopy(toByte, 0, newBytes, 0, toByte.Length);
                    newBytes[toByte.Length] = 0x2a;
                    toByte = newBytes;
                }

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
            throw new Exception("Error: " + ex.Message, ex);
        }
    }

}
