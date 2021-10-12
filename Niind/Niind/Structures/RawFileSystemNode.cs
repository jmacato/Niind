using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Niind.Structures
{
    public class RawFileSystemNode
    {
        public string Filename;
        public bool IsFile;
        public List<ushort> Clusters = new();
        public List<RawFileSystemNode> Children = new();
        public ReadableFileSystemTableEntry FSTEntry;
        public uint FSTEntryIndex;
        public RawFileSystemNode Parent;

        public byte[] GetFileContents(NandDumpFile nandData, KeyFile keyData)
        {
            if (!IsFile) return Array.Empty<byte>();
            using var mm = new MemoryStream();

            foreach (var currentCluster in Clusters)
            {
                var addr = NandAddressTranslation.AbsoluteClusterToBlockCluster(currentCluster);
                var targetClusterData = nandData.Blocks[addr.Block].Clusters[addr.Cluster].DecryptCluster(keyData);
                mm.Write(targetClusterData);
            }

            return mm.ToArray().AsSpan(0, (int)FSTEntry.FileSize).ToArray();
        }

        public IEnumerable<RawFileSystemNode> GetDescendants()
        {
            foreach (var child in Children)
            {
                yield return child;
                foreach (var descendant in child.GetDescendants())
                {
                    yield return descendant;
                }
            }
        }

        public void PrintPretty(string indent = "  ", bool last = false)
        {
            Console.Write(indent);

            Console.Write("|-");
            indent += "| ";


            Console.Write(Filename + (IsFile ? "" : "/"));
            
            if(IsFile)
            Console.Write($"\t\t{FSTEntry.FileSize} bytes");
            
            Console.WriteLine("");

            for (int i = 0; i < Children.Count; i++)
                Children[i].PrintPretty(indent, i == Children.Count - 1);
        }

        public bool ModifyFileContentsNoResize(NandDumpFile nandData, KeyFile keyData, byte[] data)
        {
            if (!IsFile || data.LongLength > FSTEntry.FileSize) return false;

            for (uint i = 0; i < Clusters.Count; i++)
            {
                var currentCluster = Clusters[(int)i];

                var (block, cluster) = NandAddressTranslation.AbsoluteClusterToBlockCluster(currentCluster);

                var chunkLen = (int)Math.Min(Constants.NandClusterNoSpareByteSize, data.LongLength);

                var chunk = data.AsSpan().Slice((int)(i * Constants.NandClusterNoSpareByteSize),
                    chunkLen).ToArray();

                var targetCluster = nandData.Blocks[block].Clusters[cluster];

                targetCluster.WriteDataAsEncrypted(keyData, chunk);

                //Setting HMAC on this cluster:
                RecalculateHMAC(keyData, i, targetCluster);
            }

            return true;
        }


        private void RecalculateHMAC(KeyFile keyData, uint clusterIndex, NandCluster targetCluster)
        {
            var saltF = new byte[0x40];

            var rawFST = FSTEntry.Source;
            rawFST.UserIDBigEndian.CopyTo(saltF.AsSpan()[..4]);
            rawFST.FileName.CopyTo(saltF.AsSpan()[0x4..]);

            var fstIndex = BitConverter.GetBytes(FSTEntryIndex).Reverse().ToArray();
            fstIndex.CopyTo(saltF.AsSpan().Slice(0x14, 4));
            rawFST.X3.CopyTo(saltF.AsSpan().Slice(0x18, 4));

            var c = BitConverter.GetBytes(clusterIndex).Reverse().ToArray();
            c.CopyTo(saltF.AsSpan().Slice(0x10, 4));

            using var hmac = new HMACSHA1(keyData.NandHMACKey);
            using var mm = new MemoryStream();
            mm.Write(saltF);
            mm.Write(targetCluster.DecryptCluster(keyData));
            mm.Position = 0;

            var newHMAC = hmac.ComputeHash(mm);
            Console.WriteLine(
                $"{Filename} C {clusterIndex} hmac {string.Join("", newHMAC.Select(x => x.ToString("X2")))}");
            var sp1 = targetCluster.Pages[0x6].SpareData;
            var sp2 = targetCluster.Pages[0x7].SpareData;

            targetCluster.PurgeSpareData();
            newHMAC.CopyTo(sp1.AsSpan()[0x1..0x15]);
            newHMAC.AsSpan()[..0xc].CopyTo(sp1.AsSpan().Slice(0x15, 0xc));
            newHMAC.AsSpan()[(newHMAC.Length - 8)..].CopyTo(sp2.AsSpan()[1..]);
            targetCluster.RecalculateECC();
        }
    }
}