using System.Collections.Generic;

namespace Niind.Structures.FileSystem
{
    public abstract class NandNode
    {
        public string FileName { get; set; }
        public int FSTIndex { get; set; }
        public NandNode Parent { get; set; }
        public LinkedList<NandNode> Children { get; } = new();

        public ushort SubordinateIndex { get; set; } = 0xFFFF;

        public ushort SiblingIndex { get; set; } = 0xFFFF;
        public NodePerm Owner { get; set; }
        public NodePerm Group { get; set; }
        public NodePerm Other { get; set; }
        public uint UserID { get; set; }
        public ushort GroupID { get; set; }

        public bool NodeConnected { get; set; }

        public abstract ReadableFileSystemTableEntry Materialize();

        public IEnumerable<NandNode> GetDescendants()
        {
            foreach (var child in Children)
            {
                yield return child;
                foreach (var descendant in child.GetDescendants())
                {
                    yield return descendant;
                }
            }
        }
    }
}