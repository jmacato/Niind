using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        public NandCluster[] Clusters;
    }
}