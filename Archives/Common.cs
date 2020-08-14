using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebComicReader.Archives
{
    public class Common
    {
        public async Task<(string[] Names, Stream[] Streams)> Open(SharpCompress.Archives.IArchive Archive, Action<int> OnProgress)
        {
            var Files = Archive.Entries.Where(entry => !entry.IsDirectory);
            int Count = Files.Count();

            string[] Names = new string[Count];
            Stream[] Streams = new Stream[Count];

            int Current = 0;

            foreach (var Entry in Files)
            {
                OnProgress?.Invoke(Program.Percentage(Current, Count * 2));
                await Program.DoEvents();

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
    }
}
