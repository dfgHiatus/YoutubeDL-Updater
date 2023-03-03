using FrooxEngine;
using System;
using System.IO;
using System.Linq;
using System.Security.Policy;

namespace YoutubeDLUpdater
{
    internal static class YoutubeDLTarget
    {
        internal static readonly Url PathSha256 = new Url("https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS");
        private static string _linuxYtDlpCache;

        internal static Url PathYtDlp
        {
            get
            {
                switch (Engine.Current.Platform)
                {
                    case Platform.Windows:
                        return new Url("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe");
                    case Platform.OSX:
                        return new Url("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_macos");
                    case Platform.Linux:
                        return new Url("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux");
                    // return GetLinuxPath("ffmpeg", ref _linuxYtDlpCache);
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal static string ExecutableName
        {
            get
            {
                switch (Engine.Current.Platform)
                {
                    case Platform.Windows:
                        return "yt-dlp.exe";
                    case Platform.OSX:
                        return "yt-dlp_macos";
                    case Platform.Linux:
                        return ("yt-dlp_linux");
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        // TODO Check if youtube-dl is installed as a package on non-windows machines?
        private static string GetLinuxPath(string command, ref string cache)
        {
            if (string.IsNullOrWhiteSpace(cache))
            {
                var path = Environment.GetEnvironmentVariable("PATH") ?? "";
                var paths = path.Split(Path.PathSeparator).ToList();
                var test = paths.Select(i => Path.Combine(i, command)).FirstOrDefault(File.Exists);
                cache = string.IsNullOrWhiteSpace(test) ? "notfound" : test;
            }
            return cache;
        }
    }
}
