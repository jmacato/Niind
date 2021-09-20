using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandCluster
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public NandPage[] Pages;

        public byte[] GetRawMainPageData()
        {
            // so inefficient but whatever.
            return Pages.SelectMany(x => x.MainData).ToArray();
        }

        public byte[] DecryptCluster(KeyFile keyFile)
        {
            var enc_data = GetRawMainPageData();

            var aes = new RijndaelManaged
            {
                Padding = PaddingMode.None,
                Mode = CipherMode.CBC
            };

            var decryptor = aes.CreateDecryptor(keyFile.NandAESKey, new byte[16]);

            using var memoryStream = new MemoryStream(enc_data);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

            var dec_data = new byte[enc_data.Length];
            _ = cryptoStream.Read(dec_data, 0, dec_data.Length);

            return dec_data;
        }
    }
}