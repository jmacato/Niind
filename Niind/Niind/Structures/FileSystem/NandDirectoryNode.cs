namespace Niind.Structures.FileSystem
{
    public class NandDirectoryNode : NandNode
    {
        public NandDirectoryNode(string fileName)
        {
            FileName = fileName.PadRight(0xc, char.MinValue)[..0x0c].Trim(char.MinValue);
        }

        public override ReadableFileSystemTableEntry Materialize()
        {
            return new ReadableFileSystemTableEntry
            {
                FileName = FileName,
                OwnerPermissions = (byte)Owner,
                GroupPermissions = (byte)Group,
                OtherPermissions = (byte)Other,
                UserID = UserID,
                GroupID = GroupID,
                IsDirectory = true,
                IsFile = false, 
                Sib = SiblingIndex, 
                Sub = SubordinateIndex
            };
        }
    }
}