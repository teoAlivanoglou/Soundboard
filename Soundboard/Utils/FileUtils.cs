using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Soundboard.Utils;

public static class FileUtils
{
    [DllImport("urlmon.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false)]
    static extern int FindMimeFromData(
        IntPtr pBC,
        [MarshalAs(UnmanagedType.LPWStr)] string pwzUrl,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.I1, SizeParamIndex=3)]
        byte[] pBuffer,
        int cbSize,
        [MarshalAs(UnmanagedType.LPWStr)] string pwzMimeProposed,
        int dwMimeFlags,
        out IntPtr ppwzMimeOut,
        int dwReserved);

    /// <summary>
    /// Ensures that file exists and retrieves the content type 
    /// </summary>
    /// <param name="file"></param>
    /// <returns>Returns for instance "images/jpeg" </returns>
    public static string GetMimeFromFile(string file)
    {
        IntPtr mimeout;
        if (!System.IO.File.Exists(file))
            throw new FileNotFoundException(file + " not found");

        var maxContent = (int)new FileInfo(file).Length;
        if (maxContent > 4096) maxContent = 4096;
        using var fs = File.OpenRead(file);


        var buf = new byte[maxContent];
        var read = fs.Read(buf, 0, maxContent);
        fs.Close();
        var result = FindMimeFromData(IntPtr.Zero, file, buf, read, null, 0, out mimeout, 0);

        if (result != 0)
            throw Marshal.GetExceptionForHR(result)!;
        var mime = Marshal.PtrToStringUni(mimeout);
        Marshal.FreeCoTaskMem(mimeout);
        return mime;
    }
}