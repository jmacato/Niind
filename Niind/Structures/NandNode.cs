using System;
using System.Collections.Generic;
using System.Linq;

namespace Niind.Structures
{
    public abstract class NandNode
    {
        public string FileName { get; set; }
        public int FSTIndex { get; set; }
        public NandNode Parent { get; set; }
        public List<NandNode> Children { get; } = new();

        public ushort SubordinateIndex { get; set; } = 0xFFFF;

        public ushort SiblingIndex { get; set; } = 0xFFFF;
        public NodePerm Owner { get; set; }
        public NodePerm Group { get; set; }
        public NodePerm Other { get; set; }
        public uint UserID { get; set; }
        public ushort GroupID { get; set; }

        public abstract ReadableFileSystemTableEntry Materialize();

        private static NandDirectory CreateDirectory(
            NandRootDir rootDir,
            IEnumerable<string> fragments,
            uint userID = 0,
            ushort groupID = 0,
            NodePerm owner = NodePerm.RW,
            NodePerm group = NodePerm.RW,
            NodePerm other = NodePerm.None)
        {
            var curNode = rootDir as NandDirectory;
            var xas = fragments.ToString();
            foreach (var fragment in fragments)
            {
                if (curNode.Children.FirstOrDefault(x => x.FileName == fragment) is NandDirectory existingDir)
                {
                    curNode = existingDir;
                    continue;
                }

                var newNode = new NandDirectory(fragment)
                {
                    Parent = curNode,
                    Owner = owner,
                    Group = group,
                    Other = other,
                    UserID = userID,
                    GroupID = groupID
                };

                curNode.Children.Add(newNode);

                curNode = newNode;
            }

            return curNode;
        }

        public static NandDirectory CreateDirectory(NandRootDir rootDir, string path,
            uint userID = 0,
            ushort groupID = 0,
            NodePerm owner = NodePerm.RW,
            NodePerm group = NodePerm.RW,
            NodePerm other = NodePerm.None)
        {
            if (path == "/") return rootDir;

            var fragments = path.Split("/").Where(x => !string.IsNullOrEmpty(x) || !string.IsNullOrWhiteSpace(x));
            var dir = CreateDirectory(rootDir, fragments, userID, groupID, owner, group, other);

            return dir;
        }

        public static NandFile CreateFile(NandRootDir rootDir, string path, Memory<byte> data, uint userID = 0,
            ushort groupID = 0,
            NodePerm owner = NodePerm.RW, NodePerm group = NodePerm.Read,
            NodePerm other = NodePerm.None)
        {
            var fragments = path.Split("/").Where(x => !string.IsNullOrEmpty(x) || !string.IsNullOrWhiteSpace(x));
            var fileName = fragments.Last();
            var dirNode = CreateDirectory(rootDir, fragments.Take(fragments.Count() - 1), userID, groupID, owner, group,
                other);
            var newFile = new NandFile(fileName, data);
            dirNode.Children.Add(newFile);
            newFile.Parent = dirNode;
            newFile.Owner = owner;
            newFile.Group = group;
            newFile.Other = other;
            newFile.UserID = userID;
            newFile.GroupID = groupID;
            return newFile;
        }

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