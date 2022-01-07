# simple_updater
most simple way to update programs on the fly / Самый простой способ обновить программу на лету

last build / последняя сборка: https://www.dropbox.com/s/kss9g8ivi8q7gxu/simple_updater.zip?dl=1

Updating c# program / Обновление программы c#:

UpdaterFuncs.checkForUpdate();

    static class UpdaterFuncs
    {
        static bool updaterEnabled = true;

        public static int loadThisVersion()
        {
            int version = 0;

            try {
                version = Convert.ToInt32(File.ReadAllText(Application.StartupPath + "\\simple_updater\\version.txt")); }
            catch (Exception eee)
            {
            }
            return version;
        }

        static async Task<int> loadFreshVersion()
        {

            using (WebClient client = new WebClient())
            {
                try
                {
                    string url = File.ReadAllText(Application.StartupPath + "\\simple_updater\\versionLink.txt");
                    return Convert.ToInt32(await client.DownloadStringTaskAsync(url));
                }
                catch (Exception exx) { return 0; }
            }
        }
        public static async void checkForUpdate()
        {
            if (!updaterEnabled) return;

            int thisVersion = loadThisVersion();

            int freshVersion = await loadFreshVersion();

            try
            {
                if (freshVersion > thisVersion)
                {
                    ProcessStartInfo info = new ProcessStartInfo(Application.StartupPath + "\\simple_updater\\simple_updater.exe");
                    //info.FileName = Application.StartupPath + "\\simple_updater\\simple_updater.exe";
                    info.WorkingDirectory = Application.StartupPath.ToString() + "\\simple_updater\\";
                    Process p = Process.Start(info);
                    p.WaitForInputIdle();
                    Environment.Exit(0);
                }
            }
            catch(Exception eee) { }

        }
    }
