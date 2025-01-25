using System.IO;

namespace Soundboard.Utils;

public class FileTreeStructure
{
    public FileTreeStructure? Parent { get; set; } = null;
    public string Text { get; set; }
    public List<FileInfo> Files { get; set; }
    public List<FileTreeStructure> Directories { get; set; }

    public int Depth { get; set; }
    public string DirectoryPath { get; set; }
    public string RootPath { get; set; }


    public FileTreeStructure(string path) : this(path, null, 0)
    {
    }

    private FileTreeStructure(string path, FileTreeStructure? parent, int depth)
    {
        Parent = parent;
        Depth = depth;
        Files = new List<FileInfo>();
        Directories = new List<FileTreeStructure>();
        DirectoryPath = path;

        RootPath = Parent?.RootPath ?? path;

        Text = Path.GetFileName(path);
        
        if (string.IsNullOrWhiteSpace(path)) return;
        
        foreach (var file in Directory.EnumerateFiles(path))
        {
            Files.Add(new FileInfo(file));
        }

        foreach (var directory in Directory.EnumerateDirectories(path))
        {
            Directories.Add(new FileTreeStructure(directory, this, Depth + 1));
        }
    }
}