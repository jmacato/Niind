using System;
using System.Collections.Generic;

namespace Niind.Structures
{
    public class FileSystemNode
    {
        public string Filename;
        public bool IsFile;
        public List<ushort> Clusters = new();
        public List<FileSystemNode> Children = new();
        public ReadableFileSystemTableEntry FSTEntry;
        public uint FSTEntryIndex;

        public void PrintPretty(string indent = "  ", bool last = false)
        {
            Console.Write(indent);

            Console.Write("|-");
            indent += "| ";


            Console.WriteLine(Filename + (IsFile ? "" : "/"));

            for (int i = 0; i < Children.Count; i++)
                Children[i].PrintPretty(indent, i == Children.Count - 1);
        }
    }
}