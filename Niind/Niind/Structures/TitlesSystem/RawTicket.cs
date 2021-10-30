using System.Runtime.InteropServices;

namespace Niind.Structures.TitlesSystem
{
    public struct RawTicket
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x104)]
        public byte[] SignatureType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x03C)]
        public byte[] Padding0;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x040)]
        public byte[] Issuer;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x03C)]
        public byte[] ECDHData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x003)]
        public byte[] Padding1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x010)]
        public byte[] TitleKeyEnc;


        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x001)]
        public byte[] unk;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x008)]
        public byte[] TicketID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x004)]
        public byte[] ConsoleID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x008)]
        public byte[] TitleID_KeyIV;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x002)]
        public byte[] Padding2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x002)]
        public byte[] TicketTitleVersion;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x004)]
        public byte[] PermittedTitlesMask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x008)]
        public byte[] PermitMask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x001)]
        public byte[] IsTitleExportAllowed;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x001)]
        public byte[] CommonKeyIndex;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x030)]
        public byte[] Padding3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x040)]
        public byte[] ContentAccessPermissions;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x002)]
        public byte[] Padding4;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x004)]
        public byte[] EnableTimeLimit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x004)]
        public byte[] TimeLimit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x038)]
        public byte[] TimeLimitStructs;
    }
}