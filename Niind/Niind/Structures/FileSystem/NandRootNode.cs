using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Niind.Helpers;

namespace Niind.Structures.FileSystem
{
    public sealed class NandRootNode : NandDirectoryNode
    {
        private readonly DistilledNand _distilledNand;

        public DistilledNand DistilledNand => _distilledNand;

        public NandRootNode(DistilledNand distilledNand) : base("/")
        {
            _distilledNand = distilledNand;
        }

        public readonly List<ushort> UsedClusters = new() { 0 };

        private NandDirectoryNode CreateDirectory(
            IEnumerable<string> fragments,
            uint userID = 0,
            ushort groupID = 0,
            NodePerm owner = NodePerm.RW,
            NodePerm group = NodePerm.RW,
            NodePerm other = NodePerm.None)
        {
            var curNode = this as NandDirectoryNode;
            var xas = fragments.ToString();
            foreach (var fragment in fragments)
            {
                if (curNode.Children.FirstOrDefault(x => x.FileName == fragment) is NandDirectoryNode existingDir)
                {
                    curNode = existingDir;
                    continue;
                }

                var newNode = new NandDirectoryNode(fragment)
                {
                    Parent = curNode,
                    Owner = owner,
                    Group = group,
                    Other = other,
                    UserID = userID,
                    GroupID = groupID
                };

                curNode.Children.AddFirst(newNode);

                curNode = newNode;
            }

            return curNode;
        }

        public NandDirectoryNode CreateDirectory(string path,
            uint userID = 0,
            ushort groupID = 0,
            NodePerm owner = NodePerm.RW,
            NodePerm group = NodePerm.RW,
            NodePerm other = NodePerm.None)
        {
            if (path == "/") return this;

            var fragments = ProcessPathToFragments(path);
            var dir = CreateDirectory(fragments, userID, groupID, owner, group, other);

            return dir;
        }

        private IEnumerable<string> ProcessPathToFragments(string path)
        {
            return path.Split("/").Where(x => !string.IsNullOrEmpty(x) || !string.IsNullOrWhiteSpace(x));
        }

        public NandFileNode CreateFile(string path, Memory<byte> data, uint userID = 0,
            ushort groupID = 0,
            NodePerm owner = NodePerm.RW, NodePerm group = NodePerm.Read,
            NodePerm other = NodePerm.None)
        {
            var fragments = ProcessPathToFragments(path);
            var fileName = fragments.Last();
            var dirNode = CreateDirectory(fragments.Take(fragments.Count() - 1), userID, groupID, owner, group,
                other);
            var newFile = new NandFileNode(fileName, data);
            dirNode.Children.AddFirst(newFile);
            newFile.Parent = dirNode;
            newFile.Owner = owner;
            newFile.Group = group;
            newFile.Other = other;
            newFile.UserID = userID;
            newFile.GroupID = groupID;

            var clustersNeeded = (int)(data.Length / Constants.NandClusterNoSpareByteSize) + 1;

            var fileClusters = _distilledNand
                .ValidClusters
                .Where(x => (ClusterDescriptor)x.Value == ClusterDescriptor.Empty &&
                            !UsedClusters.Contains(x.Key)) // select empty clusters
                .Select(x => x.Key) // select cluster address
                .OrderBy(x => x) // reduce fragmentation
                .Take(clustersNeeded)
                .ToList();

            UsedClusters.AddRange(fileClusters);

            newFile.AllocatedClusters = fileClusters;
            newFile.SubordinateIndex = newFile.AllocatedClusters.First();

            return newFile;
        }

        public static ushort ByteWiseSwap(ushort value)
        {
            return (ushort)((0x00FF & (value >> 8))
                            | (0xFF00 & (value << 8)));
        }

        public static void NandWriteSuperBlock(DistilledNand distilledNand, SuperBlock superBlockTarget)
        {
            uint sbVersion = 0;

            // Rewrite every single superblocks to erase any trace of the old files.
            foreach (var desc in distilledNand.SuperBlockDescriptors)
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

                    distilledNand.NandDumpFile.Blocks[addr.Block].Clusters[addr.Cluster]
                        .WriteData(chunk.ToArray());
                }

                SuperBlock.RecalculateHMAC(ref rawSB, distilledNand.NandDumpFile, distilledNand.KeyFile, curCluster);
            }
        }

        public DistilledNand WriteAndCommitToNand()
        {
            // Flatten the hierarchy.
            // Place the directories first in the
            // flattened list.
            var nodeList = GetDescendants()
                .OrderBy(x => x.FileName)
                .OrderByDescending(x => x is NandDirectoryNode)
                .ToList();

            // Root node should be on top.
            nodeList.Insert(0, this);

            UsedClusters.Clear();
            UsedClusters.Add(0);

            // Set FST Indices inside the nodes themselves so we dont have to 
            // keep track of them everytime.
            for (int i = 0; i < nodeList.Count; i++)
            {
                nodeList[i].FSTIndex = i;
            }

            foreach (var node in nodeList)
            {
                // If it's a directory, set its sub to the last child
                if (node.Children.Count != 0)
                {
                    if (node is NandFileNode) throw new InvalidOperationException("Files cannot have child nodes!");
                    var firstChild = node.Children.First();
                    node.SubordinateIndex = (ushort)firstChild.FSTIndex;
                }

                // If the node has a parent, set its sibling to the
                // next item in the parent's children list.
                if (node.Parent is not null)
                {
                    var children = node.Parent.Children;
                    var limit = children.Count;
                    var curNode = node.Parent.Children.First;

                    while (true)
                    {
                        var sibling = curNode.Next;

                        curNode.Value.NodeConnected = true;

                        if (sibling is not null)
                        {
                            curNode.Value.SiblingIndex = (ushort)sibling.Value.FSTIndex;
                        }
                        else
                        {
                            break;
                        }

                        curNode = sibling;
                    }
                }
            }

            _distilledNand.EraseAndReformat();

            var superBlockTarget = _distilledNand.MainSuperBlockRaw.CastToStruct<SuperBlock>();

            foreach (var node in nodeList)
            {
                var fst = node.Materialize().ToRawFST();
                superBlockTarget.RawFileSystemTableEntries[node.FSTIndex] = fst;

                if (node is not NandFileNode file) continue;

                var orderedClusters = new LinkedList<ushort>(file.AllocatedClusters.OrderBy(x => x));

                var cur = orderedClusters.First;
                var curClusterVal = cur.Value;
                var clusterIndex = 0;
                while (cur is not null)
                {
                    var nextCluster = cur.Next;

                    if (nextCluster is null)
                    {
                        superBlockTarget.ClusterEntries[curClusterVal] =
                            CastingHelper.Swap_Val((ushort)ClusterDescriptor.ChainLast);
                    }
                    else
                    {
                        superBlockTarget.ClusterEntries[curClusterVal] = CastingHelper.Swap_Val(nextCluster.Value);
                    }

                    var (block, cluster) =
                        NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(curClusterVal);

                    var chunkLen = (int)Math.Min(Constants.NandClusterNoSpareByteSize, file.RawData.Length);

                    var chunk = file.RawData.Slice((int)(clusterIndex * Constants.NandClusterNoSpareByteSize),
                        chunkLen).ToArray();
                    
                    EncryptionHelper.PadByteArrayToMultipleOf(ref chunk, (int)Constants.NandClusterNoSpareByteSize);
                    
                    var targetCluster = _distilledNand.NandDumpFile.Blocks[block].Clusters[cluster];

                    targetCluster.WriteDataAsEncrypted(_distilledNand.KeyFile, chunk);

                    RecalculateFileHMAC(fst, _distilledNand.KeyFile, (uint)file.FSTIndex, curClusterVal,
                        targetCluster);
                    
                    clusterIndex++;

                    cur = nextCluster;
                }
            }

            NandWriteSuperBlock(_distilledNand, superBlockTarget);

            return _distilledNand;
        }


        private void RecalculateFileHMAC(RawFileSystemTableEntry entry, KeyFile keyData, uint entryIndex,
            uint clusterIndex, NandCluster targetCluster)
        {
            var saltF = new byte[0x40];
            entry.UserIDBigEndian.CopyTo(saltF.AsSpan()[..4]);
            entry.FileName.CopyTo(saltF.AsSpan()[0x4..]);
            var fstIndex = CastingHelper.Swap_BA(entryIndex);
            fstIndex.CopyTo(saltF.AsSpan().Slice(0x14, 4));
            entry.X3.CopyTo(saltF.AsSpan().Slice(0x18, 4));

            var c = BitConverter.GetBytes(clusterIndex).Reverse().ToArray();
            c.CopyTo(saltF.AsSpan().Slice(0x10, 4));

            using var hmac = new HMACSHA1(keyData.NandHMACKey);
            using var mm = new MemoryStream();
            mm.Write(saltF);
            mm.Write(targetCluster.DecryptCluster(keyData));
            mm.Position = 0;

            var newHMAC = hmac.ComputeHash(mm);
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