using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;

namespace YouTubeRIP_v2
{
    public class Worker
    {
        public int Id { get; private set; }
        private string Url { get; set; }
        public string VideoName { get; private set; }
        public string AudioName { get; private set; }
        public string DownloadVideoSpeedStr { get; private set; }
        public string DownloadAudioSpeedStr { get; private set; }
        public string VideoFileSize { get; set; }
        public string AudioFileSize { get; set; }
        public string VideoFileDownloadedSize { get; set; }
        public string AudioFileDownloadedSize { get; set; }
        public string VideoDownloadedPercent { get; private set; }
        public string AudioDownloadedPercent { get; private set; }

        private static object Locker = new object();
        public Worker(int id, string url)
        {
            Id = id;
            Url = url;
        }
        public async Task Awake()
        {
            var videoInfos = YouTube.Default.GetAllVideos(Url);
            Task<string> videoName = Task.Run(() => VideoDownload(videoInfos));
            Task<string> audioName = Task.Run(() => AudioDownload(videoInfos));
            Task.WaitAll(new Task[] { videoName, audioName });
            Merger(videoName.Result, audioName.Result);
        }
        string VideoDownload(IEnumerable<YouTubeVideo> youTubeVideos)
        {
            var maxResolution = youTubeVideos.First(i => i.Resolution == youTubeVideos.Max(j => j.Resolution));
            string normalizeName = NormalizeName(maxResolution.FullName, false);
            Task.WaitAll(Task.Run(() => DownloadFile(maxResolution.Uri, normalizeName)));
            return normalizeName;
        }
        string AudioDownload(IEnumerable<YouTubeVideo> youTubeVideos)
        {
            var audioFormat = youTubeVideos.Where(i => i.AudioFormat == AudioFormat.Aac);
            var bitrate = audioFormat.First(i => i.AudioBitrate == audioFormat.Max(j => j.AudioBitrate));
            string normalizeName = NormalizeName(bitrate.FullName, true);
            Task.WaitAll(Task.Run(() => DownloadFile(bitrate.Uri, normalizeName)));
            return normalizeName;
        }
        public void Merger(string videoName, string audioName)
        {
            lock (Locker)
            {
                string ffmpegCommand = $"-i \"{Directory.GetCurrentDirectory()}\\{Program.WaitForDownloadDirectory}\\{videoName}\" " +
                    $"-i \"{Directory.GetCurrentDirectory()}\\{Program.WaitForDownloadDirectory}\\{audioName}\" " +
                    $"-c copy \"{Directory.GetCurrentDirectory()}\\{Program.ResultDirectoryName}\\{videoName.Replace(".webm", ".mp4")}\"";
                string ffmpegPath = Directory.GetCurrentDirectory() + "\\ffmpeg.exe";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                 // startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                startInfo.FileName = "ffmpeg.exe";
                /*
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.StandardInputEncoding = System.Text.Encoding.GetEncoding(65001);
                startInfo.StandardOutputEncoding = System.Text.Encoding.GetEncoding(65001);
                */
                startInfo.Arguments = ffmpegCommand;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
        }
        async Task DownloadFile(string url, string fileName)
        {
            fileName = Program.WaitForDownloadDirectory +"\\" +fileName;
            long totalBytesToDownload = 0;
            long currentBytesDownloaded = 0;
            long lastBytesDownloaded = 0;
            DateTime lastUpdate = DateTime.Now;
            bool imVideoDownload = false;
            if (fileName.Contains(".mp4") || fileName.Contains(".webm"))
                imVideoDownload = true;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                totalBytesToDownload = response.ContentLength;
            }

            using (var client = new WebClient())
            {
                if (File.Exists(fileName))
                {
                    currentBytesDownloaded = new FileInfo(fileName).Length;
                }
                if (totalBytesToDownload <= currentBytesDownloaded)
                    return;
                // Устанавливаем заголовки для запроса
                client.Headers.Add(HttpRequestHeader.Range, $"bytes={currentBytesDownloaded}-");
                if (imVideoDownload)
                {
                    VideoFileSize = FormatFileSize(totalBytesToDownload);
                    VideoFileDownloadedSize = FormatFileSize(currentBytesDownloaded);
                }
                else
                {
                    AudioFileSize = FormatFileSize(totalBytesToDownload);
                    AudioFileDownloadedSize = FormatFileSize(currentBytesDownloaded);
                }

                // Загружаем файл
                using (var stream = await client.OpenReadTaskAsync(url))
                {
                    using (var fileStream = new FileStream(fileName, FileMode.Append, FileAccess.Write))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            currentBytesDownloaded += bytesRead;
                            // Обновляем информацию о загрузке каждые 500 мс
                            if ((DateTime.Now - lastUpdate).TotalMilliseconds > 500)
                            {
                                if (imVideoDownload)
                                {
                                    VideoName = fileName;
                                    DownloadVideoSpeedStr = FormatFileSize((currentBytesDownloaded - lastBytesDownloaded) / (DateTime.Now - lastUpdate).TotalSeconds);
                                    VideoDownloadedPercent = (currentBytesDownloaded * 100 / totalBytesToDownload) + "%";
                                }
                                else
                                {
                                    AudioName = fileName;
                                    DownloadAudioSpeedStr = FormatFileSize((currentBytesDownloaded - lastBytesDownloaded) / (DateTime.Now - lastUpdate).TotalSeconds);
                                    AudioDownloadedPercent = (currentBytesDownloaded * 100 / totalBytesToDownload) + "%";
                                }
                                lastBytesDownloaded = currentBytesDownloaded;
                                lastUpdate = DateTime.Now;
                            }
                        }
                    }
                }
                if (imVideoDownload)
                {
                    DownloadVideoSpeedStr = "Файл загружен";
                    VideoDownloadedPercent = "Ожидание загрузки файла звука";
                }
                else 
                {
                    DownloadAudioSpeedStr = "Файл загружен";
                    VideoDownloadedPercent = "Ожидание загрузки файла видео";
                }
            }
        }
        string NormalizeName(string name, bool imAudio)
        {
            if (imAudio)
                name += ".aac";
            return name.Normalize().Replace((char)8211, (char)45);
        }
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                bytes /= 1024;
                order++;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }

        private string FormatFileSize(double bytesPerSecond)
        {
            string[] sizes = { "Б/с", "КБ/с", "МБ/с", "ГБ/с", "ТБ/с" };
            int order = 0;
            while (bytesPerSecond >= 1024 && order < sizes.Length - 1)
            {
                bytesPerSecond /= 1024;
                order++;
            }
            return $"{bytesPerSecond:0.##} {sizes[order]}";
        }
    }
}
