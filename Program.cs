using GFF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class ParsedItem
{
    public string Id { get; set; }
    public string Value { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        // Check for minimum required arguments
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: <program> <key> <inputFilePath> <outputFilePath>, where key is tlk2txt or txt2tlk");
            Console.WriteLine("Example: myapp.exe file.tlk tlk2txt");
            return;
        }

        string key = args[0].ToLowerInvariant();
        string inputFilePath = args[1];
        string outputFilePath = args[2];

        try
        {
            // Handle key logic
            switch (key)
            {
                case "tlk2txt":
                    Console.WriteLine("Converting TLK to TXT:");

                    if (!File.Exists(inputFilePath))
                    {
                        Console.WriteLine($"File not found: {inputFilePath}");
                        return;
                    }

                    GffTlkFile tlk_file = new GffTlkFile();
                    tlk_file.Load(inputFilePath);
                    List<TlkItem> exported_list = tlk_file.Export(false, false);
                    exported_list = exported_list.OrderBy(item => item.RefNo).ToList();

                    if (!File.Exists(outputFilePath))
                    {
                        // This will create the file. You can also write initial content if needed.
                        File.Create(outputFilePath).Dispose(); // Dispose to release the file handle immediately
                        // Optionally, write initial content:
                        // File.WriteAllText(path, "Initial content");
                    }

                    using (StreamWriter writer = new StreamWriter(outputFilePath))
                    {
                        foreach (TlkItem line in exported_list)
                        {
                            writer.WriteLine($"{{{line.RefNo}}} {{{line.Text}}}");
                        }
                    }

                    Console.WriteLine($"Result written to {outputFilePath}");
                    break;
                case "txt2tlk":
                    Console.WriteLine("Converting TXT to TLK");

                    if (!File.Exists(inputFilePath))
                    {
                        Console.WriteLine($"File not found: {inputFilePath}");
                        return;
                    }

                    if (!File.Exists(outputFilePath))
                    {
                        Console.WriteLine($"File not found: {outputFilePath}");
                        return;
                    }

                    GffTlkFile tlk_file_output = new GffTlkFile();
                    tlk_file_output.Load(outputFilePath);

                    List<TlkItem> output_list = tlk_file_output.Export(false, false);


                    List<ParsedItem> items = new List<ParsedItem>();
                    string pattern = @"^\{(\d+)\}\s*\{";
                    using (StreamReader reader = new StreamReader(inputFilePath))
                    {
                        string? line;
                        string? currentId = null;
                        StringBuilder currentValue = new StringBuilder();
                        bool capturing = false;

                        while ((line = reader.ReadLine()) != null)
                        {
                            Match match = Regex.Match(line, pattern);
                            if (match.Success)
                            {
                                // Save previous item if any
                                if (currentId != null)
                                {
                                    items.Add(new ParsedItem { Id = currentId, Value = currentValue.ToString() });
                                    currentValue.Clear();
                                }
                                // Start new item
                                currentId = match.Groups[1].Value;

                                // Find the start of the value (after the first closing brace)
                                int firstClose = line.IndexOf('}');
                                int secondOpen = line.IndexOf('{', firstClose + 1);
                                int secondClose = line.LastIndexOf('}');
                                if (secondOpen != -1 && secondClose != -1 && secondClose > secondOpen)
                                {
                                    string valuePart = line.Substring(secondOpen + 1, secondClose - secondOpen - 1);
                                    currentValue.Append(valuePart);
                                }
                                capturing = true;
                            }
                            else if (capturing)
                            {
                                // Continuation of value (multi-line)
                                currentValue.AppendLine();
                                currentValue.Append(line);
                            }
                        }
                        // Add the last item
                        if (currentId != null)
                        {
                            items.Add(new ParsedItem { Id = currentId, Value = currentValue.ToString() });
                        }
                    }

                    foreach (var item in items)
                    {
                        string sanitized_value = item.Value;
                        if (!string.IsNullOrEmpty(sanitized_value) && sanitized_value[^1] == '}')
                        {
                            sanitized_value = sanitized_value.Substring(0, sanitized_value.Length - 1);
                        }
                        output_list.Find(x => x.RefNo == int.Parse(s: item.Id)).Text = sanitized_value;
                        
                    }

                    tlk_file_output.Import(output_list, false);
                    tlk_file_output.SaveAs(outputFilePath);

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