using System;

namespace Niind.Helpers
{
    public static class NandAddressTranslationHelper
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