namespace Niind.Structures
{
    public readonly struct SuperBlockDescriptor
    {
        public readonly ushort Cluster;
        public readonly long Offset;
        public readonly uint Version;

        public SuperBlockDescriptor(ushort cluster, long offset, uint version)
        {
            Cluster = cluster;
            Offset = offset;
            Version = version;
        }
    }
}