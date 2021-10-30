using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Niind.Helpers;

namespace Niind.Structures.FileSystem
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


        public void WriteDataNoEncryption(byte[] rawData)
        {
            for (int i = 0; i < Pages.Length; i++)
            {
                Pages[i].MainData =
                    rawData.AsSpan()
                        .Slice((int)(i * Constants.NandPageNoSpareByteSize), (int)Constants.NandPageNoSpareByteSize)
                        .ToArray();
            }

            RecalculateECC();
        }

        public void PurgeSpareData()
        {
            foreach (var page in Pages)
            {
                var target = page.SpareData;
                target.AsSpan().Fill(0);
                target[0] = 0xFF;
            }
        }

        public void RecalculateECC()
        {
            foreach (var page in Pages)
            {
                page.RecalculateECC();
            }
        }


        public void WriteDataAsEncrypted(KeyFile keyFile, byte[] plainRawData)
        {
            if (plainRawData.Length > Constants.NandClusterNoSpareByteSize)
            {
                throw new ArgumentOutOfRangeException(nameof(plainRawData));
            }

            var cryptext = EncryptionHelper.AESEncrypt(plainRawData, keyFile.NandAESKey, (int)Constants.NandClusterNoSpareByteSize, Constants.EmptyAESIVBytes);
            
            WriteDataNoEncryption(cryptext);
        }

        public byte[] DecryptCluster(KeyFile keyFile)
        {
            var enc_data = GetRawMainPageData();

            // var aes = new RijndaelManaged
            // {
            //     Padding = PaddingMode.None,
            //     Mode = CipherMode.CBC
            // };
            //
            // var decryptor = aes.CreateDecryptor(keyFile.NandAESKey, new byte[0x10]);
            //
            // using var memoryStream = new MemoryStream();
            // using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            //
            // var dec_data = new byte[enc_data.Length];
            // _ = cryptoStream.Read(dec_data, 0x0, dec_data.Length);
            //
            //

            var dec_data = EncryptionHelper.AESDecrypt(enc_data, keyFile.NandAESKey, enc_data.Length,
                Constants.EmptyAESIVBytes);


            return dec_data;
        }

        public bool CheckECC()
        {
            return Pages.All(page => page.IsECCCorrect());
        }

        public void EraseData(KeyFile keyFile)
        {
            WriteDataAsEncrypted(keyFile, Constants.EmptyPageRawData);
            PurgeSpareData();
            RecalculateECC();
        }
    }
}