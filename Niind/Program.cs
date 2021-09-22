using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Niind.Structures;

namespace Niind
{
    internal class Program
    {
        private static readonly ReadOnlyMemory<byte> SuperBlockHeaderBytes = Encoding.ASCII.GetBytes("SFFS").AsMemory();

        private static void Main(string[] args)
        {
            Console.WriteLine("Loading Files...");

            var rawFullDump =
                File.ReadAllBytes("/Users/jumarmacato/Desktop/Wii NAND Experiment/wiinandfolder/nand-04z02504.bin");
            Console.WriteLine("Nand File Loaded.");

            var rawKeyFile =
                File.ReadAllBytes("/Users/jumarmacato/Desktop/Wii NAND Experiment/wiinandfolder/keys-04z02504.bin");

            Console.WriteLine("Key File Loaded.");

            var nandData = rawFullDump.CastToStruct<NandDumpFile>();

            Console.WriteLine("NAND Dump marshalled to C# structs.");

            var keyData = rawKeyFile.CastToStruct<KeyFile>();

            Console.WriteLine("Key file marshalled to C# structs.");

            if (Marshal.SizeOf(nandData) != rawFullDump.LongLength)
                throw new FormatException("The NAND dump's internal structure is not correct!" +
                                          " Try to dump your console via BootMii again.");

            var bootMiiHeaderText = Encoding.ASCII.GetString(nandData.BootMiiFooterBlock.HeaderString).Trim((char)0x0)
                .Trim('\n');

            Console.WriteLine($"BootMii Metadata Header: {bootMiiHeaderText}");

            var consoleId = BitConverter.ToString(keyData.ConsoleID).Replace("-", "");

            if (!bootMiiHeaderText.Contains(consoleId, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException("The Key File provided is not for this specific NAND dump.");

            Console.WriteLine("Key file matches the NAND dump.");

            var foundSuperblocks = new List<(uint absoluteCluster, long baseOffset, uint version)>();

            for (var i = Constants.SuperblocksBaseCluster;
                i <= Constants.SuperblocksEndCluster;
                i += Constants.SuperblocksClusterIncrement)
            {
                var sp = AddressTranslation.AbsoluteClusterToBlockCluster(i);

                var sbFirstPage = nandData.Blocks[sp.Block].Clusters[sp.Cluster].Pages[0x0].MainData;

                var sbHeader = sbFirstPage.AsSpan(0x0, 0x4);

                var sbVersion = SpanToBigEndianUInt(sbFirstPage.ToArray().AsSpan(0x5, 0x4));

                if (sbHeader.SequenceEqual(SuperBlockHeaderBytes.Span))
                {
                    var absOffset = AddressTranslation.BCPToOffset(sp.Block, sp.Cluster, 0x0);
                    Console.WriteLine(
                        $"Found a superblock at Cluster 0x{i:X} Offset 0x{absOffset:X} Version {sbVersion}");
                    foundSuperblocks.Add((i, absOffset, sbVersion));
                }
            }

            var candidateSb = foundSuperblocks
                .OrderByDescending(x => x.version)
                .First();

            Console.WriteLine(
                $"Candidate superblock with highest gen number: Cluster 0x{candidateSb.absoluteCluster:X} Offset 0x{candidateSb.baseOffset:X} Version {candidateSb.version}");

            uint clNo = 0x2CE; //Setting.txt Cluster address

            var addr = AddressTranslation.AbsoluteClusterToBlockCluster(clNo);

            var cluster = nandData.Blocks[addr.Block].Clusters[addr.Cluster];

            var sampleDataClusterMainData = cluster.DecryptCluster(keyData);

            using var hmacsha1 = new HMACSHA1(keyData.NandHMACKey);

            // Salts needs to be 0x40 long... this is what tripping up the hmac before :facepalm:
            var sampleDataClusterSalt = new byte[0x40]
            {
                0x0, 0x0, 0x10, 0x0, 0x73, 0x65, 0x74, 0x74, 0x69, 0x6E,
                0x67, 0x2E, 0x74, 0x78, 0x74, 0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x20, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0
            };

            var mm = new MemoryStream();
            mm.Write(sampleDataClusterSalt);
            mm.Write(sampleDataClusterMainData);
            mm.Position = 0x0;

            var calculatedHMAC = ToHex(hmacsha1.ComputeHash(mm));

            var nandClusterHMAC = ToHex(cluster.Pages[0x6].SpareData[0x1..0x15]);

            if (calculatedHMAC == nandClusterHMAC) Console.WriteLine("Cluster 0x2ce HMAC checks out.");

            mm.Close();
            mm.Dispose();

            // verify candidate superblock's HMAC and ECC

            var superBlockBuffer = new MemoryStream();
            var sbBufCount = 0x0;

            Span<byte> sbSpare1 = null, sbSpare2 = null;

            for (var i = candidateSb.absoluteCluster; i <= candidateSb.absoluteCluster + 0xF; i++)
            {
                var addr2 = AddressTranslation.AbsoluteClusterToBlockCluster(i);

                var cluster2 = nandData.Blocks[addr2.Block].Clusters[addr2.Cluster];

                superBlockBuffer.Write(cluster2.GetRawMainPageData());

                if (i != candidateSb.absoluteCluster + 0xF) continue;

                sbSpare1 = cluster2.Pages[0x6].SpareData;
                sbSpare2 = cluster2.Pages[0x7].SpareData;
            }

            var dbg0 = ToHex(sbSpare1.ToArray());
            var dbg1 = ToHex(sbSpare2.ToArray());

            var candidateSuperBlockHMAC = sbSpare1[0x1..0x15];

            var sbSalt = new byte[0x40];
            var sbStartAbsClusterBytes = BitConverter.GetBytes(candidateSb.absoluteCluster);

            sbSalt.AsSpan().Fill(0x0);

            // this is correct way of generating the sb salt. verified on wiiqt.

            sbSalt[0x12] = sbStartAbsClusterBytes[0x1];
            sbSalt[0x13] = sbStartAbsClusterBytes[0x0];

            hmacsha1.Initialize();

            using var mm2 = new MemoryStream();
            var xzx = superBlockBuffer.ToArray();

            mm2.Write(sbSalt);
            mm2.Write(xzx);
            mm2.Position = 0x0;

            if (candidateSuperBlockHMAC.SequenceEqual(hmacsha1.ComputeHash(mm2)))
            {
                Console.WriteLine("Candidate Superblock has valid HMAC.");
            }

            mm2.Close();
            mm2.Dispose();

            var readableSuperBlock = xzx.CastToStruct<SuperBlock>();
            Console.WriteLine("Casting Candidate Superblock to a C# struct.");


            var ValidFSTEntries = new Dictionary<uint, RawFileSystemTableEntry>();
            var ValidClusters = new Dictionary<ushort, ushort>();

            var entryCount = 0u;
            foreach (var entry in readableSuperBlock.RawFileSystemTableEntries.Where(x => !x.IsEmpty))
            {
                ValidFSTEntries.Add(entryCount, entry);
                entryCount++;
            }

            Console.WriteLine($"Found {entryCount} Filesystem Table Entries.");

            var badClusters = 0u;
            var reservedClusters = 0u;
            var freeClusters = 0u;
            var chainLastClusters = 0u;

            ushort clusterIndex = 0;

            foreach (var clusterDesc in readableSuperBlock.ClusterEntries.Select(ByteWiseSwap))
            {
                switch ((ClusterDescriptor)clusterDesc)
                {
                    case ClusterDescriptor.Bad:
                        badClusters++;
                        clusterIndex++;
                        continue;
                    case ClusterDescriptor.Reserved:
                        reservedClusters++;
                        clusterIndex++;
                        continue;
                    case ClusterDescriptor.Empty:
                        freeClusters++;
                        break;
                    case ClusterDescriptor.ChainLast:
                        chainLastClusters++;
                        break;
                }

                ValidClusters.Add(clusterIndex, clusterDesc);
                clusterIndex++;
            }

            Console.WriteLine($"Reserved Clusters:  {reservedClusters}");
            Console.WriteLine($"Bad Clusters:       {badClusters}");
            Console.WriteLine($"Free Clusters:      {freeClusters}");

            var nodes = new Dictionary<uint, FileSystemNode>();

            Console.WriteLine("Connecting File FST's to their clusters.");

            // process files first.
            foreach (var entry in ValidFSTEntries)
            {
                var rFST = entry.Value.ToReadableFST();

                if (!rFST.IsFile)
                {
                    var newDir = new FileSystemNode()
                    {
                        IsFile = false,
                        Filename = rFST.FileName,
                        FSTEntry = rFST,
                        FSTEntryIndex = entry.Key,
                    };

                    nodes.Add(entry.Key, newDir);
                }
                else
                {
                    var startingCluster = rFST.Sub;
                    
                    var newFile = new FileSystemNode()
                    {
                        IsFile = true,
                        Filename = rFST.FileName,
                        FSTEntry = rFST,
                        FSTEntryIndex = entry.Key,
                    };

                    var nextCluster = startingCluster;

                    var estimatedClusterCount = (rFST.FileSize / Constants.NandClusterNoSpareByteSize) + 1;

                    var watchdogCounter = 0;

                    while (watchdogCounter < estimatedClusterCount)
                    {
                        if ((ClusterDescriptor)nextCluster == ClusterDescriptor.ChainLast)
                            break;

                        var curCluster = ValidClusters[nextCluster];

                        newFile.Clusters.Add(curCluster);

                        nextCluster = curCluster;

                        watchdogCounter++;
                    }

                    nodes.Add(entry.Key, newFile);
                }
            }

            Console.WriteLine("Connecting Directory FST's to their children.");

            // then connect directories
            foreach (var entry in nodes)
            {
                var dirNode = entry.Value;

                if (dirNode.IsFile) continue;

                var sub = entry.Value.FSTEntry.Sub;

                if (sub == Constants.FSTSubEndCapValue || !nodes.ContainsKey(sub))
                    continue;

                dirNode.Children.Add(nodes[sub]);

                var sib = dirNode.Children[0].FSTEntry.Sib;

                var currentSib = sib;

                while (true)
                {
                    if (currentSib == Constants.FSTSubEndCapValue || !nodes.ContainsKey(currentSib))
                        break;
                    
                    var curSibNode = nodes[currentSib];
                    dirNode.Children.Add(curSibNode);
                    currentSib = curSibNode.FSTEntry.Sib;
                }
            }

            Console.WriteLine("Printing filesystem tree...\n");
            var rootNode = nodes[0];
            rootNode.PrintPretty();
            Console.WriteLine("\nFinished.");
        }


        public static ushort ByteWiseSwap(ushort value)
        {
            return (ushort)((0x00FF & (value >> 8))
                            | (0xFF00 & (value << 8)));
        }

        public static uint ByteWiseSwap(uint value)
        {
            uint swapped = (0x000000FF) & (value >> 24)
                           | (0x0000FF00) & (value >> 8)
                           | (0x00FF0000) & (value << 8)
                           | (0xFF000000) & (value << 24);
            return swapped;
        }


        private static string ToHex(byte[] inx)
        {
            return BitConverter.ToString(inx).Replace("-", "");
        }

        private static int Simplehash(byte[] data, int size)
        {
            var result = 0x7E7E;

            for (var i = 0x0; i < size; ++i)
            {
                int dax = data[i];
                result ^= dax++;
            }

            return result;
        }

        private static byte[] StringToByteArray(string hex)
        {
            var NumberChars = hex.Length;
            var bytes = new byte[NumberChars / 0x2];
            for (var i = 0x0; i < NumberChars; i += 0x2)
                bytes[i / 0x2] = Convert.ToByte(hex.Substring(i, 0x2), 0x10);
            return bytes;
        }

        public static uint SpanToBigEndianUInt(Span<byte> input)
        {
            input.Reverse();
            return BitConverter.ToUInt32(input.ToArray(), 0x0);
        }
        
        public static class AddressTranslation
        {
            public static (uint Block, uint Cluster) AbsoluteClusterToBlockCluster(uint absoluteCluster)
            {
                var block = (uint)Math.Floor((float)absoluteCluster / 0x8);
                var cluster = absoluteCluster % 0x8;
                return (block, cluster);
            }

            public static long BCPToOffset(uint block, uint cluster, uint page)
            {
                var b = block * Constants.NandBlockByteSize;
                var c = cluster * Constants.NandClusterByteSize;
                var p = page * Constants.NandPageByteSize;
                return b + c + p;
            }
        }
    }
}