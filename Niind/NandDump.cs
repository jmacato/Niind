using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandDump
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
        public NandBlock[] Blocks;

        public BootMiiMetadata BootMiiFooterBlock;
    }
}