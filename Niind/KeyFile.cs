using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyFile
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        private readonly byte[] Padding;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        public byte[] Boot1Hash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] CommonKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
        public byte[] ConsoleID;

        // EccPrivateKey and NandHMAC overlaps in between by 2 bytes by some stupid reason...
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1C)]
        public byte[] EccPrivateKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        public byte[] NandHMAC;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] NandAESKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] AES_PRNG_Seed;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x08)]
        public byte[] UnknownKeyID; 
    }
}