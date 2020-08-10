using Microsoft.JSInterop;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tewr.Blazor.FileReader;

namespace WebComicReader
{
    public static class BookLoader
    {
        public static Blob BlobHelper;
        public static IFileInfo FileInfo;
        public static string[] Pages;
        public static bool[] IsPortrait;

        public static async Task OpenPages(string[] Names, Stream[] Streams, Action<int> OnProgress)
        {
            int Count = Names.Length;
            Array.Sort(Names, Streams);

            List<string> PageList = new List<string>();
            List<bool> PortraitList = new List<bool>();
            for (int i = 0; i < Streams.Length; i++)
            {
                OnProgress?.Invoke(Percentage(i + Count, Count * 2));
                await DoEvents();

                var Rst = await OpenPage(Names[i], Streams[i]);
                PageList.Add(Rst.Item1);
                PortraitList.Add(Rst.Item2);
            }

            Pages = PageList.ToArray();
            IsPortrait = PortraitList.ToArray();

            OnProgress?.Invoke(100);
            await DoEvents();
        }

        public static async Task OpenArchive(Stream Comic, Action<int> OnProgress)
        {
            IArchive Archive;

            switch (FileInfo.Name.ToLowerInvariant().Split('.').Last().Trim())
            {
                case "zip":
                case "cbz":
                    Archive = ZipArchive.Open(Comic);
                    break;
                case "rar":
                case "cbr":
                    Archive = RarArchive.Open(Comic);
                    break;
                case "7z":
                case "cb7":
                    Archive = SevenZipArchive.Open(Comic);
                    break;
                case "tar":
                case "cbt":
                    Archive = TarArchive.Open(Comic);
                    break;
                default:
                    throw new Exception("Unsupported Comic Format");
            }

            var Result = await OpenArchive(Archive, OnProgress);
            await OpenPages(Result.Item1, Result.Item2, OnProgress);
        }

        static async Task<(string, bool)> OpenPage(string Name, Stream Page)
        {
            try
            {
                if (!(Page is MemoryStream))
                {
                    var Stream = new MemoryStream();
                    try {
                        await Page.CopyToAsync(Stream);
                    }
                    catch {
                        Page.CopyTo(Stream);
                    }
                    Page.Close();
                    Page = Stream;
                }
                var Size = ImageHelper.GetImageSize(Page);
                var Rst = (await ToBlob(Page), Size.Height > Size.Width);
                Page.Close();
                return Rst;
            }
            catch (Exception ex)
            {
                string Signature = string.Empty;
                try
                {
                    Page.Position = 0;
                    byte[] Buff = new byte[8];
                    Page.Read(Buff, 0, Buff.Length);

                    foreach (var Byte in Buff)
                        Signature += $"{Byte:X2} ";

                    Signature += " ";

                    foreach (var Byte in Buff)
                        if (Byte >= 0x20 && Byte <= 0x7E)
                            Signature += (char)Byte;
                }
                catch { }

                throw new Exception($"Failed to Load the Image {Name} ({Signature})\n" + ex.ToString());
            }
        }

        static async Task<(string[], Stream[])> OpenArchive(IArchive Archive, Action<int> OnProgress)
        {
            var Files = Archive.Entries.Where(entry => !entry.IsDirectory);
            int Count = Files.Count();

            string[] Names = new string[Count];
            Stream[] Streams = new Stream[Count];

            int Current = 0;

            foreach (var Entry in Files)
            {
                OnProgress?.Invoke(Percentage(Current, Count * 2));
                await DoEvents();

                using Stream Input = Entry.OpenEntryStream();
                var Buffer = new MemoryStream();
                Input.CopyTo(Buffer);
                Buffer.Position = 0;

                Names[Current] = Entry.Key;
                Streams[Current++] = Buffer;
            }

            Archive.Dispose();

            return (Names, Streams);
        }
        public static int Percentage(int Current, int Total) => (int)Math.Round((double)(100 * Current) / Total);

        public static async Task DoEvents() => await Task.Delay(10);

        public static async Task<string> ToBlob(Stream Data)
        {
            MemoryStream Buffer;
            if (Data is MemoryStream)
            {
                Buffer = (MemoryStream)Data;
            }
            else
            {
                Buffer = new MemoryStream();
                Data.CopyTo(Buffer);
            }

            var Blob = await ToBlob(ImageHelper.GetImageExtension(Data), Buffer.ToArray());
            Buffer?.Dispose();
            return Blob;
        }

        public static async Task<string> ToBlob(string Ext, byte[] Data)
        {
            var Mime = Ext switch
            {
                "bmp" => "image/bmp",
                "png" => "image/png",
                "gif" => "image/gif",
                "jpg" => "image/jpeg",
                "jpeg" => "image/jpeg",
                _ => throw new NotImplementedException($"Unk Extension: {Ext}"),
            };

            return await BlobHelper.ToBlobUrl(Data, Mime);
        }
    }
}
