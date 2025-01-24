// namespace Soundboard.Resources;

using System.Collections;
using System.Globalization;
using System.IO;

namespace Soundboard;

public static class ResourceManager
{
    public static void UnpackResources(string folderName = "")
    {
        var filePath = Path.Combine(folderName, "warning.mp3");
        if (!File.Exists(filePath))
        {
            var bytes = Resources.ResourceManager.GetObject("Warning");
            if (!string.IsNullOrWhiteSpace(folderName) && !Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
            _ = File.WriteAllBytesAsync(filePath, (byte[])bytes!);
        }

    }
}