using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RawFileSystemTableEntry
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xC)]
        public byte[] FileName;

        public byte AccessMode;
        public byte Attributes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] SubBigEndian;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] SibBigEndian;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] FileSizeBigEndian;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] UserIDBigEndian;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] GroupIDBigEndian;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] X3;

        public bool IsEmpty => this.CastToArray().SequenceEqual(Constants.EmptyFST);

        public ReadableFileSystemTableEntry ToReadableFST()
        {
            return new ReadableFileSystemTableEntry(this);
        }

    }
}