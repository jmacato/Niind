using System;
using System.IO;
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

        public ushort Sub { get; set; }
        public ushort Sib { get; set; }
        public uint FileSize { get; set; }
        public uint UserID { get; set; }
        public ushort GroupID { get; set; }
        public uint X3 { get; set; }

        public RawFileSystemTableEntry Source { get; }

        public RawFileSystemTableEntry ToRawFST()
        {
            var attr = (byte)(((OwnerPermissions & 3) << 6) |
                              ((GroupPermissions & 3) << 4) |
                              ((OtherPermissions & 3) << 2) |
                              ((IsDirectory ? 1 : 0) << 1) |
                              ( IsFile ? 1 : 0));

 
            var fileName = Encoding.ASCII.GetBytes(FileName);
            var fileNameBuf = new byte[0xC];
            fileName.CopyTo(fileNameBuf,0 );

            var sub = BitConverter.GetBytes(ByteWiseSwap(Sub));
            var sib = BitConverter.GetBytes(ByteWiseSwap(Sib));
            var fileSize = BitConverter.GetBytes(ByteWiseSwap( FileSize ));
            var uid = BitConverter.GetBytes(ByteWiseSwap(UserID));
            var gid = BitConverter.GetBytes(ByteWiseSwap(GroupID));
            var x3 = BitConverter.GetBytes(ByteWiseSwap(X3));

            return new RawFileSystemTableEntry(fileNameBuf, attr, sub, sib, fileSize, uid, gid, x3);
        }

        public ReadableFileSystemTableEntry(RawFileSystemTableEntry rawFST)
        {
            Source = rawFST;

            // Access Mode/Attribute's bitwise structure
            // 0bLLMMNNFD
            // L = Owner Permissions (2 bits)
            // M = Group Permissions (2 bits)
            // N = Other Permissions (2 bits)
            // F = Is a file (1 bit)
            // D = Is a directory (1 bit)

            var isFile = (rawFST.Attributes & 0b0000_0011) == 0b_1;
            var isDirectory = (rawFST.Attributes & 0b0000_0011) == 0b00000_0010;
            var ownerPermissions = (byte)((rawFST.Attributes & 0b1100_0000) >> 0b0000_0110);
            var groupPermissions = (byte)((rawFST.Attributes & 0b0011_0000) >> 0b0000_0100);
            var otherPermissions = (byte)((rawFST.Attributes & 0b0000_1100) >> 0b0000_0010);

            var fileName = Encoding.ASCII.GetString(rawFST.FileName).Trim(char.MinValue);

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

            Sub = sub;
            Sib = sib;
            FileSize = fileSize;
            UserID = uid;
            GroupID = gid;
            X3 = x3;
        }

        static uint UIntBAToLittleEndian(byte[] input) =>
            BitConverter.ToUInt32(input.ToArray().Reverse().ToArray());

        static ushort UShortToLittleEndian(byte[] input) =>
            BitConverter.ToUInt16(input.ToArray().Reverse().ToArray());
        
        
        public static ushort ByteWiseSwap(ushort value)
        {
            return (ushort)((0x00FF & (value >> 8))
                            | (0xFF00 & (value << 8)));
        }

        public static uint ByteWiseSwap(uint value)
        {
            uint swapped = (0x000000FF) & (value >> 24)
                           | (0x0000FF00) & (value >> 8)
                           | (0x00FF0000) & (value << 8)
                           | (0xFF000000) & (value << 24);
            return swapped;
        }
    }
}