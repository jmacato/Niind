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
            // Access Mode's bitwise structure
            // 0bLLMMNNFD
            // L = Owner Permissions (2 bits)
            // M = Group Permissions (2 bits)
            // N = Other Permissions (2 bits)
            // F = Is a file (1 bit)
            // D = Is a directory (1 bit)

            var isFile = (AccessMode & 0b0000_0011) == 0b_1;
            var isDirectory = (AccessMode & 0b0000_0011) == 0b00000_0010;
            var OwnerPermissions = (byte)((AccessMode & 0b1100_0000) >> 0b0000_0110);
            var GroupPermissions = (byte)((AccessMode & 0b0011_0000) >> 0b0000_0100);
            var OtherPermissions = (byte)((AccessMode & 0b0000_1100) >> 0b0000_0010);
            
            
            var fileName = Encoding.ASCII.GetString(FileName).Trim(char.MinValue);
            
            var attributes = Attributes;
            var sub = UShortToLittleEndian(SubBigEndian);
            var sib = UShortToLittleEndian(SibBigEndian);
            var fileSize = UIntBAToLittleEndian(FileSizeBigEndian);
            var uid = UIntBAToLittleEndian(UserIDBigEndian);
            var gid = UShortToLittleEndian(GroupIDBigEndian);
            var x3 = UIntBAToLittleEndian(X3);

            return new ReadableFileSystemTableEntry(isFile, isDirectory, OwnerPermissions,
                GroupPermissions, OtherPermissions, fileName, attributes, sub, sib, fileSize, uid, gid, x3);
        }

        static uint UIntBAToLittleEndian(byte[] input) =>
            BitConverter.ToUInt32(input.ToArray().Reverse().ToArray());

        static ushort UShortToLittleEndian(byte[] input) =>
            BitConverter.ToUInt16(input.ToArray().Reverse().ToArray());
    }
}