using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandCluster
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        public NandPage[] Pages;

        public byte[] GetRawMainPageData()
        {
            // so inefficient but whatever.
            return Pages.SelectMany(x => x.MainData).ToArray();
        }

        public void WriteDataAsEncrypted(KeyFile keyFile, byte[] plainRawData)
        {
            if (plainRawData.Length > Constants.NandClusterNoSpareByteSize)
            {
                throw new ArgumentOutOfRangeException();
            }

            var aes = new RijndaelManaged
            {
                Padding = PaddingMode.None,
                Mode = CipherMode.CBC
            };

            var encryptor = aes.CreateEncryptor(keyFile.NandAESKey, new byte[0x10]);

            using var memoryStream = new MemoryStream(plainRawData);
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Read);

            var outdata = new byte[Constants.NandClusterNoSpareByteSize];

            _ = cryptoStream.Read(outdata, 0x0, outdata.Length);

            WriteDataNoEncryption(outdata);
        }
        
        public void WriteDataNoEncryption(byte[] rawData, bool deleteHMAC = false)
        {
            for (int i = 0; i < Pages.Length; i++)
            {
                Pages[i].MainData = 
                rawData.AsSpan()
                    .Slice((int)(i*Constants.NandPageNoSpareByteSize), (int)Constants.NandPageNoSpareByteSize).ToArray();
                
                if (deleteHMAC)
                {
                    Pages[i].SpareData.AsSpan().Fill(0);
                    Pages[i].SpareData[0] = 0xFF;
                }
                
                Pages[i].RecalculateECC();
            }
        }

        public byte[] DecryptCluster(KeyFile keyFile)
        {
            var enc_data = GetRawMainPageData();

            var aes = new RijndaelManaged
            {
                Padding = PaddingMode.None,
                Mode = CipherMode.CBC
            };

            var decryptor = aes.CreateDecryptor(keyFile.NandAESKey, new byte[0x10]);

            using var memoryStream = new MemoryStream(enc_data);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

            var dec_data = new byte[enc_data.Length];
            _ = cryptoStream.Read(dec_data, 0x0, dec_data.Length);

            return dec_data;
        }

        public bool CheckECC()
        {
            return Pages.All(page => page.IsECCCorrect());
        }

        public void EraseData(KeyFile keyFile)
        {
            WriteDataAsEncrypted(keyFile, Constants.EmptyPageRawData);
        }
    }
}