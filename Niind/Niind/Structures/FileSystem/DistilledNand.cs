using System.Collections.Generic;

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
    }
}