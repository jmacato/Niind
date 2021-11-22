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


        public void WriteData(byte[] rawData)
        {
            foreach (var pageChunk in Enumerable.Range(0, Pages.Length).Zip(rawData.Chunk((int)Constants.NandPageNoSpareByteSize)))
            {
                Pages[pageChunk.First].MainData = pageChunk.Second.ToArray();
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

            var cryptext = EncryptionHelper.AESEncrypt(plainRawData, keyFile.NandAESKey, Constants.EmptyAESIVBytes);

            WriteData(cryptext);
        }

        public byte[] DecryptCluster(KeyFile keyFile)
        {
            var enc_data = GetRawMainPageData();

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
            WriteDataAsEncrypted(keyFile, Constants.EmptyClusterRawData);
            PurgeSpareData();
            RecalculateECC();
        }
    }
}