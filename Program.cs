using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        // Check for minimum required arguments
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: <program> <filePath> <key>");
            Console.WriteLine("Example: myapp.exe file.tlk tlk2txt");
            return;
        }

        string filePath = args[0];
        string key = args[1].ToLowerInvariant();

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File not found: {filePath}");
            return;
        }

        try
        {
            string content = File.ReadAllText(filePath);

            // Handle key logic
            switch (key)
            {
                case "tlk2txt":
                    Console.WriteLine("Converting TLK to TXT (simulated):");
                    Console.WriteLine(content);
                    break;
                case "txt2tlk":
                    Console.WriteLine("Converting TXT to TLK (simulated):");
                    Console.WriteLine(content);
                    break;
                default:
                    Console.WriteLine("Unknown key. Supported keys: tlk2txt, txt2tlk");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
        }
    }
}