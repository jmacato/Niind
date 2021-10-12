using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandDumpFile
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1000)]
        public NandBlock[] Blocks;

        public BootMiiMetadata BootMiiFooterBlock;
    }
}