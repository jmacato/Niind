using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandPage
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2048)]
        public byte[] MainData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] SpareData;
    }
}