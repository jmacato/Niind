using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Niind.Helpers;

namespace Niind.Structures.FileSystem
{
    public class DistilledNand
    {
        public byte[] MainSuperBlockRaw;
        public Dictionary<ushort, ushort> ValidClusters;
        public List<SuperBlockDescriptor> SuperBlockDescriptors;
        public RawFileSystemNode RootNode;
        public NandDumpFile NandDumpFile;
        public KeyFile KeyFile;

        public DistilledNand(NandDumpFile nandDumpFile, KeyFile keyFile)
        {
            NandDumpFile = nandDumpFile;
            KeyFile = keyFile;
            NandProcessAndCheck();
        }

        public void NandProcessAndCheck()
        {
            var foundSuperblocks = new List<SuperBlockDescriptor>();

            for (var i = Constants.SuperBlocksBaseCluster;
                 i <= Constants.SuperBlocksEndCluster;
                 i += Constants.SuperBlocksClusterIncrement)
            {
                var sp = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(i);

                var sbFirstPage = NandDumpFile.Blocks[sp.Block].Clusters[sp.Cluster].Pages[0x0].MainData;

                var sbHeader = sbFirstPage.AsSpan(0x0, 0x4);

                var sfd = sbFirstPage.AsSpan(0x4, 0x4);

                var sbVersion = BitConverter.ToUInt32(sfd.ToArray().Reverse().ToArray());

                if (sbHeader.SequenceEqual(Constants.SuperBlockHeader))
                {
                    var byteOffset = NandAddressTranslationHelper.BCPToOffset(sp.Block, sp.Cluster, 0x0);
                    Console.WriteLine(
                        $"Found a superblock at Cluster 0x{i:X} Offset 0x{byteOffset:X} Version {sbVersion}");
                    foundSuperblocks.Add(new SuperBlockDescriptor(i, byteOffset, sbVersion));
                }
            }

            var mainSuperBlock = foundSuperblocks
                .OrderByDescending(x => x.Version)
                .First();

            Console.WriteLine(
                $"Candidate superblock with highest gen number: Cluster 0x{mainSuperBlock.Cluster:X} Offset 0x{mainSuperBlock.Offset:X} Version {mainSuperBlock.Version}");

            using var hmacsha1 = new HMACSHA1(KeyFile.NandHMACKey);

            // verify candidate superblock's HMAC and ECC

            var superBlockBuffer = new MemoryStream();

            Span<byte> sbSpare1 = null, sbSpare2 = null;

            for (var i = mainSuperBlock.Cluster; i <= mainSuperBlock.Cluster + 0xF; i++)
            {
                var addr2 = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(i);

                var cluster2 = NandDumpFile.Blocks[addr2.Block].Clusters[addr2.Cluster];

                superBlockBuffer.Write(cluster2.GetRawMainPageData());

                if (i != mainSuperBlock.Cluster + 0xF) continue;

                sbSpare1 = cluster2.Pages[0x6].SpareData;
            }

            var candidateSuperBlockHMAC = sbSpare1[0x1..0x15];

            var sbSalt = new byte[0x40];

            var sbStartClusterBytes = BitConverter.GetBytes(mainSuperBlock.Cluster);

            // this is correct way of generating the sb salt. verified on wiiqt.

            sbSalt[0x12] = sbStartClusterBytes[0x1];
            sbSalt[0x13] = sbStartClusterBytes[0x0];

            using var mm2 = new MemoryStream();
            var mainSuperBlockRaw = superBlockBuffer.ToArray();

            mm2.Write(sbSalt);
            mm2.Write(mainSuperBlockRaw);
            mm2.Position = 0x0;

            if (candidateSuperBlockHMAC.SequenceEqual(hmacsha1.ComputeHash(mm2)))
            {
                Console.WriteLine("Candidate Superblock has valid HMAC.");
            }

            mm2.Close();
            mm2.Dispose();

            var readableSuperBlock = mainSuperBlockRaw.CastToStruct<SuperBlock>();
            Console.WriteLine("Casting Candidate Superblock to a C# struct.");


            var ValidFSTEntries = new Dictionary<uint, RawFileSystemTableEntry>();
            var validClusters = new Dictionary<ushort, ushort>();

            var entryCount = 0x0u;
            foreach (var entry in readableSuperBlock.RawFileSystemTableEntries.Where(x => !x.IsEmpty))
            {
                ValidFSTEntries.Add(entryCount, entry);
                entryCount++;
            }

            Console.WriteLine($"Found {entryCount} Filesystem Table Entries.");

            var badClusters = 0x0u;
            var reservedClusters = 0x0u;
            var freeClusters = 0x0u;
            var chainLastClusters = 0x0u;

            ushort clusterIndex = 0x0;

            foreach (var clusterDesc in readableSuperBlock.ClusterEntries.Select(CastingHelper.Swap_Val))
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

                validClusters.Add(clusterIndex, clusterDesc);
                clusterIndex++;
            }

            Console.WriteLine($"Reserved Clusters:  {reservedClusters}");
            Console.WriteLine($"Bad Clusters:       {badClusters}");
            Console.WriteLine($"Free Clusters:      {freeClusters}");

            var nodes = new Dictionary<uint, RawFileSystemNode>();

            Console.WriteLine("Connecting File FST's to their clusters.");

            // process files first.
            foreach (var entry in ValidFSTEntries)
            {
                var rFST = entry.Value.ToReadableFST();

                if (!rFST.IsFile)
                {
                    var newDir = new RawFileSystemNode()
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

                    var newFile = new RawFileSystemNode()
                    {
                        IsFile = true,
                        Filename = rFST.FileName,
                        FSTEntry = rFST,
                        FSTEntryIndex = entry.Key,
                    };

                    newFile.Clusters.Add(startingCluster);

                    var nextCluster = startingCluster;

                    var pad = (int)Constants.NandClusterNoSpareByteSize;

                    var estimatedClusterCount = (rFST.FileSize + pad - 1) / pad;

                    var watchdogCounter = 0x0;

                    while (watchdogCounter < estimatedClusterCount && rFST.FileSize > 0)
                    {
                        var curCluster = validClusters[nextCluster];

                        if ((ClusterDescriptor)curCluster == ClusterDescriptor.ChainLast)
                            break;

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

                var sib = dirNode.Children[0x0].FSTEntry.Sib;

                var currentSib = sib;

                while (true)
                {
                    if (currentSib == Constants.FSTSubEndCapValue || !nodes.ContainsKey(currentSib))
                        break;

                    var curSibNode = nodes[currentSib];
                    curSibNode.Parent = curSibNode;
                    dirNode.Children.Add(curSibNode);
                    currentSib = curSibNode.FSTEntry.Sib;
                }
            }

            Console.WriteLine("Verifying File Clusters Integrity...");

            foreach (var entry in nodes)
            {
                var file = entry.Value;

                if (!file.IsFile) continue;
                if (file.FSTEntry.FileSize == 0)
                    continue;

                var saltF = new byte[0x40];

                var rawFST = entry.Value.FSTEntry.Source;
                rawFST.UserIDBigEndian.CopyTo(saltF.AsSpan()[..4]);
                rawFST.FileName.CopyTo(saltF.AsSpan()[0x4..]);

                var fstIndex = BitConverter.GetBytes(entry.Value.FSTEntryIndex).Reverse().ToArray();
                fstIndex.CopyTo(saltF.AsSpan().Slice(0x14, 4));
                rawFST.X3.CopyTo(saltF.AsSpan().Slice(0x18, 4));

                for (var i = 0x0; i < file.Clusters.Count; i++)
                {
                    var clusterIndex1 = BitConverter.GetBytes((uint)i).Reverse().ToArray();
                    clusterIndex1.CopyTo(saltF.AsSpan().Slice(0x10, 4));

                    var dataAdr = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(file.Clusters[i]);
                    var rawCluster = NandDumpFile.Blocks[dataAdr.Block].Clusters[dataAdr.Cluster];
                    var decryptedCluster = rawCluster.DecryptCluster(KeyFile);

                    using var mmx = new MemoryStream();
                    mmx.Write(saltF);
                    mmx.Write(decryptedCluster);
                    mmx.Position = 0;

                    var calculatedHMACx = (hmacsha1.ComputeHash(mmx));

                    var nandClusterHMACx = (rawCluster.Pages[0x6].SpareData[0x1..0x15]);

                    if (!calculatedHMACx.SequenceEqual(nandClusterHMACx))
                        Console.WriteLine($"{file.Filename} Cluster {file.Clusters[i]:x4} HMAC verification failed!");

                    if (!rawCluster.CheckECC())
                        Console.WriteLine($"{file.Filename} Cluster {file.Clusters[i]:x4} ECC verification failed!");
                }
            }

            Console.WriteLine("File Clusters Integrity Verified...");

            RootNode = nodes.First(x => x.Value.Filename == "/").Value;

            Console.WriteLine("Listing Files and Directories...");
            RootNode.PrintPretty();

            Console.WriteLine("Filesystem Integrity Verification Complete.");

            MainSuperBlockRaw = mainSuperBlockRaw;
            ValidClusters = validClusters;
            SuperBlockDescriptors = foundSuperblocks;
            MainSuperBlock = mainSuperBlock;
        }

        public SuperBlockDescriptor MainSuperBlock { get; set; }


        public void EraseAndReformat()
        {
            var superBlockTarget = MainSuperBlockRaw.CastToStruct<SuperBlock>();
            var validClustersLe = ValidClusters.Keys;
            var clusterDeleted = 0;


            Enumerable.Range(0, superBlockTarget.ClusterEntries.Length).Select(x => (ushort)x).ParallelForEachAsync(
                async i =>
                {
                    if (validClustersLe.Contains(i))
                    {
                        Console.Write($"\rDeleting cluster {clusterDeleted} of {validClustersLe.Count}");

                        // Mark cluster as free space.
                        superBlockTarget.ClusterEntries[i] = CastingHelper.Swap_Val((ushort)ClusterDescriptor.Empty);

                        
                        
                        // Actually delete the data.
                        // var addr = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(i);
                        // var target = NandDumpFile.Blocks[addr.Block].Clusters[addr.Cluster];
                        // target.EraseData(KeyFile);
                        clusterDeleted++;
                    }
                }).GetAwaiter().GetResult();
            Console.WriteLine("\r");
            Console.WriteLine("Cluster deletion complete.                 ");
            Console.WriteLine("Purging Filesystem Entries except for root.");

            var emptyFST = Constants.EmptyFST.CastToStruct<RawFileSystemTableEntry>();

            for (var i = 1; i < superBlockTarget.RawFileSystemTableEntries.Length; i++)
            {
                superBlockTarget.RawFileSystemTableEntries[i] = emptyFST;
            }

            Console.WriteLine("Capping root FST.");

            superBlockTarget.RawFileSystemTableEntries[0].SubBigEndian = BitConverter.GetBytes(0xFFFF);

            uint sbVersion = 0;

            // Rewrite every single superblocks to erase any trace of the old files.
            foreach (var desc in SuperBlockDescriptors)
            {
                Console.WriteLine(
                    $"Overwriting Superblock at Cluster 0x{desc.Cluster:X} Offset 0x{desc.Offset:X} Version {desc.Version}");
                Console.WriteLine($"Overwriting Version Number from {desc.Version} to {sbVersion++}");

                var curCluster = desc.Cluster;

                superBlockTarget.VersionBigEndian = BitConverter.GetBytes(sbVersion).Reverse().ToArray();

                var rawSB = superBlockTarget.CastToArray();

                for (var i = 0; i < Constants.SuperBlocksClusterIncrement; i++)
                {
                    var addr = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(
                        (uint)(curCluster + i));
                    var chunk = rawSB.AsSpan().Slice(i * (int)Constants.NandClusterNoSpareByteSize,
                        (int)Constants.NandClusterNoSpareByteSize).ToArray();

                    NandDumpFile.Blocks[addr.Block].Clusters[addr.Cluster]
                        .WriteData(chunk.ToArray());
                }

                SuperBlock.RecalculateHMAC(ref rawSB, NandDumpFile, KeyFile, curCluster);
            }

            NandProcessAndCheck();
        }
    }
}