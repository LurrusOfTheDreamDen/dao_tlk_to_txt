using GFF;
using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        // Check for minimum required arguments
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: <program> <key> <filePath> <filePath>, where key is tlk2txt or txt2tlk");
            Console.WriteLine("Example: myapp.exe file.tlk tlk2txt");
            return;
        }

        string key = args[0].ToLowerInvariant();
        string filePathTlk = args[1];
        string filePathTxt = args[2];

        if (!File.Exists(filePathTlk))
        {
            Console.WriteLine($"File not found: {filePathTlk}");
            return;
        }

        //if (!File.Exists(filePathTxt))
        //{
        //    Console.WriteLine($"File not found: {filePathTxt}");
        //    return;
        //}

        try
        {
            //string content = File.ReadAllText(filePathTlk);

            // Handle key logic
            switch (key)
            {
                case "tlk2txt":
                    Console.WriteLine("Converting TLK to TXT (simulated):");
                    GffTlkFile tlk_file = new GffTlkFile();

                    tlk_file.Load(filePathTlk);

                    List <TlkItem> exported_list = tlk_file.Export(false, false);
                    //exported_list = exported_list.OrderBy(item => item.RefNo).ToList();
                    //for (int i = 0; i < exported_list.Count; i++)
                    //{
                    //    TlkItem item = exported_list[i];
                    //    Console.WriteLine($"RefNo: {item.RefNo} Text: {item.Text}");
                    //    Console.ReadLine();
                    //}

                    // experimental

                    exported_list.ForEach(x => x.Text = "ўўў ЎЎЎЎ ііі ІІІІ тэст");

                    GffTlkFile tlk_file_exp = new GffTlkFile();
                    tlk_file_exp.Load(filePathTlk);

                    tlk_file_exp.Import(exported_list, false);
                    tlk_file_exp.SaveAs("path");

                    //Console.WriteLine(content);
                    break;
                case "txt2tlk":
                    Console.WriteLine("Converting TXT to TLK (simulated):");
                    //Console.WriteLine(content);
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