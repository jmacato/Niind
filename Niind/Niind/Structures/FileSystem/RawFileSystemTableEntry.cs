using System.Linq;
using System.Runtime.InteropServices;
using Niind.Helpers;

namespace Niind.Structures.FileSystem
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RawFileSystemTableEntry
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xC)]
        public byte[] FileName;

        public byte Attributes;

        public byte Unused;

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

        public RawFileSystemTableEntry(byte[] fileNameBuf, byte attr, byte[] sub, byte[] sib, byte[] fileSize,
            byte[] uid, byte[] gid, byte[] x3)
        {
            FileName = fileNameBuf;
            Attributes = attr;
            SubBigEndian = sub;
            SibBigEndian = sib;
            FileSizeBigEndian = fileSize;
            UserIDBigEndian = uid;
            GroupIDBigEndian = gid;
            X3 = x3;
            Unused = 0;
        }

        public bool IsEmpty => this.CastToArray().SequenceEqual(Constants.EmptyFST);

        public ReadableFileSystemTableEntry ToReadableFST()
        {
            return new ReadableFileSystemTableEntry(this);
        }
    }
}