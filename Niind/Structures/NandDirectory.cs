namespace Niind.Structures
{
    public class NandDirectory : NandNode
    {
        public NandDirectory(string fileName)
        {
            FileName = fileName;
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