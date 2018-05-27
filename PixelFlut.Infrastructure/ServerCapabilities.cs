using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace PixelFlut.Infrastructure
{
    [Flags]
    public enum ServerCapabilities
    {
        None = 0,
        Offset = 1 << 0, // OFFSET x y\n
        GreyScale = 1 << 1 // PX x y gg\n
    }
}