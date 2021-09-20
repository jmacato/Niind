using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandCluster
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public NandPage[] Pages;

        public Span<byte> GetRawMainPageData()
        {
            var buffer = ArrayPool<byte>.Shared.Rent((int)Constants.NandClusterNoSpareByteSize);
            var x = buffer.AsSpan();

            for (var i = 0; i < Pages.Length; i++)
                Pages[i].MainData.AsSpan(0).CopyTo(x.Slice((int)(i * Constants.NandPageNoSpareByteSize)));

            return x;
        }

        public byte[] DecryptCluster(KeyFile keyFile)
        {
            var enc_data = GetRawMainPageData().ToArray();

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