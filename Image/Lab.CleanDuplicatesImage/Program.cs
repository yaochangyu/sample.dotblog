using System.Security.Cryptography;

namespace Lab.CleanDuplicatesImage;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
    
    public static Dictionary<string, List<string>> FindDuplicates(string folderPath)
    {
        var hashGroups = new Dictionary<string, List<string>>();

        var imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));

        foreach (var file in imageFiles)
        {
            var hash = CalculateMD5(file);
            if (!hashGroups.ContainsKey(hash))
                hashGroups[hash] = new List<string>();

            hashGroups[hash].Add(file);
        }

        return hashGroups.Where(g => g.Value.Count > 1)
            .ToDictionary(g => g.Key, g => g.Value);
					
        string CalculateMD5(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}