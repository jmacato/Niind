using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BootMiiMetadata
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] HeaderString;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] OTPData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        private readonly byte[] Padding0;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] SEEPROMData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        private readonly byte[] Padding1;
    }
}