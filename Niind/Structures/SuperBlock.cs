using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SuperBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3)]
        public uint[] SuperBlockHeader;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8000)]
        public ushort[] ClusterEntries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6140)] // ? Idk if this is the absolute limit
        public RawFileSystemTableEntry[] RawFileSystemTableEntries;
    }
}