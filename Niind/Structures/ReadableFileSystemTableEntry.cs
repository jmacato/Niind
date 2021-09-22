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

        public ReadableFileSystemTableEntry(bool isFile, bool isDirectory, byte ownerPermissions,
            byte groupPermissions, byte otherPermissions, string fileName, byte attributes, ushort sub, ushort sib,
            uint fileSize, uint uid, ushort gid, uint x3)
        {
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
    }
}