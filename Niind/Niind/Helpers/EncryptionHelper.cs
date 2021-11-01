using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Niind.Helpers
{
    public static class AsyncHelper
    {
        
        
        
        public static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, TaskScheduler scheduler = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };
            if (scheduler != null)
                options.TaskScheduler = scheduler;

            var block = new ActionBlock<T>(body, options);

            foreach (var item in source)
                block.Post(item);

            block.Complete();
            return block.Completion;
        }
         
         
    }
    
    public static class EncryptionHelper
    {
        private static readonly SHA1 shaEngine = SHA1.Create();

        public static byte[] GetSHA1(byte[] data)
        {
            lock (shaEngine)
            {
                shaEngine.Initialize();
                shaEngine.ComputeHash(data);
                return shaEngine.Hash;
            }
        }


        public static string GetSHA1String(byte[] data) => ByteArrayToHexString(GetSHA1(data));
        public static string GetSHA1String(string data) => ByteArrayToHexString(GetSHA1(Encoding.ASCII.GetBytes(data)));
        public static byte[] AESDecrypt(byte[] cryptext, byte[] key, int outputLen, byte[]? iv = null)
        {
            using var aes = new RijndaelManaged
            {
                Padding = PaddingMode.None,
                Mode = CipherMode.CBC
            };

            var decryptor = aes.CreateDecryptor(key, iv);

            using var memoryStream = new MemoryStream(cryptext);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

            var plaintext = new byte[outputLen];
            _ = cryptoStream.Read(plaintext, 0x0, outputLen);

            return plaintext;
        }

        public static byte[] AESEncrypt(byte[] plaintext, byte[] key, int len, byte[]? iv = null)
        {
            using var aes = new RijndaelManaged
            {
                Padding = PaddingMode.None,
                Mode = CipherMode.CBC
            };

            var encryptor = aes.CreateEncryptor(key, iv);
            using var memoryStream = new MemoryStream(plaintext);
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Read);

            var cryptext = new byte[len];
            _ = cryptoStream.Read(cryptext, 0x0, len);

            return cryptext;
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