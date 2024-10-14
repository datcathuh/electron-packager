using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

class CustomElectronPackager
{
    static async Task Main(string[] args)
    {
        string projectPath = @"C:\Users\sheepy\source\repos\electron compiler\bin\Debug\net8.0\my-electron-app";
        string outputPath = @"C:\Users\sheepy\source\repos\electron compiler\bin\Debug\net8.0";
        string tempElectronPath = @"C:\Users\sheepy\AppData\Local\Temp\electron";
        string electronVersion = "25.3.0";
        string platform = "win32";
        string arch = "x64";
        string appName = "test";

        var electronPath = await DownloadElectronBinaries(electronVersion, platform, arch, outputPath);
        PackageElectronApp(projectPath, electronPath, outputPath);
        var exePath = RenameElectronExecutable(outputPath, appName);
        var finalExePath = MovePackagedApp(outputPath, tempElectronPath);
        CreateShortcutOnDesktop(appName, finalExePath);
        RunElectronApp(finalExePath);

        Console.WriteLine("Packaging complete. App is running.");
    }

    static async Task<string> DownloadElectronBinaries(string version, string platform, string arch, string outputDir)
    {
        string url = $"https://github.com/electron/electron/releases/download/v{version}/electron-v{version}-{platform}-{arch}.zip";
        string zipPath = Path.Combine(outputDir, "electron.zip");

        Console.WriteLine($"Downloading Electron {version}...");

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            await using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        var extractPath = Path.Combine(outputDir, "electron");
        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true); 
        }

        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath, true);  
        Console.WriteLine("Download and extraction complete.");

        return extractPath;
    }

    static void PackageElectronApp(string projectPath, string electronPath, string outputPath)
    {
        string packagedPath = Path.Combine(outputPath, "packagedApp");
        Directory.CreateDirectory(packagedPath);

        CopyDirectory(electronPath, packagedPath);

        var resourcesPath = Path.Combine(packagedPath, "resources", "app");
        Directory.CreateDirectory(resourcesPath);

        CopyDirectory(projectPath, resourcesPath);
    }

    static string RenameElectronExecutable(string outputPath, string appName)
    {
        string exePath = Path.Combine(outputPath, "packagedApp", "electron.exe");
        string newExePath = Path.Combine(outputPath, "packagedApp", $"{appName}.exe");

        if (File.Exists(exePath))
        {
            File.Move(exePath, newExePath);
            Console.WriteLine($"Renamed to {appName}.exe.");
        }
        return newExePath;
    }

    static string MovePackagedApp(string sourceDir, string targetDir)
    {
        string sourcePath = Path.Combine(sourceDir, "packagedApp");
        string destinationPath = Path.Combine(targetDir, "packagedApp");

        if (Directory.Exists(destinationPath))
        {
            Directory.Delete(destinationPath, true);
        }

        Directory.CreateDirectory(targetDir);
        CopyDirectory(sourcePath, destinationPath);

        Console.WriteLine($"Moved to {destinationPath}.");
        return Path.Combine(destinationPath, "test.exe");
    }

    static void CreateShortcutOnDesktop(string appName, string exePath)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string shortcutPath = Path.Combine(desktopPath, $"{appName}.lnk");

        using (var writer = new StreamWriter(shortcutPath))
        {
            writer.WriteLine("[InternetShortcut]");
            writer.WriteLine($"URL=file:///{exePath}");
            writer.WriteLine("IconIndex=0");
            string iconPath = exePath.Replace('\\', '/');
            writer.WriteLine($"IconFile={iconPath}");
        }

        Console.WriteLine("Shortcut created on desktop.");
    }

    static void RunElectronApp(string exePath)
    {
        if (File.Exists(exePath))
        {
            Console.WriteLine($"Running {exePath}...");

            var startInfo = new ProcessStartInfo(exePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }

            Console.WriteLine("App exited.");
        }
    }

    static void CopyDirectory(string sourceDir, string targetDir)
    {
        foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourceDir, targetDir));
        }

        foreach (var filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(filePath, filePath.Replace(sourceDir, targetDir), true);
        }
    }
}
