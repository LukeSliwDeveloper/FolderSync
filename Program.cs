using System.Collections;
using System.Security.Cryptography;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: FolderSync <sourcePath> <replicaPath> <intervalInSeconds> <logFilePath>");
            return;
        }

        string sourcePath = args[0];
        string replicaPath = args[1];
        int interval = int.Parse(args[2]);
        string logFile = args[3];

        if (!Directory.Exists(sourcePath))
        {
            Console.WriteLine($"Source folder does not exist: {sourcePath}");
            return;
        }
        if (interval <= 0)
        {
            Console.WriteLine("Invalid interval. Must be a positive integer.");
            return;
        }

        while (true)
        {
            try
            {
                Synchronize(sourcePath, replicaPath, logFile);
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}", logFile);
            }
            Thread.Sleep(interval * 1000);
        }
    }

    static void Synchronize(string sourcePath, string replicaPath, string logFile)
    {
        if (!Directory.Exists(replicaPath))
        {
            Directory.CreateDirectory(replicaPath);
            Log($"Created replica directory: {replicaPath}", logFile);
        }

        foreach (var sourceFile in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourcePath, sourceFile);
            string replicaFile = Path.Combine(replicaPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(replicaFile)!);
            if (!File.Exists(replicaFile) || !FilesAreEqual(sourceFile, replicaFile))
            {
                File.Copy(sourceFile, replicaFile, true);
                Log($"Copied/Updated: {relativePath}", logFile);
            }
        }

        foreach (var replicaFile in Directory.GetFiles(replicaPath, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(replicaPath, replicaFile);
            string sourceFile = Path.Combine(sourcePath, relativePath);

            if (!File.Exists(sourceFile))
            {
                File.Delete(replicaFile);
                Log($"Deleted: {relativePath}", logFile);
            }
        }

        foreach (var replicaDir in Directory.GetDirectories(replicaPath, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(replicaPath, replicaDir);
            string sourceDir = Path.Combine(sourcePath, relativePath);

            if (!Directory.Exists(sourceDir))
            {
                Directory.Delete(replicaDir, true);
                Log($"Deleted directory: {relativePath}", logFile);
            }
        }

        Log("Synchronization completed.", logFile);
    }

    static bool FilesAreEqual(string file1, string file2)
    {
        var fi1 = new FileInfo(file1);
        var fi2 = new FileInfo(file2);

        if (fi1.Length != fi2.Length) return false;
        if (fi1.LastWriteTimeUtc != fi2.LastWriteTimeUtc)
            return FilesAreEqualByHash(file1, file2);

        return true;
    }

    static bool FilesAreEqualByHash(string file1, string file2)
    {
        using var md5 = MD5.Create();
        using var fs1 = File.OpenRead(file1);
        using var fs2 = File.OpenRead(file2);

        var hash1 = md5.ComputeHash(fs1);
        var hash2 = md5.ComputeHash(fs2);

        return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
    }

    static void Log(string message, string logFile)
    {
        string logEntry = $"{DateTime.Now}: {message}";
        Console.WriteLine(logEntry);
        File.AppendAllText(logFile, logEntry + Environment.NewLine);
    }
}
