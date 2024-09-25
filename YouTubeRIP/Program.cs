using System.Threading;

namespace YouTubeRIP
{
    class Program()
    {
        static void Main()
        {
            Console.SetWindowSize(100, 30);
            Console.CursorVisible = false;
            Console.WriteLine("1 - Начать работу\n2 - Скачать файл видео" +
                "\n3 - Скачать файл звука\n4 - Обьединить звук с видео");
            switch (Console.ReadLine())
            {
                case "1":
                    {
                        Preparation();
                        break;
                    }
                case "2":
                    {
                        Console.WriteLine("Введите ссылку на видео");
                        string str = Console.ReadLine();
                        FileDownload(str, true);
                        break;
                    }
                case "3":
                    {
                        Console.WriteLine("Введите ссылку на видео");
                        string str = Console.ReadLine();
                        FileDownload(str, false);
                        break;
                    }
                case "4":
                    {
                        Console.WriteLine("Введите название файла видео");
                        string video = Console.ReadLine();
                        Console.WriteLine("Введите название файла звука");
                        string audio = Console.ReadLine();
                        MergerFiles(video, audio);
                        break;
                    }
            }
        }
        static async void Preparation()
        {
            if (FirstStart())
            {
                Console.WriteLine("Откройте приложение заново");
                Task.Delay(1000).Wait();
                Environment.Exit(0);
            }
            string[] urls = File.ReadAllLines("Urls.txt");
            if (urls.Length == 0)
                throw new ArgumentException("Urls.txt файл пуст");
            Queue<string> queue = new Queue<string>(urls.Length);
            foreach (var url in urls)
                queue.Enqueue(url);
            Console.WriteLine("Введите количество одновременно загружаемых файлов\n" +
                "Где при 100Мбит скорости интернета статус загрузки сети:\n4 - 90%\n5 - 100%");
            //string str = Console.ReadLine();
            if (!int.TryParse(Console.ReadLine(), out int numOfTasks))
            {
                throw new ArgumentException("Введено не целочисленное значение");
            }
            Task.Run(() => { while (true) { Console.Clear(); Thread.Sleep(10000); } });
            Task[] tasks = new Task[queue.Count()];
            SemaphoreSlim semaphore = new SemaphoreSlim(numOfTasks);
            while (queue.Count() != 0)
            {
                for (int i = 0; i < queue.Count; i++)
                {
                    int localI = i;
                    if (localI >= numOfTasks)
                        localI %= numOfTasks;
                    tasks[i] = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(); // Ожидание свободного места в семафоре
                        try
                        {
                            new Worker(localI, queue.Dequeue()).Awake();
                        }
                        finally
                        {
                            semaphore.Release(); // Освобождение места в семафоре
                        }
                    });
                }

                Task.WaitAll(tasks);
            }

            /*
            if (int.TryParse(str, out int numOfTasks))
            {
                Task.Run(() => { while (true) { Console.Clear(); Thread.Sleep(10000); } });
                Task[] tasks = new Task[numOfTasks];
                while (queue.Count != 0)
                {
                    for (int i = 0; i < Math.Min(numOfTasks, queue.Count); i++)
                    {
                        int localI = i;
                        tasks[i] = Task.Run(() => new Worker(localI, queue.Dequeue()).Awake());
                    }
                    Task.WaitAll(tasks);
                }
                //Для очистки консоли от артефактов
                Task.WaitAll(tasks);
                TheEndOfEvangelion();
            }
            else
                throw new ArgumentException("Введено не целочисленное значение");
            */


        }
        static void FileDownload(string url, bool thisVideo)
        {
            if (url == null)
                throw new ArgumentNullException("Строка пустая");
            Worker worker = new(0, url);
            string fileName = "";
            if (thisVideo)
                fileName = worker.VideoDownload(url);
            else
                fileName = worker.AudioDownload(url);
            Console.WriteLine("Файл " + fileName + " загружен");
        }
        static void MergerFiles(string video, string audio)
        {
            Worker worker = new(0, "");
            if (video == null || audio == null)
                throw new ArgumentNullException("Строка пустая");
            worker.Merger(video, audio);
            Console.WriteLine("Файл " + video + "готов");
        }
        static bool FirstStart()
        {
            bool thisFirstStart = false;
            if (!File.Exists("Urls.txt"))
            {
                thisFirstStart = true;
                File.Create("Urls.txt");
            }
            if (!Directory.Exists(ResultDirectoryName))
            {
                thisFirstStart = true;
                Directory.CreateDirectory(ResultDirectoryName);
            }
            return thisFirstStart;
        }
        static void TheEndOfEvangelion()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < 100; i++)
                Console.WriteLine("==> Программа завершила работу. Ссылки закончились <==");
        }
        public static readonly string ResultDirectoryName = "Downloaded&Merged";
    }
}