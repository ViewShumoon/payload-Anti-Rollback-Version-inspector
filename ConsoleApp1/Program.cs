using System.Diagnostics;

namespace ConsoleApp1
{
    internal class Program
    {
        private string ImageFolderName => "images";
        private string ToolsFolderName => "tools";
        private string SecToolsName => "sectools.exe";

        static void Main(string[] args)
        {
            var program = new Program();
            program.ScanImages();
        }

        public void ScanImages()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string imageFolder = Path.Combine(baseDir, ImageFolderName);

            if (!Directory.Exists(imageFolder))
            {
                Console.WriteLine($"[Error] {imageFolder} folder not exist.");
                return;
            }

            string secToolsPath = Path.Combine(baseDir, ToolsFolderName, SecToolsName);
            if (!File.Exists(secToolsPath))
            {
                Console.WriteLine($"[Error] {secToolsPath} folder not exist.");
                return;
            }

            string[] imgFiles = Directory.GetFiles(imageFolder, "*.img");

            Console.WriteLine($"Found {imgFiles.Length} .img files, starting inspection.");

            if (imgFiles.Any())
            {
                Console.WriteLine($"| Image | Name | Value |");

                foreach (string imgFile in imgFiles)
                {
                    string fileName = Path.GetFileName(imgFile);

                    var (code, line) = RunSecTools(secToolsPath, imgFile);
                    switch (code)
                    {
                        case 1:
                            Console.WriteLine($"| {fileName} {line}");
                            break;
                        //case 0:
                        //    Console.WriteLine($"| {fileName}: Not found: {line} |  |");
                        //    break;

                        default:
                            Console.WriteLine($"| {fileName} | Not found |  |");
                            break;
                    }
                    ;
                }
            }

            Console.WriteLine("Inspection complete!");
        }

        private (int code, string message) RunSecTools(string secToolsPath, string imagePath)
        {
            try
            {
                var arguments = $"secure-image --inspect \"{imagePath}\"";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = secToolsPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        foreach (var line in output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                        {
                            if (line.Contains("Anti-Rollback"))
                            {
                               return (1, line.Trim());
                            }
                        }
                    }

                    string error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(error))
                    {
                        return (-1, error);
                    }

                    return (0, string.Empty);
                }
            }
            catch (Exception ex)
            {
                return (-2, $"An exception occurred while executing the command: {ex.Message}");
            }
        }
    }
}
