using BaseX;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace YoutubeDLUpdater
{
    public class YoutubeDLUpdater : NeosMod
    {
        public override string Name => "YoutubeDL-Updater";
        public override string Author => "dfgHiatus and Frozen";
        public override string Version => "1.0.2";
        public override string Link => "https://github.com/dfgHiatus/YoutubeDL-Updater";

        private static ModConfiguration _modConfig;

        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            builder
                .Version(new Version(1, 0, 1))
                .AutoSave(true);
        }

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> _runOnStartup = new ModConfigurationKey<bool>("run_on_startup", "Check for updates on startup", () => true);

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> _useDefaultYoutubeDl = new ModConfigurationKey<bool>("use_default_youtubeDL", "Use Neos's default YoutubeDL (requires restart)", () => false);

        private static readonly string ExecutablePath = Path.Combine(Engine.Current.AppPath, "RuntimeData");
        private static readonly string BackupPath = Path.Combine(ExecutablePath, "YoutubeDL-Backups");
        private const string ExecutableName = "yt-dlp.exe";
        
        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.YoutubeDLUpdater").PatchAll();
            _modConfig = GetConfiguration();
            EnsureBackup();
            if (_modConfig == null) return;
            if (_modConfig.GetValue(_useDefaultYoutubeDl))
            {
                LoadBackup();
                return;
            }
            if (_modConfig.GetValue(_runOnStartup)) UpdateYoutubeDL();
        }

        private static void EnsureBackup()
        {
            Directory.CreateDirectory(BackupPath);
            // See if we already have a backup
            if (Directory.GetFiles(BackupPath).Length > 0) return;
            // If we don't have a backup (first time!), make it
            File.Copy(Path.Combine(ExecutablePath, ExecutableName), Path.Combine(BackupPath, ExecutableName));
        }
        
        private static void LoadBackup()
        {
            // Get the SHA256 of the backup and the current executable
            var backupSha256 = ComputeSha256(Path.Combine(BackupPath, ExecutableName));
            var currentSha256 = ComputeSha256(Path.Combine(ExecutablePath, ExecutableName));
            if (backupSha256 != currentSha256)
                File.Copy(Path.Combine(BackupPath, ExecutableName), Path.Combine(ExecutablePath, ExecutableName));
        }

        private static void UpdateYoutubeDL()
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = $@"/C {ExecutablePath}/{ExecutableName} -U",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
            };
            
            process.StartInfo = startInfo;
            process.OutputDataReceived += CopyOutput;
            process.Start();
            process.WaitForExit();

            Msg($"Download complete! yt-dlp.exe is now up to date.");
        }

        private static void CopyOutput(object sender, DataReceivedEventArgs e)
        {
            UniLog.Log(e.Data);
        }

        // https://stackoverflow.com/questions/38474362/get-a-file-sha256-hash-code-and-checksum with corrections
        private static string ComputeSha256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var fileStream = File.OpenRead(filePath))
                return BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
        }
    }
}