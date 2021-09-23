using System.ComponentModel;
using System.Linq;

namespace Niind.Structures
{
    public sealed class NandRootDir : NandDirectory
    {
        public NandRootDir() : base("/")
        {
        }

        public void ToConnectedTable(DistilledNand distilledNand)
        {
            // Flatten the hierarchy.
            var nodeList = GetDescendants()
                .OrderBy(x => x.FileName)
                .OrderByDescending(x => x is NandDirectory)
                .ToList();

            // Root node should be on top.
            nodeList.Insert(0, this);


            for (int i = 0; i < nodeList.Count; i++)
            {
                nodeList[i].FSTIndex = i;
            }

            foreach (var node in nodeList)
            {
                if (node.Children.Count != 0)
                {
                    var firstChild = node.Children[0];
                    node.SubordinateIndex = (ushort)firstChild.FSTIndex;
                }

                if (node.Parent is not null)
                {
                    var sibPos = node.Parent.Children.IndexOf(node) + 1;
                    
                    if (sibPos == node.Parent.Children.Count)
                        continue;

                    node.SiblingIndex = (ushort)node.Parent.Children[sibPos].FSTIndex;
                }
                    
            }
        }
    }
}