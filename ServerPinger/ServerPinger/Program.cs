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

            var requestUris = requests.Select(request => new Uri(request)).ToList();

            var pingerProgram = new Program()
            {
                RequestUris = requestUris,
            };


        }
    }

    public class Program : IFailureWatcher
    {
        public static Program Instance
        {
            get { return _instance; }
        }
        private static readonly Program _instance = new Program();

        private CancellationTokenSource TokenSource { get; set; }
        public List<Uri> RequestUris { get; set; }
        private RequestLogger Logger { get; set; }

        public Program()
        {
            TokenSource = new CancellationTokenSource();
        }

        public async Task StartPinging()
        {
            Logger = new RequestLogger();
            Logger.AddFailureWatch(this);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var pingTasks =
                RequestUris.Select(uri => new ServerPinger(uri, TimeSpan.Zero, Logger).BeginPinging(TokenSource.Token));
            // Tasks are running... now we just wait...
            var completeTask = Task.WhenAll(pingTasks);

            await completeTask;

            stopwatch.Stop();
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
