using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BootMiiMetadata
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        public byte[] HeaderString;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x80)]
        public byte[] OTPData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x80)]
        private readonly byte[] Padding0;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        public byte[] SEEPROMData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        private readonly byte[] Padding1;
    }
}