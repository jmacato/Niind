using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Niind
{
    public class NintendoUpdateServerDownloader
    {
        public void GetUpdate()
        {
            using var client = new WebClient();

            client.Headers["User-Agent"] = Constants.UpdaterUserAgent;

            foreach (var titles in Constants.Version4_3U_Titles)
            {
                var titleID = $"{titles.TicketID:X4}".PadLeft(16, '0');
                var tmdVersion = $"tmd.{titles.Version}";

                var downloadTmdUri = new Uri(Constants.NUSBaseUrl + titleID + "/" + tmdVersion);
                var x = client.DownloadData(downloadTmdUri);


                var xsdasdas = RawTitleMetadata.FromByteArray(x);


                // var downloadCetkUri = new Uri(Constants.NUSBaseUrl + titleID + "/cetk");
                // var y = client.DownloadData(downloadCetkUri);
            }
        }

        public enum SignatureType : uint
        {
            RSA_2048 = 0x00010001u,
            RSA_4096 = 0x00010000u
        };


        public struct RawTitleMetadataContentDescriptor
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] ContentID;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Index;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Type;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Size;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] SHA1;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawTitleMetadata
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] SignatureType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] Signature;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            public byte[] Padding0;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] Issuer;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] Version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] ca_crl_version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] signer_crl_version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] IsVWii;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] SystemVersion;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] TitleID;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] TitleType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] GroupID;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Zero;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Region;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Ratings;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] Padding2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] IPCMask;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
            public byte[] Reserved;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] AccessRights;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] TitleVersion;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] NumberOfContents;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] BootIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Unused;

            public IntPtr RawContentData;

            public static RawTitleMetadata FromByteArray(byte[] bytes)
            {
                var g = bytes.CastToStruct<RawTitleMetadata>();

                var sub = CastingHelper.BEToLE_UInt16(g.Region);
                if (sub > 0)

                {
                }

                return g;
            }
        }
    }
}