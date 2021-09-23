using System;

namespace Niind.Structures
{
    public class NandFile : NandNode
    {
        public NandFile(string name, Memory<byte> data)
        {
            FileName = name[..0x0c];
            RawData = data;
        }

        public Memory<byte> RawData { get; }

        public override ReadableFileSystemTableEntry Materialize()
        {
            return new ReadableFileSystemTableEntry
            {
                FileName = FileName,
                FileSize = (uint)RawData.Length,
                OwnerPermissions = (byte)Owner,
                GroupPermissions = (byte)Group,
                OtherPermissions = (byte)Other,
                UserID = UserID,
                GroupID = GroupID,
                IsDirectory = false,
                IsFile = true,
                Sib = SiblingIndex,
                Sub = SubordinateIndex
            };
        }
    }
}