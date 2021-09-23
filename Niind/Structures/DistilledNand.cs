using System.Collections.Generic;

namespace Niind.Structures
{
    public readonly struct DistilledNand
    {
        public readonly byte[] MainSuperBlockRaw;
        public readonly Dictionary<ushort, ushort> ValidClusters;
        public readonly List<SuperBlockDescriptor> SuperBlockDescriptors;
        public readonly SuperBlockDescriptor MainSuperBlock;
        public readonly FileSystemNode RootNode;

        public DistilledNand(List<SuperBlockDescriptor> foundSuperblocks,
            SuperBlockDescriptor mainSuperBlock,
            byte[] mainSuperBlockRaw,
            FileSystemNode rootNode,
            Dictionary<ushort, ushort> validClusters)
        {
            MainSuperBlockRaw = mainSuperBlockRaw;
            ValidClusters = validClusters;
            SuperBlockDescriptors = foundSuperblocks;
            MainSuperBlock = mainSuperBlock;
            RootNode = rootNode;
        }
    }
}