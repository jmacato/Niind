using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandCluster
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public NandPage[] Pages;
    }
}