using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Niind.Helpers
{
    public static class EncryptionHelper
    {
        public static byte[] GetSHA1(byte[] data)
        {
            using var shaEngine = SHA1.Create();
            shaEngine.ComputeHash(data);
            return shaEngine.Hash;
        }

        public static string GetSHA1String(byte[] data) => ByteArrayToHexString(GetSHA1(data));
        public static string GetSHA1String(string data) => ByteArrayToHexString(GetSHA1(Encoding.ASCII.GetBytes(data)));

        public static byte[] AESDecrypt(byte[] cryptext, byte[] key, int outputLen, byte[]? iv = null)
        {
            using var x = Aes.Create();
            x.Key = key;
            return x.DecryptCbc(cryptext, iv, PaddingMode.None);
        }

        public static byte[] AESEncrypt(byte[] plaintext, byte[] key, byte[]? iv = null)
        {
            using var x = Aes.Create();
            x.Key = key;
            return x.EncryptCbc(plaintext, iv, PaddingMode.None);
        }

        public static void PadByteArrayToMultipleOf(ref byte[] src, int pad)
        {
            var len = (src.Length + pad - 1) / pad * pad;
            Array.Resize(ref src, len);
        }

        public static string ByteArrayToHexString(byte[] inx)
        {
            return BitConverter.ToString(inx).Replace("-", "");
        }
    }
}