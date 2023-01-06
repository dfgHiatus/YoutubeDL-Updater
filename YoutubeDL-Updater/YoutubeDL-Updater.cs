using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Policy;

namespace YoutubeDLUpdater
{
    public class YoutubeDLUpdater : NeosMod
    {
        public override string Name => "YoutubeDL-Updater";
        public override string Author => "dfgHiatus";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/dfgHiatus/YoutubeDL-Updater";

        private static ModConfiguration modConfig;

        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            builder
                .Version(new Version(1, 0, 0))
                .AutoSave(true);
        }

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> runOnStartup = new ModConfigurationKey<bool>("run_on_startup", "Check for updates on startup", () => true);

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> useDefaultYoutubeDL = new ModConfigurationKey<bool>("use_default_youtubeDL", "Use Neos's default YoutubeDL (requires restart)", () => false);

        private static readonly Url PATH_SHA_256 = new Url("https://github.com/yt-dlp/yt-dlp/releases/download/2023.01.02/SHA2-256SUMS");
        private static readonly Url PATH_YT_DLP = new Url("https://github.com/yt-dlp/yt-dlp/releases/download/2023.01.02/yt-dlp.exe");
        private static readonly string EXECUTABLE_PATH = Path.Combine(Engine.Current.AppPath, "RuntimeData");
        private static readonly string BACKUP_PATH = Path.Combine(EXECUTABLE_PATH, "YoutubeDL-Backups");
        private const string EXECUTABLE_NAME = "yt-dlp.exe";
        
        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.YoutubeDLUpdater").PatchAll();
            modConfig = GetConfiguration();

            EnsureBackup();

            if (modConfig.GetValue(useDefaultYoutubeDL))
            {
                LoadBackup();
                return;
            }

            if (modConfig.GetValue(runOnStartup))
            {
                UpdateYoutubeDL();
            }
        }

        private void EnsureBackup()
        {
            Directory.CreateDirectory(BACKUP_PATH);

            // See if we already have a backup
            if (Directory.GetFiles(BACKUP_PATH).Length > 0) return;

            // If we don't have a backup (first time!), make it
            File.Copy(Path.Combine(EXECUTABLE_PATH, EXECUTABLE_NAME), Path.Combine(BACKUP_PATH, EXECUTABLE_NAME));
        }
        
        private void LoadBackup()
        {
            // Get the SHA256 of the backup and the current executable
            string backupSHA256 = ComputeSHA256(Path.Combine(BACKUP_PATH, EXECUTABLE_NAME));
            string currentSHA256 = ComputeSHA256(Path.Combine(EXECUTABLE_PATH, EXECUTABLE_NAME));
            
            if (backupSHA256 != currentSHA256)
                File.Copy(Path.Combine(BACKUP_PATH, EXECUTABLE_NAME), Path.Combine(EXECUTABLE_PATH, EXECUTABLE_NAME));

            // modConfig.Set(useDefaultYoutubeDL, false);
        }

        private void UpdateYoutubeDL()
        {
            // Download the text file at PATH_SHA_256. This might throw, so we need to wrap it
            string sha256 = string.Empty;
            try
            {
                using (WebClient client = new WebClient())
                    sha256 = client.DownloadString(PATH_SHA_256.Value);
            }
            catch (Exception e)
            {
                Error("An error occured while downloading SHA256 hashes from yt-dlp's github.");
                Error(e.Message);
                return;
            }

            if (string.IsNullOrEmpty(sha256))
            {
                Error("There were no SHA256 hashes from yt-dlp's github. Aborting update.");
                return;
            }

            // Split the string on newlines
            string[] allLines = sha256.Split('\n');

            // Get the entry that ends with "yt-dlp.exe"
            string line = Array.Find(allLines, x => x.EndsWith(EXECUTABLE_NAME));

            // We now have a string that looks like "<SHA256>  yt-dlp.exe", with 2 spaces(?!). To get the hash, we remove the executable name plus 2 spaces
            string stringHash = line.Remove(line.Length - (EXECUTABLE_NAME.Length + 2));

            // Compute the SHA256 of yt-dlp.exe in the above directory
            string computedHash = ComputeSHA256(Path.Combine(EXECUTABLE_PATH, EXECUTABLE_NAME));

            // If these 2 are equal, then we have the latest build
            if (stringHash == computedHash)
            {
                Msg($"yt-dlp.exe is up to date.");
                return;
            }

            Msg($"Local yt-dlp.exe is {computedHash} but remote is {stringHash}! Downloading the latest version...");

            // Download the binary file at PATH_YT_DLP. This might throw, so we need to wrap it again
            try
            {
                using (WebClient client = new WebClient())
                    client.DownloadFile(PATH_YT_DLP.Value, Path.Combine(EXECUTABLE_PATH, EXECUTABLE_NAME));
            }
            catch (Exception e)
            {
                Error("An error occured while downloading yt-dlp.exe from github.");
                Error(e.Message);
                return;
            }

            Msg($"Download complete! yt-dlp.exe is now up to date.");
        }

        // https://stackoverflow.com/questions/38474362/get-a-file-sha256-hash-code-and-checksum with corrections
        public static string ComputeSHA256(string filePath)
        {
            using (SHA256 SHA256 = SHA256Managed.Create())
            using (FileStream fileStream = File.OpenRead(filePath))
                return BitConverter.ToString(SHA256.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
        }
    }
}