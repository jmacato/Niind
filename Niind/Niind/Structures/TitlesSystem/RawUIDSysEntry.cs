using System.Runtime.InteropServices;
using Niind.Helpers;

namespace Niind.Structures.TitlesSystem;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RawUIDSysEntry
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public readonly byte[] TitleID;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public readonly byte[] UserID;

    public RawUIDSysEntry(ulong tid, uint uid)
    {
        TitleID = CastingHelper.Swap_BA(tid);
        UserID = CastingHelper.Swap_BA(uid);
    }
}