using System;
using System.Threading;

namespace SystemProgramming_4
{
    internal class Program
    {
        static Semaphore sem = new Semaphore(3, 3);
        static CancellationTokenSource cancelSrc = new CancellationTokenSource();
        static CancellationToken token;
        static object locker = new object();

        class FileData
        {
            public string? Code;
            public string? FileName;
            public double Size;
            public string Status = "Waiting";
            public int Percent = 0;
            public double LoadedSize = 0;
        }

        static FileData[] fileList = new FileData[]
        {
            new FileData { Code = "#1", FileName = "mp4_sample_file_25MB.mp4", Size = 25.0 },
            new FileData { Code = "#2", FileName = "mp4_sample_file_50MB.mp4", Size = 50.0 },
            new FileData { Code = "#3", FileName = "mp4_sample_file_100MB.mp4", Size = 99.2 },
            new FileData { Code = "#4", FileName = "mp4_sample_file_200MB.mp4", Size = 200.0 }
        };

        static int activeTasks = 0;
        static int waitingTasks = 0;
        static int doneTasks = 0;

        static void Main(string[] args)
        {
            token = cancelSrc.Token;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            foreach (var item in fileList)
            {
                ThreadPool.QueueUserWorkItem(StartDownload, item);
            }

            Thread displayThread = new Thread(() =>
            {
                while (!token.IsCancellationRequested && doneTasks < fileList.Length)
                {
                    PrintUI();
                    Thread.Sleep(100);
                }
                PrintUI();
            });
            displayThread.Start();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        cancelSrc.Cancel();
                        break;
                    }
                }

                if (doneTasks == fileList.Length)
                {
                    Thread.Sleep(1000);
                    break;
                }

                Thread.Sleep(50);
            }

            Console.WriteLine("\nButun emeliyyatlar bitdi. Cixis ucun duymeye basin.");
            Console.ReadLine();
        }

        static void StartDownload(object obj)
        {
            var item = (FileData)obj;

            try
            {
                token.ThrowIfCancellationRequested();

                lock (locker)
                {
                    waitingTasks++;
                }

                sem.WaitOne();

                if (token.IsCancellationRequested) return;

                lock (locker)
                {
                    waitingTasks--;
                    activeTasks++;
                    item.Status = "Downloading";
                }

                int steps = 20;
                for (int i = 1; i <= steps; i++)
                {
                    if (token.IsCancellationRequested) return;
                    Thread.Sleep(200);

                    lock (locker)
                    {
                        item.Percent = (i * 100) / steps;
                        item.LoadedSize = Math.Round((item.Size * item.Percent) / 100.0, 1);
                    }
                }

                lock (locker)
                {
                    activeTasks--;
                    doneTasks++;
                    item.Status = "Completed";
                    item.Percent = 100;
                    item.LoadedSize = item.Size;
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                try { sem.Release(); } catch { }
            }
        }

        static void PrintUI()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  MULTI-THREAD VIDEO DOWNLOADER                      ║");
            Console.WriteLine("║                  Semaphore: MAXIMUM 3                               ║");
            Console.WriteLine("╚═════════════════════════════════════════════════════════════════════╝");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  1-3  → Downloading");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  4+   → Waiting");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ESC  → Cancel ALL");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘\n");

            lock (locker)
            {
                foreach (var item in fileList)
                {
                    int totalBars = 20;
                    int filled = (item.Percent * totalBars) / 100;
                    string bar = new string('█', filled) + new string('░', totalBars - filled);

                    if (item.Status == "Downloading")
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"{item.Code,-4} Downloading   [{bar}]");
                        Console.ResetColor();
                        Console.WriteLine($"   {item.Percent,3}%  {item.LoadedSize:0.1} MB/{item.Size:0.1} MB");
                    }
                    else if (item.Status == "Waiting")
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"{item.Code,-4} Waiting       [{bar}]");
                        Console.ResetColor();
                        Console.WriteLine($"   {item.Percent,3}%");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"{item.Code,-4} Completed   [{bar}]");
                        Console.ResetColor();
                        Console.WriteLine($"   100%  {item.Size:0.1} MB/{item.Size:0.1} MB");
                    }

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"     {item.FileName}");
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }

            Console.WriteLine("───────────────────────────────────────────────────────────────────────");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Aktiv: {activeTasks} | Gozleyen: {waitingTasks} | Tamamlanan: {doneTasks}");
            Console.ResetColor();
        }
    }
}