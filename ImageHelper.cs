using System;
using System.Drawing;
using System.IO;

namespace WebComicReader
{
    public static class ImageHelper
    {
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
    }
}
