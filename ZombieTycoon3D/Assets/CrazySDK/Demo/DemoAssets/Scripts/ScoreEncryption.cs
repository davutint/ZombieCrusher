using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CrazyGames
{
    public static class ScoreEncryption
    {
        public static string EncryptScore(float score, string encryptionKey)
        {
            // generate random IV
            byte[] iv = new byte[12];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            byte[] key = Convert.FromBase64String(encryptionKey);
            byte[] plaintext = Encoding.UTF8.GetBytes(score.ToString(CultureInfo.InvariantCulture));

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                using (var encryptor = aes.CreateEncryptor())
                {
                    // CTR mode encryption
                    byte[] ciphertext = new byte[plaintext.Length];
                    byte[] counter = new byte[16];
                    Buffer.BlockCopy(iv, 0, counter, 0, 12);
                    counter[15] = 1; // Start counter at 1

                    for (int i = 0; i < plaintext.Length; i += 16)
                    {
                        byte[] keystream = encryptor.TransformFinalBlock(counter, 0, 16);
                        int blockSize = Math.Min(16, plaintext.Length - i);
                        for (int j = 0; j < blockSize; j++)
                        {
                            ciphertext[i + j] = (byte)(plaintext[i + j] ^ keystream[j]);
                        }

                        // increment counter (32-bit big-endian)
                        for (int k = 15; k >= 12; k--)
                        {
                            if (++counter[k] != 0)
                                break;
                        }
                    }

                    // combine: IV + ciphertext + Unity marker
                    byte[] result = new byte[iv.Length + ciphertext.Length + 1];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
                    result[result.Length - 1] = 0x55; // Unity marker byte

                    string base64Result = Convert.ToBase64String(result);
                    return base64Result;
                }
            }
        }
    }
}
