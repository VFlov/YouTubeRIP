using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;

namespace YouTubeRIP
{
    public class Worker
    {
        int Id { get; set;}
        string Url { get; set; }
        readonly string ResultDirectoryName = "Downloaded&Merged";
        static object lockObject = new object();
        public Worker(int id, string url) 
        {
            Id = id;
            Url = url;
        }
        public void Awake()
        {
            var videoInfos = YouTube.Default.GetAllVideos(Url);
            Task<string> videoName = Task.Run(() => VideoDownload(videoInfos));
            Task<string> audioName = Task.Run(() => AudioDownload(videoInfos));
            Task.WaitAll(new Task[] { videoName, audioName});
            Merger(videoName.Result,audioName.Result);
        }
        string VideoDownload(IEnumerable<YouTubeVideo> youTubeVideos)
        {
            var maxResolution = youTubeVideos.First(i => i.Resolution == youTubeVideos.Max(j => j.Resolution));
            string normalizeName = NormalizeName(maxResolution.FullName, false);
            Task.WaitAll(Task.Run(() => DownloadFile(maxResolution.Uri, normalizeName)));
            return normalizeName;
        }
        public string VideoDownload(string url)
        {
            var youTubeVideos = YouTube.Default.GetAllVideos(Url);
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
        public string AudioDownload(string url)
        {
            var youTubeVideos = YouTube.Default.GetAllVideos(Url);
            var audioFormat = youTubeVideos.Where(i => i.AudioFormat == AudioFormat.Aac);
            var bitrate = audioFormat.First(i => i.AudioBitrate == audioFormat.Max(j => j.AudioBitrate));
            string normalizeName = NormalizeName(bitrate.FullName, true);
            Task.WaitAll(Task.Run(() => DownloadFile(bitrate.Uri, normalizeName)));
            return normalizeName;
        }
        public void Merger(string videoName, string audioName)
        {
            string ffmpegCommand = $"-i \"{Directory.GetCurrentDirectory()}\\{videoName}\" " +
                $"-i \"{Directory.GetCurrentDirectory()}\\{audioName}\" " +
                $"-c copy \"{Directory.GetCurrentDirectory()}\\{ResultDirectoryName}\\{videoName.Replace(".webm", ".mp4")}\"";
            string ffmpegPath = Directory.GetCurrentDirectory() + "\\ffmpeg.exe";
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
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
        }
        async Task DownloadFile2(string url, string fileName)
        {
            Console.Clear();
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                bool imVideoDownload = false;
                long fileSizeBytes = response.ContentLength;
                if (fileName.Contains(".mp4") || fileName.Contains(".webm"))
                {
                    imVideoDownload = true;
                }
                using (WebClient client = new WebClient())
                {
                    long downloadedBytes = 0;
                    int progressPercentage = 0;
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        progressPercentage = e.ProgressPercentage;
                        downloadedBytes = e.BytesReceived;
                    };
                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        sw.Stop();
                    };

                    client.DownloadFileAsync(new Uri(url), fileName);
                    while (downloadedBytes < fileSizeBytes)
                    {
                        Thread.Sleep(10);
                        DisplayProgress(new DownloadInfo(downloadedBytes, fileSizeBytes, progressPercentage, imVideoDownload, fileName, sw.Elapsed.TotalSeconds));
                    }
                }

            }
        }
        async Task DownloadFile(string url, string fileName)
        {
            long totalBytesToDownload = 0;
            long currentBytesDownloaded = 0;
            long lastBytesDownloaded = 0;
            DateTime lastUpdate = DateTime.Now;
            bool imVideoDownload = false;
            if (fileName.Contains(".mp4") || fileName.Contains(".webm"))
            {
                imVideoDownload = true;
            }
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
                                // Вычисляем скорость загрузки
                                double downloadSpeed = (currentBytesDownloaded - lastBytesDownloaded) / (DateTime.Now - lastUpdate).TotalSeconds;
                                // Форматируем скорость загрузки
                                string speedStr = FormatFileSize(downloadSpeed);

                                // Выводим информацию о загрузке
                                //Console.WriteLine($"Загружено: {FormatFileSize(currentBytesDownloaded)} / {FormatFileSize(totalBytesToDownload)} ({(currentBytesDownloaded * 100 / totalBytesToDownload):0.##}%) ({speedStr})");
                                DisplayProgress(new DownloadInfo(currentBytesDownloaded, totalBytesToDownload, currentBytesDownloaded * 100 / totalBytesToDownload, imVideoDownload,fileName, downloadSpeed));
                                // Обновляем последние данные
                                lastBytesDownloaded = currentBytesDownloaded;
                                lastUpdate = DateTime.Now;
                            }
                        }
                    }
                }
            }
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
        void DisplayProgress(object obj)
        {
            DownloadInfo downloadInfo = (DownloadInfo)obj;
            Console.SetCursorPosition(100, 0);
            lock (lockObject)
            {
                Console.SetCursorPosition(0, Id * 5);
                Console.WriteLine("========================================");
                if (downloadInfo.ImVideoDownload)
                {
                    Console.SetCursorPosition(0, Id * 5 + 1);
                    Console.WriteLine("Загрузка файла видео " + downloadInfo.FileName);
                    Console.WriteLine($"Статус файла: " +
                    $"{downloadInfo.FileSizeBytes / 1024 / 1024}/{downloadInfo.DownloadedBytes / 1024 / 1024}" +
                    $"МБайт ({downloadInfo.ProgressPercentage}%) " + $"Скорость: " +
                    $"{(downloadInfo.DownloadSpeed / 1024 / 1024).ToString("0.00")} " +
                    $"Мбит\\с");

                }
                else
                {
                    Console.SetCursorPosition(0, Id * 5 + 3);
                    Console.WriteLine("Загрузка файла звука " + downloadInfo.FileName);
                    Console.WriteLine($"Статус файла: " +
                    $"{downloadInfo.FileSizeBytes / 1024 / 1024}/{downloadInfo.DownloadedBytes / 1024 / 1024}" +
                    $"МБайт ({downloadInfo.ProgressPercentage}%) " + $"Скорость: " +
                    $"{(downloadInfo.DownloadSpeed / 1024 / 1024).ToString("0.00")} " +
                    $"Мбит\\с");
                }
            }
        
        }
        string NormalizeName(string name, bool imAudio) 
        {
            if (imAudio)
                name += ".aac";
            return name.Normalize().Replace((char)8211, (char)45);
        }
        record class DownloadInfo(long DownloadedBytes, long FileSizeBytes,
            long ProgressPercentage, bool ImVideoDownload, string FileName, double DownloadSpeed);
    }
}
