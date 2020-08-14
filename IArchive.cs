using System;
using System.IO;
using System.Threading.Tasks;

namespace WebComicReader
{
    interface IArchive
    {
        public string[] Extensions { get; }
        Task<(string[] Names, Stream[] Streams)> Open(Stream Comic, Action<int> OnProgress);
    }
}
