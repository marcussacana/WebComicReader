using SharpCompress.Archives.SevenZip;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WebComicReader.Archives
{
    public class CB7 : Common, IArchive
    {
        public string[] Extensions => new string[] { "cb7", "7z" };

        public async Task<(string[] Names, Stream[] Streams)> Open(Stream Comic, Action<int> OnProgress)
        {
            var Archive = SevenZipArchive.Open(Comic);
            return await Open(Archive, OnProgress);
        }
    }
}
