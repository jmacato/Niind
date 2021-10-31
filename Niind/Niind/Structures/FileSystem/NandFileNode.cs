using System;
using System.Collections.Generic;

namespace Niind.Structures.FileSystem
{
    public class NandFileNode : NandNode
    {
        public NandFileNode(string fileName, byte[] data)
        {
            FileName = fileName.PadRight(0xc, char.MinValue)[..0x0c].Trim(char.MinValue);
            RawData = data;
        }

        public byte[] RawData { get; }
        public List<ushort> AllocatedClusters { get; set; }

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