using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyFile
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        private readonly byte[] Padding;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Boot1Hash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] CommonKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] ConsoleID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] EccPrivateKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] NandHMAC;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] NandAESKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] AES_PRNG_Seed;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] UnknownKeyID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public byte[] UnknownKeySig;
    }
}