using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class Blob
{
    IJSRuntime Runtime;
    public Blob(IJSRuntime Runtime)
    {
        this.Runtime = Runtime;
    }

    public async Task<string> ToBlobUrl(byte[] Data, string Mime = "application/octet-stream")
    {
        var Addr = Marshal.AllocHGlobal(Data.Length);
        Marshal.Copy(Data, 0, Addr, Data.Length);
        var sCreateBuffer = $"HEAP8.slice({Addr.ToInt64()}, {Addr.ToInt64() + Data.Length})";
        var sCreateBlob = $"new Blob([{sCreateBuffer}], {{type: \"{Mime}\"}})";
        var sBlobToUrl = $"URL.createObjectURL({sCreateBlob})";
        var Result = await Runtime.InvokeAsync<string>("eval", sBlobToUrl);
        Marshal.FreeHGlobal(Addr);
        return Result;
    }
}