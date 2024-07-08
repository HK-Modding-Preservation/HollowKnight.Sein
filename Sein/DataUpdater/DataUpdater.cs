using System.Collections.Generic;
using System.IO;

namespace Sein.DataUpdater;

public class DataUpdater
{
    public static void Run()
    {
        string modPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Hollow Knight\\hollow_knight_Data\\Managed\\Mods\\Custom Knight\\Skins\\Ori";
        DirectoryInfo modDir = new(modPath);
        if (modDir.Exists) modDir.Delete(true);
        modDir.Create();

        var root = InferGitRoot(Directory.GetCurrentDirectory());
        var sourcePath = Path.Combine(root, "Ori");

        FixSpriteSheets(sourcePath);
        CopyAllFiles(sourcePath, modPath);
    }

    private static string InferGitRoot(string path)
    {
        var info = Directory.GetParent(path);
        while (info != null)
        {
            if (Directory.Exists(Path.Combine(info.FullName, ".git"))) return info.FullName;
            info = Directory.GetParent(info.FullName);
        }

        return path;
    }

    private static void FixSpriteSheets(string path)
    {
        Dictionary<string, string> remaps = new()
        {
            {"Dream Nail Get Cln", "" },
            {"Knight Dream Cutscene Cln", "DreamArrival" },
            {"Knight Dream Gate Cln", "Sprint" },
            {"Knight Slug Cln", "" },
            {"Spell Effects 2", "Wraiths" },
            {"Spell Effects Neutral", "VoidSpells" },
            {"Spell Effects", "VS" },
            {"White Wave Lone", "" },
        };

        foreach (var e in remaps)
        {
            var src = e.Key;
            var dst = e.Value;

            var srcPath = Path.Combine(path, $"{src}.png");
            var dstPath = Path.Combine(path, $"{dst}.png");
            if (File.Exists(srcPath))
            {
                if (dst.Length == 0) File.Delete(srcPath);
                else
                {
                    File.Delete(dstPath);
                    File.Move(srcPath, dstPath);
                }
            }
        }
    }

    private static void CopyAllFiles(string sourcePath, string targetPath)
    {
        // Create all directories.
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

        // Copy all files.
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
    }
}
