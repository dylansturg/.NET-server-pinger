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

            var pingerProgram = new Program()
            {
                RequestUris = requestUris,
            };

            var programTask = pingerProgram.StartPinging();
        }
    }

    public class ExecutionWatcher
    {
        private static String GeneralReportFormat = "Status Report: {0} total requests worth {1} points completed in {3:0.000} seconds.";
        public Program WatchedProgram { get; set; }


        public async void StartPeriodicProgressReport(TimeSpan reportDelay, CancellationToken stopToken)
        {
            while (!stopToken.IsCancellationRequested)
            {
                Thread.Sleep(reportDelay);

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

                
            }


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
