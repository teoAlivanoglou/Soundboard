// namespace Soundboard.Resources;

using System.Collections;
using System.Globalization;
using System.IO;

namespace Soundboard;

public static class ResourceManager
{
    public static void UnpackResources(string folderName = "")
    {
        return;
        var filePath = Path.Combine(folderName, "warning.mp3");
        // if (!File.Exists(filePath))
        {
            UnmanagedMemoryStream? bytes = Resources.Assets.ResourceManager.GetStream("Warning");
            // bytes.
            // if (!string.IsNullOrWhiteSpace(folderName) && !Directory.Exists(folderName))
            //     Directory.CreateDirectory(folderName);
            // _ = File.WriteAllBytesAsync(filePath, (byte[])bytes!);
        }

    }
}