using YoutubeExtractor;

string url = "https://www.youtube.com/watch?v=gK8m-VPBs80";
IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url);
VideoInfo video = videoInfos
    .First(info => info.Resolution == 720);
    
/*
 * If the video has a decrypted signature, decipher it
 */
if (video.RequiresDecryption)
{
    DownloadUrlResolver.DecryptDownloadUrl(video);
}

/*
 * Create the video downloader.
 * The first argument is the video to download.
 * The second argument is the path to save the video file.
 */
var videoDownloader = new VideoDownloader(video, "C:\\Users\\VSP\\Documents\\GitHub\\YouTubeRIP\\ForTests\\bin\\Debug\\net8.0" + "\\" + video.Title + video.VideoExtension);

// Register the ProgressChanged event and print the current progress
videoDownloader.DownloadProgressChanged += (sender, args) => Console.WriteLine(args.ProgressPercentage);

/*
 * Execute the video downloader.
 * For GUI applications note, that this method runs synchronously.
 */
videoDownloader.Execute();
Console.ReadLine();