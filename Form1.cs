using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;
using System.Diagnostics;

namespace simple_updater
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string downloadsPath = "downloads";

        private async void Form1_Load(object sender, EventArgs e)
        {
            FileDownloadResult res = new FileDownloadResult { success = false, filename = "" };
            try
            {
                string updateFileUrl = File.ReadAllText(Application.StartupPath.ToString() + "\\" + "updateLink.txt");
                res = await DownloadFileAsync(updateFileUrl, downloadsPath);
            }
            catch(Exception ee) { }

            if (res.success)
            {
                try
                {
                    string filePath = Path.Combine(downloadsPath, res.filename);

                    string extractFolderPath = Path.Combine(downloadsPath, Path.GetFileNameWithoutExtension(filePath));

                    await ExtractUpdate(filePath, extractFolderPath);

                    File.Delete(filePath);

                    string fullAppDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;


                    await CreateBackupForUpperFolderAsync();

                    string path = Directory.GetParent(Application.StartupPath).ToString();

                    Directory.CreateDirectory(path);

                    await copyFolderToAnotherFolderAsync(Path.Combine(fullAppDirectoryPath, extractFolderPath), path);


                    //System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(Path.Combine(fullAppDirectoryPath, extractFolderPath));
                    //Empty(directory);


                    int newVersion = await loadFreshVersion();
                    File.WriteAllText("version.txt", newVersion.ToString());
                }
                catch (Exception exx){  }

                try
                {
                    string appExe = Directory.GetParent(Application.StartupPath).ToString() + "\\" + File.ReadAllText("updateExe.txt");
                    ProcessStartInfo info = new ProcessStartInfo(appExe);
                    info.FileName = appExe;
                    info.WorkingDirectory = Path.GetDirectoryName(appExe);
                    Process p = Process.Start(info);
                    p.WaitForInputIdle();
                }
                catch(Exception eeee)
                {

                }

                //await waitTime(15000);

                Environment.Exit(0);
            }
        }


        void Empty(System.IO.DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }

        static async Task<int> loadFreshVersion()
        {

            using (WebClient client = new WebClient())
            {
                string url = File.ReadAllText("versionLink.txt");

                try
                {
                    return Convert.ToInt32(await client.DownloadStringTaskAsync(url));
                }
                catch (Exception exx) { return 0; }
            }
        }

        async Task<bool> waitTime(int ms)
        {
            DateTime timeold = DateTime.Now;
            while (timeold.AddMilliseconds(ms) >= DateTime.Now) Application.DoEvents();
            return true;
        }

        async Task<bool> copyFolderToAnotherFolderAsync(string folder1, string folder2)
        {
            string[] files = Directory.GetFiles(folder1, "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                try
                {
                    string relativePath = GetRelativePath(file, folder1);
                    string currentFilePath = Path.Combine(folder2, relativePath);

                    string g = Path.GetDirectoryName(currentFilePath);
                    Directory.CreateDirectory(g);

                    File.Copy(file, currentFilePath, true);
                    //await CopyFileAsync(file, currentFilePath);
                }
                catch (Exception ex)
                {
                    string s = "";
                }
            }

            return true;
        }

        async Task<bool> CreateBackupForUpperFolderAsync()
        {
            Directory.CreateDirectory("backups");

            string updateDirectory = Directory.GetParent(Application.StartupPath).ToString();
            string updateDirectoryName = new DirectoryInfo(updateDirectory).Name;
            string[] files = Directory.GetFiles(updateDirectory, "*", SearchOption.AllDirectories);

            string backupFolder = updateDirectoryName; //DateTime.Now.ToString("yyyy_MM_dd   HH-mm-ss - ") + ;

            string backupPath = Path.Combine("backups", backupFolder);


            Directory.CreateDirectory(backupPath);

            // File.Delete(backupPath);

            foreach (string file in files)
            {
                try
                {
                    if (file.Contains("simple_updater")) {
                        continue; }
                    //string relativePath = GetRelativePath(file, updateDirectory);

                    string relativePath = GetRelativePath(file, updateDirectory);
                    string currentFilePath = Path.Combine(backupPath, relativePath);

                    string g = Path.GetDirectoryName(currentFilePath);
                    Directory.CreateDirectory(g);

                    File.Copy(file, currentFilePath, true);
                    //await CopyFileAsync(file, currentFilePath);
                }
                catch (Exception ex)
                {
                    string s = "";
                }
            }

            return true;
        }

        public static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var destinationStream = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                await sourceStream.CopyToAsync(destinationStream);
        }

        string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }



        private async Task<FileDownloadResult> DownloadFileAsync(string url, string downloadsPath)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
                    client.DownloadProgressChanged += WebClient_DownloadProgressChanged;

                    var c = client.OpenRead(new Uri(url));

                    string header_contentDisposition = client.ResponseHeaders["content-disposition"];
                    string filename = new System.Net.Mime.ContentDisposition(header_contentDisposition).FileName;

                    c.Close();

                    string filepath = Path.Combine(downloadsPath, filename);
                    await client.DownloadFileTaskAsync(new Uri(url), filepath).ConfigureAwait(true);

                    return new FileDownloadResult { success = true, filename = filename };
                }
                catch (Exception ex)
                {
                    return new FileDownloadResult { success = false, filename = "" };
                }
            }
        }

        async Task<bool> ExtractUpdate(string filePath, string extractFolderPath)
        {
            try
            {
                using (ZipStorer zip = ZipStorer.Open(filePath, FileAccess.Read))
                {
                    List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();

                    foreach (ZipStorer.ZipFileEntry entry in dir)
                    {
                        zip.ExtractFile(entry, Path.Combine(extractFolderPath, entry.FilenameInZip));
                        await zip.ExtractFileAsync(entry, new MemoryStream());
                    }
                }

                return true;
            }
            catch(Exception eee) { return false; }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        /*
        private void backupUpperLevelFolder()
        {
            string baseFolder = "backups";

            string source = Directory.GetParent(
                Application.StartupPath
            ).ToString();


            string target = Path.Combine(baseFolder, DateTime.Now.ToString("yyyyMMddHHmmss"));
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }
            CopyFolder(source, target);
        }
        private void CopyFolder(string source, string destinationBase)
        {
            CopyFolder(new DirectoryInfo(source), destinationBase);

            var h = new DirectoryInfo(source).GetDirectories("*.*", SearchOption.AllDirectories);

            foreach (DirectoryInfo di in h)
            {
                string dirName = di.Name;
                if (dirName.Contains("simple_updater"))
                {
                    continue;
                }
                CopyFolder(di, destinationBase);
            }
        }

        private void CopyFolder(DirectoryInfo di, string destinationBase)
        {
            string destinationFolderName = Path.Combine(destinationBase, di.FullName.Replace(":", ""));
            if (!Directory.Exists(destinationFolderName))
            {
                Directory.CreateDirectory(destinationFolderName);
            }
            foreach (FileInfo fi in di.GetFiles())
            {
                fi.CopyTo(Path.Combine(destinationFolderName, fi.Name), false);
            }
        }*/
    }

    public class FileDownloadResult {
        public bool success;
        public string filename;
    }
}
