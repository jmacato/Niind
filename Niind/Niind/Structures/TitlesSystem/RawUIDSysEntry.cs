using System.Runtime.InteropServices;
using Niind.Helpers;

namespace Niind.Structures.TitlesSystem;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RawUIDSysEntry
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public readonly byte[] TitleID;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public readonly byte[] Padding;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public readonly byte[] UserID;

    public RawUIDSysEntry(ulong tid, int uid)
    {
        TitleID = CastingHelper.Swap_BA(tid);
        Padding = new byte[2];
        UserID = CastingHelper.Swap_BA((ushort)uid);
    }
}