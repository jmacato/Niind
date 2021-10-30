using System;

namespace Niind.Structures.FileSystem
{
    [Flags]
    public enum NodePerm : byte
    {
        None = 0,
        Read = 1,
        Write = 2,
        RW = Read | Write
    }
}