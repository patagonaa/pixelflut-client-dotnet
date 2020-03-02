using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;

namespace PixelFlut.Infrastructure
{
    public class FileOutputService : IOutputService
    {
        private readonly FileStream file;

        public FileOutputService(string file)
        {
            this.file = File.Open(file, FileMode.Append, FileAccess.Write, FileShare.Read);
        }

        public int Output(ArraySegment<byte> rendered)
        {
            try
            {
                file.Write(rendered.Array, rendered.Offset, rendered.Count);
                return rendered.Count;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Connection fucked: {ex.SocketErrorCode}");
                return 0;
            }
        }

        public Size GetSize()
        {
            return new Size(1920, 1080);
        }
    }
}