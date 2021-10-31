using System;
using System.Collections.Generic;
using System.Linq;
using Niind.Helpers;

namespace Niind.Structures.FileSystem
{
    public readonly struct DistilledNand
    {
        public readonly byte[] MainSuperBlockRaw;
        public readonly Dictionary<ushort, ushort> ValidClusters;
        public readonly List<SuperBlockDescriptor> SuperBlockDescriptors;
        public readonly SuperBlockDescriptor MainSuperBlock;
        public readonly RawFileSystemNode RootNode;

        public readonly Dictionary<int, ReadableFileSystemTableEntry> FileSystemTable;

        public readonly NandDumpFile NandDumpFile;
        public readonly KeyFile KeyFile;

        public DistilledNand(NandDumpFile nandDumpFile, KeyFile keyFile, List<SuperBlockDescriptor> foundSuperblocks,
            SuperBlockDescriptor mainSuperBlock,
            byte[] mainSuperBlockRaw,
            RawFileSystemNode rootNode,
            Dictionary<ushort, ushort> validClusters)
        {
            NandDumpFile = nandDumpFile;
            KeyFile = keyFile;
            MainSuperBlockRaw = mainSuperBlockRaw;
            ValidClusters = validClusters;
            SuperBlockDescriptors = foundSuperblocks;
            MainSuperBlock = mainSuperBlock;
            RootNode = rootNode;
            FileSystemTable = new Dictionary<int, ReadableFileSystemTableEntry>();
        }
        
        public void EraseAndReformat()
        {
            var superBlockTarget = MainSuperBlockRaw.CastToStruct<SuperBlock>();
            var validClustersLe = ValidClusters.Keys;
            var clusterDeleted = 0;
            
            // Erase all used clusters.
            for (ushort i = 0; i < superBlockTarget.ClusterEntries.Length; i++)
            {
                if (validClustersLe.Contains(i))
                {
                    Console.Write($"\rDeleting cluster {clusterDeleted} of {validClustersLe.Count}");
                    // Mark cluster as free space.
                    var old = superBlockTarget.ClusterEntries[i];
                    superBlockTarget.ClusterEntries[i] = CastingHelper.Swap_Val((ushort)ClusterDescriptor.Empty);
                    var newx = superBlockTarget.ClusterEntries[i];

                    // Actually delete the data.
                    var addr = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(i);
                    var target = NandDumpFile.Blocks[addr.Block].Clusters[addr.Cluster];
                    target.EraseData(KeyFile);
                    clusterDeleted++;
                }
            }

            Console.WriteLine("Cluster deletion complete.");
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
                        (int)Constants.NandClusterNoSpareByteSize);

                     NandDumpFile.Blocks[addr.Block].Clusters[addr.Cluster]
                        .WriteDataNoEncryption(chunk.ToArray());
                }

                SuperBlock.RecalculateHMAC(ref rawSB,  NandDumpFile,  KeyFile, curCluster);
            }
        }
    }
}