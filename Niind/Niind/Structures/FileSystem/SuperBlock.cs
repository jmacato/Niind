using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Niind.Helpers;

namespace Niind.Structures.FileSystem
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SuperBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        public byte[] SuperBlockHeader;
            
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        public byte[] VersionBigEndian;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        public byte[] MagicNumberBigEndian;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8000)]
        public ushort[] ClusterEntries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6143)] // ? Idk if this is the absolute limit
        public RawFileSystemTableEntry[] RawFileSystemTableEntries;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        private byte[] Padding;

        public static void RecalculateHMAC(ref byte[] rawSuperBlockData, NandDumpFile nandData, KeyFile keyData, ushort cluster)
        {
            NandCluster targetCluster = default;
            
            for (var i = cluster; i <= cluster + 0xF; i++)
            {
                var addr2 = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(i);
                if (i != cluster + 0xF) continue;
                targetCluster = nandData.Blocks[addr2.Block].Clusters[addr2.Cluster];
            }
            
            var sbSalt = new byte[0x40];

            var sbStartClusterBytes = BitConverter.GetBytes(cluster);

            // this is correct way of generating the sb salt. verified on wiiqt.

            sbSalt[0x12] = sbStartClusterBytes[0x1];
            sbSalt[0x13] = sbStartClusterBytes[0x0];

            using var hmacsha1 = new HMACSHA1(keyData.NandHMACKey);
            using var mm2 = new MemoryStream();
 
            mm2.Write(sbSalt);
            mm2.Write(rawSuperBlockData);
            mm2.Position = 0x0;

            var newHMAC = hmacsha1.ComputeHash(mm2);
            
            // Complicated way of setting the new HMAC hash
            // based on wii_qt checks.

            targetCluster.PurgeSpareData();
 
            newHMAC.CopyTo(targetCluster.Pages[0x6].SpareData.AsSpan()[0x1..0x15]);
  
            newHMAC.AsSpan()[..0xc].CopyTo(targetCluster.Pages[0x6].SpareData.AsSpan().Slice(0x15, 0xc));
            newHMAC.AsSpan()[(newHMAC.Length-8)..].CopyTo(targetCluster.Pages[0x7].SpareData.AsSpan()[1..]);
            
            targetCluster.RecalculateECC();
        }
    }
}