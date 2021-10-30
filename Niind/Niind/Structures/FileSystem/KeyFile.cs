using System.Runtime.InteropServices;

namespace Niind.Structures.FileSystem
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

        // PartialEccPrivateKey and NandHMACKey overlaps in between by 2 bytes by some stupid reason...
        // So we're cutting it out of PartialEccPrivateKey since we need the NAND HMAC Key more.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1C)]
        public byte[] PartialEccPrivateKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        public byte[] NandHMACKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] NandAESKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] AES_PRNG_Seed;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        public byte[] UnknownKeyID;

        // public byte[] ProperNANDHMacKey { get; } 
    }
}