using SharpCompress.Archives.Zip;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WebComicReader.Archives
{
    public class CBZ : Common, IArchive
    {
        public string[] Extensions => new string[] { "cbz", "zip" };

        public async Task<(string[] Names, Stream[] Streams)> Open(Stream Comic, Action<int> OnProgress)
        {
            var Archive = ZipArchive.Open(Comic);
            return await Open(Archive, OnProgress);
        }
    }
}
