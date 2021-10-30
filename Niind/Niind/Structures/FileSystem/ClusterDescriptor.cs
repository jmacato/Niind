namespace Niind.Structures.FileSystem
{
    public enum ClusterDescriptor : ushort
    {
        // last cluster within a chain
        ChainLast = 0xFFFB,

        // reserved cluster
        Reserved = 0xFFFC,

        // bad block (marked at factory)  
        Bad = 0xFFFD,

        // empty (unused / available) space
        Empty = 0xFFFE
    }
}