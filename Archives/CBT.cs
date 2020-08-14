using SharpCompress.Archives.Tar;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WebComicReader.Archives
{
    public class CBT : Common, IArchive
    {
        public string[] Extensions => new string[] { "cbt", "tar" };

        public async Task<(string[] Names, Stream[] Streams)> Open(Stream Comic, Action<int> OnProgress)
        {
            var Archive = TarArchive.Open(Comic);
            return await Open(Archive, OnProgress);
        }
    }
}
