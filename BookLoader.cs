using Microsoft.JSInterop;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using System;
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
        public static IJSRuntime Runtime;
        public static IFileInfo FileInfo;
        public static string[] Pages;
        public static bool[] IsPortrait;

        public static async Task Open(Stream Comic, Action<int> OnProgress)
        {
            int Count;
            string[] Names;
            Stream[] Streams;
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

            Count = Result.Item1;
            Names = Result.Item2;
            Streams = Result.Item3;

            Array.Sort(Names, Streams);

            Pages = new string[Streams.Length];
            IsPortrait = new bool[Streams.Length];
            for (int i = 0; i < Streams.Length; i++)
            {
                OnProgress?.Invoke(Percentage(i + Count, Count * 2));
                await DoEvents();

                try
                {
                    var Size = GetImageSize(Streams[i]);
                    Pages[i] = await ToBlob(Streams[i]);
                    IsPortrait[i] = Size.Height > Size.Width;
                    Streams[i].Close();
                }
                catch (Exception ex)
                {
                    Streams[i].Position = 0;
                    byte[] Buff = new byte[8];
                    Streams[i].Read(Buff, 0, Buff.Length);

                    string Signature = string.Empty;

                    foreach (var Byte in Buff)
                        Signature += $"{Byte:X2} ";

                    Signature += " ";

                    foreach (var Byte in Buff)
                        if (Byte >= 0x20 && Byte <= 0x7E)
                            Signature += (char)Byte;

                    throw new Exception($"Failed to Load the Image {Names} ({Signature})\n" + ex.ToString());
                }
            }


            OnProgress?.Invoke(100);
            await DoEvents();
        }

        public static async Task<(int, string[], Stream[])> OpenArchive(IArchive Archive, Action<int> OnProgress)
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

            return (Count, Names, Streams);
        }
        public static int Percentage(int Current, int Total) => (int)Math.Round((double)(100 * Current) / Total);

        public static async Task DoEvents() => await Task.Delay(10);
        public static Size GetImageSize(Stream Buffer)
        {
            var OriPos = Buffer.Position;
            var Ext = GetImageExtension(Buffer);

            byte[] Data = new byte[8];
            Buffer.Read(Data, 0, Data.Length);
            Size ImgSize;
            var Reader = new BinaryReader(Buffer);

            switch (Ext)
            {
                case "bmp":
                    Buffer.Position = 0x12;
                    ImgSize = new Size(Reader.ReadInt32(), Reader.ReadInt32());
                    break;
                case "png":
                    Buffer.Position = 0x10;
                    ImgSize = new Size(Reader.ReadInt32().Reverse(), Reader.ReadInt32().Reverse());
                    break;
                case "gif":
                    Buffer.Position = 0x06;
                    ImgSize = new Size(Reader.ReadInt16(), Reader.ReadInt16());
                    break;
                case "jpg":
                    Buffer.Position = FindJpgSizeOffset(Buffer);
                    var Height = Reader.ReadUInt16().Reverse();
                    var Width = Reader.ReadUInt16().Reverse();
                    ImgSize = new Size(Width, Height);
                    break;
                default:
                    throw new NotImplementedException($"Unk Extension: {Ext}");
            }

            Buffer.Position = OriPos;
            return ImgSize;
        }
        public static int Reverse(this int Value)
        {
            var Data = BitConverter.GetBytes(Value);
            Array.Reverse(Data);
            return BitConverter.ToInt32(Data, 0);
        }
        public static ushort Reverse(this ushort Value)
        {
            var Data = BitConverter.GetBytes(Value);
            Array.Reverse(Data);
            return BitConverter.ToUInt16(Data, 0);
        }
        public static string GetImageExtension(Stream Buffer)
        {
            Buffer.Position = 0;
            var Reader = new BinaryReader(Buffer);
            var Header = Reader.ReadUInt32();

            if (Header == 0x474E5089)
                return "png";
            if ((Header & 0x0000FFFF) == 0x4D42)
                return "bmp";
            if ((Header & 0x00FFFFFF) == 0x464947)
                return "gif";

            Buffer.Position = 0x06;
            Header = Reader.ReadUInt32();
            if (Header == 0x4649464A)
                return "jpg";

            throw new NotImplementedException($"Unsupported or Corrupted Image Format");
        }

        public static int FindJpgSizeOffset(Stream Buffer)
        {
            var Reader = new BinaryReader(Buffer);
            Reader.BaseStream.Position = 2;
            while (true)
            {
                var TagId = Reader.ReadUInt16().Reverse();
                if ((TagId & 0xFFF0) != 0xFFC0)
                {
                    Reader.BaseStream.Position += Reader.ReadUInt16().Reverse();
                    continue;
                }

                return (int)Reader.BaseStream.Position + 3;
            }
        }
        public static async Task<string> ToBlob(Stream Data)
        {
            using MemoryStream Buffer = new MemoryStream();
            Data.CopyTo(Buffer);
            return await ToBlob(GetImageExtension(Data), Buffer.ToArray());
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
