using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Niind.Structures
{
    
    [StructLayout(LayoutKind.Sequential)]
    public struct NandCluster
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public NandPage[] Pages;
        
        public byte[] DecryptCluster(KeyFile keyFile)
        {
            var enc_data = new byte[Constants.NandClusterNoSpareByteSize];
            
            for (var i = 0; i < Pages.Length; i++)
            {
                Array.Copy(Pages[i].MainData, enc_data, Pages[i].MainData.Length);
            }
        
            var aes = new RijndaelManaged
            {
                Padding = PaddingMode.None,
                Mode = CipherMode.CBC
            };
            
            var decryptor = aes.CreateDecryptor(keyFile.NandAESKey, Constants.EmptyAESIVBytes);
            
            using var memoryStream = new MemoryStream(enc_data);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        
            var dec_data = new byte[enc_data.Length];
            _ = cryptoStream.Read(dec_data, 0, dec_data.Length);
            
            return dec_data;
        }
    }
}