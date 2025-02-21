using System.Diagnostics;
using CommandLine;


public class Options
{
    [Option("printer", Required = false, HelpText = "Name of the printer.")]
    public string PrinterName { get; set; }

    [Option("watchpath", Required = true, HelpText = "Directory to watch for changes.")]
    public string WatchPath { get; set; }

    [Option("printedpath", Required = false, HelpText = "Where to place the files when printed.")]
    public string PrintedPath { get; set; }
}

class Program
{
    private string printer = null;
    private static string watchDirectory = @"D:\share\TO-PRINT";
    private static string printedDirectory = @"D:\share\TO-PRINT\PRINTED";

    static void Main(string[] args)
    {

        Parser.Default.ParseArguments<Options>(args)
        .WithParsed(options =>
        {
            printedDirectory = options.PrintedPath;
            Console.WriteLine("Printer: " + options.PrinterName);
            Console.WriteLine("Watch Path: " + options.WatchPath);
            Console.WriteLine("Printed Path: " + options.PrintedPath);


            // Ensure directories exist.
            Directory.CreateDirectory(options.WatchPath);
            Directory.CreateDirectory(options.PrintedPath);

            using (var watcher = new FileSystemWatcher(options.WatchPath, "*.pdf"))
            {
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                watcher.Created += OnFileCreated;
                watcher.EnableRaisingEvents = true;

                Console.WriteLine("Monitoring directory: " + options.WatchPath);
                Console.WriteLine("Press 'q' then ENTER to quit.");

                while (Console.ReadLine()?.ToLower() != "q") { }
            }


        })
        .WithNotParsed(errors =>
        {
            // Handle errors, display help, etc.
            Console.WriteLine("Invalid arguments provided.");
            Environment.Exit(1);
        });


    }

    private static void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("New file detected: " + e.FullPath);
        // Wait briefly to ensure the file is completely written.
        Thread.Sleep(1000);

        bool printed = false;
        string extension = Path.GetExtension(e.FullPath);
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                PrintPdfViaAcrobat(e.FullPath);
                //PrintPdf(e.FullPath);
                Console.WriteLine("PDF printed successfully using gembox.");
                printed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error printing PDF file: " + ex.Message);
            }
        }
        // Code to handle non PDF files, might fix later but right now it's a little buggy
        //else
        //{
        //    try
        //    {
        //        Process printProcess = new Process();
        //        printProcess.StartInfo.FileName = e.FullPath;
        //        printProcess.StartInfo.Verb = "Print";
        //        printProcess.StartInfo.UseShellExecute = true; // Needed for non-executable files.
        //        printProcess.StartInfo.CreateNoWindow = true;
        //        printProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        //        printProcess.Start();
        //        printProcess.WaitForExit(10000); // Wait up to 10 seconds.
        //        Console.WriteLine("Print command executed.");
        //        printed = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error printing file: " + ex.Message);
        //    }
        //}

        if (printed)
        {
            try
            {
                string destinationPath = Path.Combine(printedDirectory, Path.GetFileName(e.FullPath));
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
                File.Move(e.FullPath, destinationPath);
                Console.WriteLine("Moved file to: " + destinationPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error moving file: " + ex.Message);
            }
        }
    }

    
    public static void PrintPdfViaAcrobat(string pdfFilePath, string acrobatReaderPath = @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe", string printerName = null)
    {
        // Validate the PDF file path.
        if (string.IsNullOrWhiteSpace(pdfFilePath))
            throw new ArgumentException("PDF file path cannot be null or empty.", nameof(pdfFilePath));

        if (!File.Exists(pdfFilePath))
            throw new FileNotFoundException("The specified PDF file was not found.", pdfFilePath);

        // Validate the Acrobat Reader executable path.
        if (!File.Exists(acrobatReaderPath))
            throw new FileNotFoundException("Acrobat Reader executable was not found.", acrobatReaderPath);

        // Prepare the command-line arguments.
        // Acrobat Reader’s /t switch prints the file. The syntax is:
        // AcroRd32.exe /t <PDFfile> [<printerName> [<driverName> [<portName>]]]
        // For simplicity, if a printer name is provided we pass it; otherwise, only the PDF file.
        string arguments = string.IsNullOrEmpty(printerName)
            ? $"/t \"{pdfFilePath}\""
            : $"/t \"{pdfFilePath}\" \"{printerName}\"";

        // Setup the process to start Acrobat Reader with these arguments.
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = acrobatReaderPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using (Process process = Process.Start(psi))
            {
                if (process != null)
                {
                    // Wait for a reasonable time for Acrobat Reader to start printing.
                    process.WaitForExit(10000); // wait up to 10 seconds
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error printing PDF file: {ex.Message}");
        }
    }

}
