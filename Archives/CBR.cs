using SharpCompress.Archives.Rar;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WebComicReader.Archives
{
    public class CBR : Common, IArchive
    {
        public string[] Extensions => new string[] { "cbr", "rar" };

        public async Task<(string[] Names, Stream[] Streams)> Open(Stream Comic, Action<int> OnProgress)
        {
            var Archive = RarArchive.Open(Comic);
            return await Open(Archive, OnProgress);
        }
    }
}
