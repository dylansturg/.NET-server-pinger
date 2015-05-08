using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerPinger
{

    class ProgramRunner
    {
        static void Main(string[] args)
        {
            var requests = new List<string>();

            Console.WriteLine("Please enter the URLs you'd like to request, one per line, enter a blank line to stop:");
            var url = Console.ReadLine();
            while (!String.IsNullOrWhiteSpace(url))
            {
                requests.Add(url);
                url = Console.ReadLine();
            }

            var requestUris = requests.Select(request =>
            {
                var uri = request;
                var weight = 1;
                var parts = request.Split(' ');
                if (parts.Length > 1)
                {
                    uri = parts[0];
                    weight = int.Parse(parts[1]);
                }

                return new WeightedRequest
                {
                    Uri = new Uri(uri),
                    Weight = weight,
                };

            }).ToList();

            var programDoneToken = new CancellationTokenSource();
            var pingerProgram = new Program()
            {
                RequestUris = requestUris,
            };

            var programStatusMonitor = new ExecutionWatcher
            {
                WatchedProgram = pingerProgram
            };
            programStatusMonitor.StartPeriodicProgressReport(TimeSpan.FromSeconds(10), programDoneToken.Token);

            var programTask = pingerProgram.StartPinging();

            string shouldQuit;
            do
            {
                shouldQuit = Console.ReadLine();
            } while (!String.IsNullOrWhiteSpace(shouldQuit) && shouldQuit.Equals("quit", StringComparison.InvariantCultureIgnoreCase));

            programDoneToken.Cancel();
            pingerProgram.RequestStop();

            programTask.Wait();

            programStatusMonitor.PrintStatusReport();
        }
    }

    public class ExecutionWatcher
    {
        private const String GeneralReportFormat = "Status Report: {0} total requests worth {1} points completed in {2:0.000} seconds.";
        private const String ThroughputReportFormat = "Server Throughput: {0:0.00000} per second";
        public Program WatchedProgram { get; set; }

        public void PrintStatusReport()
        {
            var report = WatchedProgram.SuccessHistogram();
            var currentElapsed = WatchedProgram.ElapsedTime();

            var totalWeight = 0;
            var totalCount = 0;
            foreach (var record in report)
            {
                totalWeight += record.Key.Weight;
                totalCount++;
            }

            var elapsedSeconds = currentElapsed.TotalSeconds;

            var statusReport = String.Format(GeneralReportFormat, totalCount, totalWeight, elapsedSeconds);
            var throughput = totalWeight / elapsedSeconds;
            var throughputReport = String.Format(ThroughputReportFormat, throughput);

            Console.WriteLine(statusReport);
            Console.WriteLine(throughputReport);
        }

        public void StartPeriodicProgressReport(TimeSpan reportDelay, CancellationToken stopToken)
        {
            var reportThread = new Thread(delegate()
            {
                while (!stopToken.IsCancellationRequested)
                {
                    Thread.Sleep(reportDelay);
                    PrintStatusReport();                   
                }
            });

            reportThread.Start();
        }
    }

    public class Program : IFailureWatcher
    {
        private CancellationTokenSource TokenSource { get; set; }
        public List<WeightedRequest> RequestUris { get; set; }
        private RequestLogger Logger { get; set; }
        private Stopwatch Timer { get; set; }

        public Program()
        {
            TokenSource = new CancellationTokenSource();
        }

        public Dictionary<WeightedRequest, int> SuccessHistogram()
        {
            var histogram = Logger.SuccessHistogram;

            var weightedHistogram = new Dictionary<WeightedRequest, int>();
            foreach (var entry in histogram)
            {
                var weightedRequest = RequestUris.Find(uri => uri.Uri == entry.Key);
                weightedHistogram.Add(weightedRequest, entry.Value);
            }

            return weightedHistogram;
        }

        public TimeSpan ElapsedTime()
        {
            return Timer.Elapsed;
        }

        public async Task StartPinging()
        {
            Logger = new RequestLogger();
            Logger.AddFailureWatch(this);

            Timer = new Stopwatch();
            Timer.Start();

            var pingTasks =
                RequestUris.Select(uri => new ServerPinger(uri.Uri, TimeSpan.Zero, Logger).BeginPinging(TokenSource.Token));
            // Tasks are running... now we just wait...
            var completeTask = Task.WhenAll(pingTasks);

            await completeTask;

            Timer.Stop();
        }

        public void RequestStop()
        {
            TokenSource.Cancel();
        }

        public void RequestFailureHasOccurred(Uri failedRequestUri)
        {
           TokenSource.Cancel();
        }
    }
}
