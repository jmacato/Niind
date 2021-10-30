using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Niind.Helpers;
using Niind.Structures.FileSystem;
using Niind.Structures.TitlesSystem;

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

                Console.WriteLine($"Downloading Title Ticket for {titleID}v{titles.Version}");

                var downloadCetkUri = new Uri(Constants.NUSBaseUrl + titleID + "/cetk");
                var rawTicket = client.DownloadData(downloadCetkUri).CastToStruct<RawTicket>();

                var issuer = Encoding.ASCII.GetString(rawTicket.Issuer).Trim(char.MinValue);

                if (rawTicket.CommonKeyIndex[0] != 0)
                {
                    Console.WriteLine(
                        $"Expected Common Key Index to be 0 for {titleID}v{titles.Version} but got {rawTicket.CommonKeyIndex[0]}. Skipping.");
                    continue;
                }

                Console.WriteLine("Downloading Title Metadata. ");

                var downloadTmdUri = new Uri(Constants.NUSBaseUrl + titleID + "/" + tmdVersion);
                var decodedTmd = TitleMetadata.FromByteArray(client.DownloadData(downloadTmdUri));

                var keyIV = rawTicket.TitleID_KeyIV;
                EncryptionHelper.PadByteArrayToMultipleOf(ref keyIV, 0x10);

                var decryptedTitleKey = EncryptionHelper.AESDecrypt(rawTicket.TitleKeyEnc, keyFile.CommonKey,
                    rawTicket.TitleKeyEnc.Length, keyIV);

                Console.WriteLine(
                    "Title Key Encrypted: " + EncryptionHelper.ByteArrayToHexString(rawTicket.TitleKeyEnc));
                Console.WriteLine(
                    "Title Key Common Key: " + EncryptionHelper.ByteArrayToHexString(keyFile.CommonKey));
                Console.WriteLine(
                    "Title Key IV: " + EncryptionHelper.ByteArrayToHexString(keyIV));
                Console.WriteLine(
                    "Title Key Decrypted: " + EncryptionHelper.ByteArrayToHexString(decryptedTitleKey));

                foreach (var contentDescriptor in decodedTmd.ContentDescriptors)
                {
                    Console.WriteLine(
                        $"Decrypting Title Content {contentDescriptor.ContentID:X8} Index {contentDescriptor.Index}");

                    var sapd = new Uri(Constants.NUSBaseUrl + titleID + "/" + $"{contentDescriptor.ContentID:X8}");
                    var encryptedContent = client.DownloadData(sapd);

                    EncryptionHelper.PadByteArrayToMultipleOf(ref encryptedContent, 0x40);

                    var contentIV = CastingHelper.LEToBE_UInt16(contentDescriptor.Index);

                    EncryptionHelper.PadByteArrayToMultipleOf(ref contentIV, 0x10);

                    var decryptedContent = EncryptionHelper.AESDecrypt(
                        encryptedContent,
                        decryptedTitleKey,
                        (int)contentDescriptor.Size,
                        contentIV);


                    var decryptedHash = EncryptionHelper.GetSHA1(decryptedContent);
                    
                    Console.WriteLine($"Received Data Length from NUS: {encryptedContent.Length}");

                    if (contentDescriptor.SHA1.SequenceEqual(decryptedHash))
                    {
                        Console.WriteLine("Hash Matched with Title Metadata: " +
                                          EncryptionHelper.ByteArrayToHexString(contentDescriptor.SHA1));
                        Console.WriteLine($"Decrypted Length: {decryptedContent.Length}");
                        Console.WriteLine($"Length Delta: {decryptedContent.Length - encryptedContent.Length}");

                        contentDescriptor.DecryptedContent = decryptedContent;
                        contentDescriptor.DecryptionKey = decryptedTitleKey;
                        contentDescriptor.DecryptionIV = keyIV;
                    }
                    else
                    {
                        Console.WriteLine("Hash did not match! Skipping this title..");
                        Console.WriteLine("Expected Hash: " +
                                          EncryptionHelper.ByteArrayToHexString(contentDescriptor.SHA1));
                        Console.WriteLine("Got Hash     : " + EncryptionHelper.ByteArrayToHexString(decryptedHash));
                    }
                }
            }
        }
    }
}