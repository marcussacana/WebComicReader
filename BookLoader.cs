using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebComicReader.Archives;

namespace WebComicReader
{
    public static class BookLoader
    {
        static IArchive[] Formats = new IArchive[] {
            new CBZ(), new CB7(), new CBR(), new CBT()
        };

        public static Blob BlobHelper;
        public static string[] Pages;
        public static bool[] IsPortrait;
        public static bool EmbeddedFunctions;
        public static async Task OpenPages(string[] Names, Stream[] Streams, Action<int> OnProgress)
        {
            int Count = Names.Length;
            Array.Sort(Names, Streams);

            List<string> PageList = new List<string>();
            List<bool> PortraitList = new List<bool>();
            for (int i = 0; i < Streams.Length; i++)
            {
                OnProgress?.Invoke(Program.Percentage(i + Count, Count * 2));
                await Program.DoEvents();

                var Rst = await OpenPage(Names[i], Streams[i]);

                if (Rst.Blob != null)
                {
                    PageList.Add(Rst.Blob);
                    PortraitList.Add(Rst.Portrait);
                }
            }

            Pages = PageList.ToArray();
            IsPortrait = PortraitList.ToArray();

            OnProgress?.Invoke(100);
            await Program.DoEvents();
        }

        public static async Task OpenArchive(string Name, Stream Archive, Action<int> OnProgress)
        {

            var Ext = Name.ToLowerInvariant().Split('.').Last().Trim();
            var PresumedFormats = (from x in Formats where x.Extensions.Contains(Ext) select x);
            var UnexpectedFormats = (from x in Formats where !x.Extensions.Contains(Ext) select x);

            (string[] Names, Stream[] Streams)? Result = null;

            foreach (var Format in PresumedFormats.Concat(UnexpectedFormats))
            {
                try
                {
                    Result = await Format.Open(Archive, OnProgress);
                    break;
                }
                catch { }
            }

            if (Result == null) { 
                var Signature = GetSignature(Archive);
                Archive?.Close();
                throw new Exception($"Failed to open the Archive \"{Name}\" ({Signature})");
            };

            await OpenPages(Result?.Names, Result?.Streams, OnProgress);
            Archive?.Close();
        }

        static async Task<(string Blob, bool Portrait)> OpenPage(string Name, Stream Page)
        {
            try
            {
                if (!(Page is MemoryStream))
                {
                    var MemStream = await Page.ToMemoryStream();
                    Page.Close();
                    Page = MemStream;
                }
                var Size = ImageHelper.GetImageSize(Page);
                var Rst = (await ToBlob(Page), Size.Height > Size.Width);
                Page.Close();
                return Rst;
            }
            catch (Exception ex)
            {
                var Signature = GetSignature(Page);

                await Program.Warning($"Failed to Load the Image {Name} ({Signature})\n" + ex.ToString());
                return (null, false);
            }
        }

        public static async Task<string> ToBlob(Stream Data)
        {
            var Buffer = await Data.ToMemoryStream();

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

        public static string GetSignature(Stream Input)
        {
            string Signature = string.Empty;
            try
            {
                Input.Position = 0;
                byte[] Buff = new byte[8];
                Input.Read(Buff, 0, Buff.Length);

                foreach (var Byte in Buff)
                    Signature += $"{Byte:X2} ";

                Signature += " ";

                foreach (var Byte in Buff)
                    if (Byte >= 0x20 && Byte <= 0x7E)
                        Signature += (char)Byte;
            }
            catch { }
            return Signature;
        }
    }
}
