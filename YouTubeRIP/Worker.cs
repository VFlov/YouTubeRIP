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
            DownloadFile(maxResolution.Uri, normalizeName);
            return normalizeName;
        }
        public string VideoDownload(string url)
        {
            var youTubeVideos = YouTube.Default.GetAllVideos(Url);
            var maxResolution = youTubeVideos.First(i => i.Resolution == youTubeVideos.Max(j => j.Resolution));
            string normalizeName = NormalizeName(maxResolution.FullName, false);
            DownloadFile(maxResolution.Uri, normalizeName);
            return normalizeName;
        }
        string AudioDownload(IEnumerable<YouTubeVideo> youTubeVideos)
        {
            var audioFormat = youTubeVideos.Where(i => i.AudioFormat == AudioFormat.Aac);
            var bitrate = audioFormat.First(i => i.AudioBitrate == audioFormat.Max(j => j.AudioBitrate));
            string normalizeName = NormalizeName(bitrate.FullName, true);
            DownloadFile(bitrate.Uri, normalizeName);
            return normalizeName;
        }
        public string AudioDownload(string url)
        {
            var youTubeVideos = YouTube.Default.GetAllVideos(Url);
            var audioFormat = youTubeVideos.Where(i => i.AudioFormat == AudioFormat.Aac);
            var bitrate = audioFormat.First(i => i.AudioBitrate == audioFormat.Max(j => j.AudioBitrate));
            string normalizeName = NormalizeName(bitrate.FullName, true);
            DownloadFile(bitrate.Uri, normalizeName);
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
        void DownloadFile(string url, string fileName)
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
                    
                    //TimerCallback timer = new TimerCallback(DisplayProgress);
                    //Timer tm = new Timer(timer, 
                    //    new DownloadInfo(downloadedBytes,fileSizeBytes,progressPercentage, imVideoDownload, fileName, sw.Elapsed.TotalSeconds), 
                    //    0, 1000);
                    client.DownloadFileAsync(new Uri(url), fileName);
                    while (downloadedBytes < fileSizeBytes)
                    {
                        Thread.Sleep(100);
                        DisplayProgress(new DownloadInfo(downloadedBytes, fileSizeBytes, progressPercentage, imVideoDownload, fileName, sw.Elapsed.TotalSeconds));
                    }
                }

            }
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
                    $"МБайт " + $"Скорость: " +
                    $"{(downloadInfo.DownloadedBytes / downloadInfo.SecondsPassed / 1024 / 1024).ToString("0.00")} " +
                    $"Мбит\\с | " +
                    $"Статус загрузки: {downloadInfo.ProgressPercentage}% " +
                        $"{((downloadInfo.FileSizeBytes / (downloadInfo.DownloadedBytes / downloadInfo.SecondsPassed)) / 60).ToString("0.00")} " +
                        $"Минут осталось");

                }
                else
                {
                    Console.SetCursorPosition(0, Id * 5 + 3);
                    Console.WriteLine("Загрузка файла звука " + downloadInfo.FileName);
                    Console.WriteLine($"Статус файла: " +
                    $"{downloadInfo.FileSizeBytes / 1024 / 1024}/{downloadInfo.DownloadedBytes / 1024 / 1024}" +
                    $"МБайт " + $"Скорость: " +
                    $"{(downloadInfo.DownloadedBytes / downloadInfo.SecondsPassed / 1024 / 1024).ToString("0.00")} " +
                    $"Мбит\\с" +
                    $"Статус загрузки: {downloadInfo.ProgressPercentage}% " +
                        $"{((downloadInfo.FileSizeBytes / (downloadInfo.DownloadedBytes / downloadInfo.SecondsPassed)) / 60).ToString("0.00")} " +
                        $"Минут осталось");
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
            int ProgressPercentage, bool ImVideoDownload, string FileName, double SecondsPassed);
    }
}
