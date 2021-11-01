using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Niind.Helpers;
using Niind.Structures.FileSystem;
using Niind.Structures.TitlesSystem;

namespace Niind
{
    public class NintendoUpdateServerDownloader
    {
        public ConcurrentBag<SharedContent> SharedContents = new();
        public ConcurrentBag<TitleMetadataContent> DecryptedTitles = new();
        private static object lockObj = new();
        private string CachePathName = Path.Combine(Path.GetTempPath(), "niind_nus_cache");

        static readonly HttpClient client = new();

        public async Task GetUpdateAsync(KeyFile keyFile)
        {
            if (!Directory.Exists(CachePathName))
            {
                Directory.CreateDirectory(CachePathName);
            }

            Console.WriteLine("Starting Titles Download from Nintendo Update Servers...");
            
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", Constants.UpdaterUserAgent);

            await Constants.Version4_3U_Titles.ParallelForEachAsync(async titles =>
            {
                var logOutput = "";
                var titleID = $"{titles.TicketID:X16}";
                var tmdVersion = $"tmd.{titles.Version}";

                var tikFile = $"{CachePathName}/{EncryptionHelper.GetSHA1String($"{titleID}")}.tik";

                RawTicket rawTicket;
                if (File.Exists(tikFile))
                {
                    logOutput += $"Getting Title Ticket for {titleID}v{titles.Version} from cache folder.\n";
                    rawTicket = (await File.ReadAllBytesAsync(tikFile)).CastToStruct<RawTicket>();
                }
                else
                {
                    logOutput += $"Downloading Title Ticket for {titleID}v{titles.Version}\n";
                    var downloadCetkUri = new Uri(Constants.NUSBaseUrl + titleID + "/cetk");
                    var downloadedTicket = await client.GetByteArrayAsync(downloadCetkUri);
                    rawTicket = downloadedTicket.CastToStruct<RawTicket>();
                    await File.WriteAllBytesAsync(tikFile, downloadedTicket);
                    logOutput += $"Saved Title Ticket to cache folder.\n";
                }

                var issuer = Encoding.ASCII.GetString(rawTicket.Issuer).Trim(char.MinValue);

                if (rawTicket.CommonKeyIndex[0] != 0)
                {
                    logOutput +=
                        $"Expected Common Key Index to be 0 for {titleID}v{titles.Version} but got {rawTicket.CommonKeyIndex[0]}. Skipping." +
                        "\n";
                    return;
                }

                var tmdFile = $"{CachePathName}/{EncryptionHelper.GetSHA1String($"{titleID}")}.tmd";
                TitleMetadata decodedTmd;

                if (File.Exists(tmdFile))
                {
                    logOutput += $"Getting Title Metadata for {titleID}v{titles.Version} from cache folder.\n";
                    decodedTmd = TitleMetadata.FromByteArray(await File.ReadAllBytesAsync(tmdFile));
                }
                else
                {
                    logOutput += $"Downloading Title Metadata for {titleID}v{titles.Version}. \n";

                    var downloadTmdUri = new Uri(Constants.NUSBaseUrl + titleID + "/" + tmdVersion);
                    var downloadedTmd = await client.GetByteArrayAsync(downloadTmdUri);
                    decodedTmd = TitleMetadata.FromByteArray(downloadedTmd);
                    await File.WriteAllBytesAsync(tmdFile, downloadedTmd);
                    logOutput += "Saved Title Metadata to cache folder. \n";
                }

                var keyIV = rawTicket.TitleID;
                EncryptionHelper.PadByteArrayToMultipleOf(ref keyIV, 0x10);

                var decryptedTitleKey = EncryptionHelper.AESDecrypt(rawTicket.TitleKeyEnc, keyFile.CommonKey,
                    rawTicket.TitleKeyEnc.Length, keyIV);

                logOutput +=
                    "Title Key Encrypted : " + EncryptionHelper.ByteArrayToHexString(rawTicket.TitleKeyEnc) + "\n";
                logOutput +=
                    "Title Key Common Key: " + EncryptionHelper.ByteArrayToHexString(keyFile.CommonKey) + "\n";
                logOutput +=
                    "Title Key IV        : " + EncryptionHelper.ByteArrayToHexString(keyIV) + "\n";
                logOutput +=
                    "Title Key Decrypted : " + EncryptionHelper.ByteArrayToHexString(decryptedTitleKey) + "\n";

                await decodedTmd.ContentDescriptors.ParallelForEachAsync(async contentDescriptor =>
                {
                    lock (lockObj)
                    {
                        if (SharedContents.Any(x => x.SHA1.SequenceEqual(contentDescriptor.SHA1)))
                        {
                            logOutput +=
                                $"Skipping Content {contentDescriptor.ContentID:X8} Index {contentDescriptor.Index} since it's on the shared map already.\n";

                            return;
                        }
                    }

                    var contentFileName =
                        $"{CachePathName}/{EncryptionHelper.ByteArrayToHexString(contentDescriptor.SHA1)}.app";

                    byte[] decryptedHash;
                    byte[] decryptedContent;

                    if (File.Exists(contentFileName))
                    {
                        logOutput +=
                            $"Getting Decrypted Content {contentDescriptor.ContentID:X8} Index {contentDescriptor.Index} from cache folder.\n";
                        decryptedContent = await File.ReadAllBytesAsync(contentFileName);
                        decryptedHash = EncryptionHelper.GetSHA1(decryptedContent);
                    }
                    else
                    {
                        logOutput +=
                            $"Downloading Title Content {contentDescriptor.ContentID:X8} Index {contentDescriptor.Index} from NUS.\n";

                        var contentUri =
                            new Uri(Constants.NUSBaseUrl + titleID + "/" + $"{contentDescriptor.ContentID:X8}");
                        var encryptedContent = await client.GetByteArrayAsync(contentUri);

                        EncryptionHelper.PadByteArrayToMultipleOf(ref encryptedContent, 0x40);

                        var contentIV = CastingHelper.Swap_BA(contentDescriptor.Index);

                        EncryptionHelper.PadByteArrayToMultipleOf(ref contentIV, 0x10);

                        decryptedContent = EncryptionHelper.AESDecrypt(
                            encryptedContent,
                            decryptedTitleKey,
                            (int)contentDescriptor.Size,
                            contentIV);

                        logOutput +=
                            $"Title Content {contentDescriptor.ContentID:X8} Index {contentDescriptor.Index} Decrypted.\n";

                        decryptedHash = EncryptionHelper.GetSHA1(decryptedContent);
                        logOutput += $"Received Data Length from NUS: {encryptedContent.Length}\n";
                        logOutput += $"Decrypted Length: {decryptedContent.Length}\n";
                        logOutput += $"Length Delta: {decryptedContent.Length - encryptedContent.Length}\n";

                        await File.WriteAllBytesAsync(contentFileName, decryptedContent);

                        logOutput +=
                            $"Saved Decrypted Content {contentDescriptor.ContentID:X8} Index {contentDescriptor.Index} to cache folder.\n";
                    }


                    if (contentDescriptor.SHA1.SequenceEqual(decryptedHash))
                    {
                        logOutput += "Hash Matched with Title Metadata: " +
                                     EncryptionHelper.ByteArrayToHexString(contentDescriptor.SHA1) + "\n";

                        contentDescriptor.ParentTMD = decodedTmd;
                        contentDescriptor.Ticket = rawTicket;
                        contentDescriptor.DecryptedContent = decryptedContent;
                        contentDescriptor.DecryptionKey = decryptedTitleKey;
                        contentDescriptor.DecryptionIV = keyIV;

                        if (contentDescriptor.Type == 0x8001)
                        {
                            logOutput +=
                                $"Added decrypted content {EncryptionHelper.ByteArrayToHexString(contentDescriptor.SHA1)} in shared content list.\n";

                            SharedContents.Add(new SharedContent( decryptedHash, decryptedContent));
                        }
                        else
                        {
                            logOutput +=
                                $"Added decrypted content as {decodedTmd.Header.TitleID:X16}/{contentDescriptor.ContentID:X8} on the installed titles list.\n";
                            DecryptedTitles.Add(contentDescriptor);
                        }
                    }
                    else
                    {
                        logOutput += "Hash did not match! Skipping this title..\n";
                        logOutput += "Expected Hash: " +
                                     EncryptionHelper.ByteArrayToHexString(contentDescriptor.SHA1) + "\n";
                        logOutput += "Got Hash     : " + EncryptionHelper.ByteArrayToHexString(decryptedHash) + "\n";
                    }
                });

                Console.WriteLine(logOutput);
            });
            
            
            // this is so inefficient...
            SharedContents = new ConcurrentBag<SharedContent>(SharedContents
                .GroupBy(x => EncryptionHelper.ByteArrayToHexString(x.SHA1))
                .Select(g => g.First()).ToList());
            
            
            Console.WriteLine("Done Downloading from Nintendo Update Servers...");
        }
    }
}