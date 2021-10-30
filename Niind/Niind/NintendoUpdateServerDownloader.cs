using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Niind.Structures;

namespace Niind
{
    public class NintendoUpdateServerDownloader
    {
        public void GetUpdate(KeyFile keyFile)
        {
            using var client = new WebClient();

            client.Headers["User-Agent"] = Constants.UpdaterUserAgent;

            foreach (var titles in Constants.Version4_3U_Titles)
            {
                var titleID = $"{titles.TicketID:X16}";
                var tmdVersion = $"tmd.{titles.Version}";
                
                Console.WriteLine($"Downloading Title Ticket for {titleID}v{tmdVersion}");
                
                var downloadCetkUri = new Uri(Constants.NUSBaseUrl + titleID + "/cetk");
                var rawTicket = client.DownloadData(downloadCetkUri).CastToStruct<RawTicket>();

                var issuer = Encoding.ASCII.GetString(rawTicket.Issuer).Trim(char.MinValue);

                if (rawTicket.CommonKeyIndex[0] != 0)
                {
                    Console.WriteLine(
                        $"Expected Common Key Index to be 0 for {titleID}v{tmdVersion} but got {rawTicket.CommonKeyIndex[0]}. Skipping.");
                    continue;
                }


                Console.WriteLine("Downloading Title Metadata for ");


                var downloadTmdUri = new Uri(Constants.NUSBaseUrl + titleID + "/" + tmdVersion);
                var decodedTmd = TitleMetadata.FromByteArray(client.DownloadData(downloadTmdUri));
                var keyIV = (rawTicket.TitleID_KeyIV);

                EncryptionHelper.PadByteArrayToMultipleOf(ref keyIV, 0x10);
                var shaEngine = SHA1.Create();
                byte[] decryptedTitleKey;

                using (var aes = new RijndaelManaged
                       {
                           Padding = PaddingMode.None,
                           Mode = CipherMode.CBC
                       })
                {
                    var decryptor = aes.CreateDecryptor(keyFile.CommonKey, keyIV);
                    var encryptedTitleKey = rawTicket.TitleKeyEnc;

                    using var memoryStream = new MemoryStream(encryptedTitleKey);
                    using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

                    decryptedTitleKey = new byte[encryptedTitleKey.Length];
                    _ = cryptoStream.Read(decryptedTitleKey, 0x0, decryptedTitleKey.Length);


                    Console.WriteLine(
                        "Title Key Encrypted: " + EncryptionHelper.ByteArrayToHexString(encryptedTitleKey));
                    Console.WriteLine(
                        "Title Key Common Key: " + EncryptionHelper.ByteArrayToHexString(keyFile.CommonKey));
                    Console.WriteLine(
                        "Title Key IV: " + EncryptionHelper.ByteArrayToHexString(keyIV));
                    Console.WriteLine(
                        "Title Key Decrypted: " + EncryptionHelper.ByteArrayToHexString(decryptedTitleKey));
                }

                foreach (var contentDescriptor in decodedTmd.ContentDescriptors)
                {
                    Console.WriteLine($"Decrypting Title Content Index: {contentDescriptor.Index}");
                    
                    var sapd = new Uri(Constants.NUSBaseUrl + titleID + "/" + $"{contentDescriptor.ContentID:X8}");
                    var encryptedContent = client.DownloadData(sapd);
                   
                    Console.WriteLine($"Received Data Length from NUS: {encryptedContent.Length}");

                    EncryptionHelper.PadByteArrayToMultipleOf(ref encryptedContent, 0x40);
                    byte[] decryptedContent;
                    using (var aes = new RijndaelManaged
                           {
                               Padding = PaddingMode.None,
                               Mode = CipherMode.CBC,
                           })
                    {
                        var contentIV = CastingHelper.LEToBE_UInt16(contentDescriptor.Index);
                        EncryptionHelper.PadByteArrayToMultipleOf(ref contentIV, 0x10);

                        var decryptor = aes.CreateDecryptor(decryptedTitleKey, contentIV);

                        using var memoryStream = new MemoryStream(encryptedContent);
                        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

                        decryptedContent = new byte[contentDescriptor.Size];
                        _ = cryptoStream.Read(decryptedContent, 0x0, decryptedContent.Length);
                    }

                    shaEngine.Initialize();
                    shaEngine.ComputeHash(decryptedContent);

                    Console.WriteLine($"Received Data Length from NUS: {encryptedContent.Length}");

                    if (contentDescriptor.SHA1.SequenceEqual(shaEngine.Hash))
                    {
                        Console.WriteLine("Hash Matched with Title Metadata: " + EncryptionHelper.ByteArrayToHexString(contentDescriptor.SHA1));
                        Console.WriteLine($"Decrypted Length: {decryptedContent.Length}");
                        Console.WriteLine($"Length Delta: {decryptedContent.Length - encryptedContent.Length}");

                        contentDescriptor.DecryptedContent = decryptedContent;
                        contentDescriptor.DecryptionKey = decryptedTitleKey;
                        contentDescriptor.DecryptionIV = keyIV;
                    }
                    else
                    {
                        Console.WriteLine("Hash did not match! Skipping this title..");
                        Console.WriteLine("Expected Hash: " + EncryptionHelper.ByteArrayToHexString(contentDescriptor.SHA1));
                        Console.WriteLine("Got Hash     : " + EncryptionHelper.ByteArrayToHexString(shaEngine.Hash));
                    }
                }
            }
        }

        public enum SignatureType : uint
        {
            RSA_2048 = 0x00010001u,
            RSA_4096 = 0x00010000u
        };

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

        public struct RawTitleMetadataContentDescriptor : IMaterialize<TitleMetadataContent>
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

            public TitleMetadataContent ToManagedObject()
            {
                return new TitleMetadataContent
                {
                    ContentID = CastingHelper.BEToLE_UInt32(ContentID),
                    Index = CastingHelper.BEToLE_UInt16(Index),
                    Type = CastingHelper.BEToLE_UInt16(Type),
                    Size = CastingHelper.BEToLE_UInt64(Size),
                    SHA1 = SHA1
                };
            }
        }

        public struct TitleMetadata
        {
            public TitleMetadataHeader Header { get; }
            public IList<TitleMetadataContent> ContentDescriptors { get; }

            private TitleMetadata(TitleMetadataHeader header,
                IList<TitleMetadataContent> contentDescriptors)
            {
                Header = header;
                ContentDescriptors = contentDescriptors;
            }

            public static TitleMetadata FromByteArray(byte[] tmdBytes)
            {
                var tmdHeaderSize = Marshal.SizeOf<RawTitleMetadataHeader>();

                var rawHeader = tmdBytes.CastToStruct<RawTitleMetadataHeader>();

                var header = rawHeader.ToManagedObject();

                var contentSpan = tmdBytes[tmdHeaderSize..];

                var nbr = header.NumberOfContents;

                var sz = Marshal.SizeOf<RawTitleMetadataContentDescriptor>();

                var contentDescriptors = new List<TitleMetadataContent>();
                for (var i = 0; i < nbr * sz; i += sz)
                {
                    var contentDescBytes = contentSpan[i..(i + sz)];
                    var contentDesc = contentDescBytes.CastToStruct<RawTitleMetadataContentDescriptor>()
                        .ToManagedObject();

                    contentDescriptors.Add(contentDesc);
                }

                return new TitleMetadata(header, contentDescriptors);
            }
        }


        public class TitleMetadataContent
        {
            public uint ContentID;
            public ushort Index;
            public ushort Type;
            public ulong Size;
            public byte[] SHA1;
            public byte[] DecryptedContent;
            public byte[] DecryptionKey;
            public byte[] DecryptionIV;
        }

        public struct TitleMetadataHeader
        {
            public SignatureType SignatureType;
            public byte[] Signature;
            public byte[] Issuer;
            public byte Version;
            public byte ca_crl_version;
            public byte signer_crl_version;
            public bool IsVWii;
            public ulong SystemVersion;
            public ulong TitleID;
            public uint TitleType;
            public ushort GroupID;
            public ushort Region;
            public byte[] Ratings;
            public byte[] IPCMask;
            public uint AccessRights;
            public ushort TitleVersion;
            public ushort NumberOfContents;
            public ushort BootIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawTitleMetadataHeader : IMaterialize<TitleMetadataHeader>
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] SignatureType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] Signature;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            public byte[] Padding0;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] Issuer;

            public byte Version;

            public byte ca_crl_version;

            public byte signer_crl_version;

            public byte IsVWii;

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

            public TitleMetadataHeader ToManagedObject()
            {
                return new TitleMetadataHeader
                {
                    SignatureType = (SignatureType)CastingHelper.BEToLE_UInt32(SignatureType),
                    Signature = Signature,
                    Issuer = Issuer,
                    Version = Version,
                    ca_crl_version = ca_crl_version,
                    signer_crl_version = signer_crl_version,
                    IsVWii = IsVWii == 1,
                    SystemVersion = CastingHelper.BEToLE_UInt64(SystemVersion),
                    TitleID = CastingHelper.BEToLE_UInt64(TitleID),
                    TitleType = CastingHelper.BEToLE_UInt32(TitleType),
                    GroupID = CastingHelper.BEToLE_UInt16(GroupID),
                    Region = CastingHelper.BEToLE_UInt16(Region),
                    Ratings = Ratings,
                    IPCMask = IPCMask,
                    AccessRights = CastingHelper.BEToLE_UInt32(AccessRights),
                    TitleVersion = CastingHelper.BEToLE_UInt16(TitleVersion),
                    NumberOfContents = CastingHelper.BEToLE_UInt16(NumberOfContents),
                    BootIndex = CastingHelper.BEToLE_UInt16(BootIndex)
                };
            }
        }
    }

    public interface IMaterialize<out T>
    {
        T ToManagedObject();
    }
}