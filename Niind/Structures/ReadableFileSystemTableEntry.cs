using System;
using System.Linq;
using System.Text;

namespace Niind.Structures
{
    public struct ReadableFileSystemTableEntry
    {
        public bool IsFile { get; set; }
        public bool IsDirectory { get; set; }
        public byte OwnerPermissions { get; set; }
        public byte GroupPermissions { get; set; }
        public byte OtherPermissions { get; set; }
        public string FileName { get; set; }
        public byte Attributes { get; set; }
        public ushort Sub { get; set; }
        public ushort Sib { get; set; }
        public uint FileSize { get; set; }
        public uint Uid { get; set; }
        public ushort Gid { get; set; }
        public uint X3 { get; set; }
 
        public RawFileSystemTableEntry Source { get; }

        public ReadableFileSystemTableEntry(RawFileSystemTableEntry rawFST)
        {
            Source = rawFST;
            
            // Access Mode's bitwise structure
            // 0bLLMMNNFD
            // L = Owner Permissions (2 bits)
            // M = Group Permissions (2 bits)
            // N = Other Permissions (2 bits)
            // F = Is a file (1 bit)
            // D = Is a directory (1 bit)

            var isFile = (rawFST.AccessMode & 0b0000_0011) == 0b_1;
            var isDirectory = (rawFST.AccessMode & 0b0000_0011) == 0b00000_0010;
            var ownerPermissions = (byte)((rawFST.AccessMode & 0b1100_0000) >> 0b0000_0110);
            var groupPermissions = (byte)((rawFST.AccessMode & 0b0011_0000) >> 0b0000_0100);
            var otherPermissions = (byte)((rawFST.AccessMode & 0b0000_1100) >> 0b0000_0010);
            
            
            var fileName = Encoding.ASCII.GetString(rawFST.FileName).Trim(char.MinValue);
            
            var attributes = rawFST.Attributes;
            var sub = UShortToLittleEndian(rawFST.SubBigEndian);
            var sib = UShortToLittleEndian(rawFST.SibBigEndian);
            var fileSize = UIntBAToLittleEndian(rawFST.FileSizeBigEndian);
            var uid = UIntBAToLittleEndian(rawFST.UserIDBigEndian);
            var gid = UShortToLittleEndian(rawFST.GroupIDBigEndian);
            var x3 = UIntBAToLittleEndian(rawFST.X3);
            
            IsFile = isFile;
            IsDirectory = isDirectory;
            OwnerPermissions = ownerPermissions;
            GroupPermissions = groupPermissions;
            OtherPermissions = otherPermissions;
            FileName = fileName;
            Attributes = attributes;
            Sub = sub;
            Sib = sib;
            FileSize = fileSize;
            Uid = uid;
            Gid = gid;
            X3 = x3;
        }
        
        static uint UIntBAToLittleEndian(byte[] input) =>
            BitConverter.ToUInt32(input.ToArray().Reverse().ToArray());

        static ushort UShortToLittleEndian(byte[] input) =>
            BitConverter.ToUInt16(input.ToArray().Reverse().ToArray());
    }
}