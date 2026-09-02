using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Updater
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Crossworlds Mod Manager Updater";
            Console.WriteLine("Updater started...");

            // 1. Parse command-line arguments
            var pid = GetArgument(args, "--pid");
            var appPath = GetArgument(args, "--appPath");
            var downloadUrl = GetArgument(args, "--downloadUrl");

            if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(appPath) || string.IsNullOrEmpty(downloadUrl))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nError: Missing required arguments.");
                Console.WriteLine("This updater is intended to be run by the main application.");
                Console.WriteLine("Usage: updater.exe --pid <ID> --appPath \"<PATH>\" --downloadUrl \"<URL>\"");
                PauseAndExit();
                return;
            }

            Console.WriteLine($" > Parent Process ID: {pid}");
            Console.WriteLine($" > Application Path: {appPath}");
            Console.WriteLine($" > Download URL: {downloadUrl}");

            // 2. Wait for and kill the main application process
            if (int.TryParse(pid, out int processId))
            {
                try
                {
                    var mainAppProcess = Process.GetProcessById(processId);
                    Console.WriteLine("\nWaiting for main application to close...");
                    mainAppProcess.Kill();
                    mainAppProcess.WaitForExit(5000); // Wait up to 5 seconds
                    Console.WriteLine("Main application closed successfully.");
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Main application process not found. It may have already closed.");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error closing main application: {ex.Message}");
                    PauseAndExit();
                    return;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid Process ID provided.");
                PauseAndExit();
                return;
            }

            // Small delay to ensure all file handles from the old process are released.
            Thread.Sleep(1000);

            string? appDirectory = Path.GetDirectoryName(appPath);
            if (string.IsNullOrEmpty(appDirectory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not determine application directory from the provided path.");
                PauseAndExit();
                return;
            }

            string downloadedZipPath = Path.Combine(appDirectory, "update.zip");

            try
            {
                // 3. Download the new version
                Console.WriteLine("\nDownloading new version...");
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(downloadedZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                Console.WriteLine("Download complete.");

                // 4. Extract the contents, overwriting old files
                Console.WriteLine("\nExtracting update...");
                // We can't use ExtractToDirectory directly because it will fail if it tries to overwrite
                // the updater's own executable/DLLs which are currently in use.
                // Instead, we'll iterate and extract file-by-file, skipping any that are locked.
                using (ZipArchive archive = ZipFile.OpenRead(downloadedZipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(appDirectory, entry.FullName);
                        
                        // This handles empty directory entries
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                            continue;
                        }

                        try
                        {
                            entry.ExtractToFile(destinationPath, true);
                        }
                        catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                        {
                            Console.WriteLine($" > Skipping locked file: {entry.Name}");
                        }
                    }
                }
                Console.WriteLine("Extraction complete.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nAn error occurred during the update process: {ex.Message}");
                Console.WriteLine("The application may be in an inconsistent state. Please try re-downloading from GitHub.");
                PauseAndExit();
                return;
            }
            finally
            {
                // 5. Clean up the downloaded zip file
                if (File.Exists(downloadedZipPath))
                {
                    try
                    {
                        File.Delete(downloadedZipPath);
                        Console.WriteLine("\nCleaned up temporary files.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not delete temporary file '{downloadedZipPath}'. {ex.Message}");
                    }
                }
            }

            // 6. Relaunch the main application
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nUpdate successful! Relaunching the application...");
            Thread.Sleep(2000); // Give user time to read the message

            Process.Start(new ProcessStartInfo(appPath) { UseShellExecute = true });
        }

        /// <summary>
        /// A simple helper to parse named command-line arguments.
        /// </summary>
        private static string? GetArgument(string[] args, string option)
        {
            return args.SkipWhile(val => val != option).Skip(1).FirstOrDefault();
        }

        /// <summary>
        /// Pauses the console window before exiting so the user can read the error.
        /// </summary>
        private static void PauseAndExit()
        {
            Console.ResetColor();
            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}
