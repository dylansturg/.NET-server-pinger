using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ServerPinger
{
    public class RequestLogger
    {
        List<IFailureWatcher> FailureWatchers { get; set; }

        public Dictionary<Uri, int> SuccessHistogram
        {
            get { return _histogram; }
        }

        private readonly Dictionary<Uri, int> _histogram = new Dictionary<Uri, int>();

        public RequestLogger()
        {
            FailureWatchers = new List<IFailureWatcher>();
        }

        public void AddFailureWatch(IFailureWatcher watcher)
        {
            FailureWatchers.Add(watcher);
        }

        public async void LogSuccessfulRequest(Uri requestedUri)
        {
            if (!SuccessHistogram.ContainsKey(requestedUri))
            {
                SuccessHistogram.Add(requestedUri, 0);
            }

            SuccessHistogram[requestedUri] = SuccessHistogram[requestedUri] + 1;
        }

        public async void LogFailureRequest(Uri requestedUri)
        {
            NonBlockingConsole.WriteLine("Received a SERVER FAILURE accessing resource: " + requestedUri);
            foreach (var watcher in FailureWatchers)
            {
                watcher.RequestFailureHasOccurred(requestedUri);
            }
        }
    }

    static class NonBlockingConsole
    {
        private static readonly BlockingCollection<string> ConsoleQueue = new BlockingCollection<string>();

        static NonBlockingConsole()
        {
            var thread = new Thread(
                () => { while (true) Console.WriteLine(ConsoleQueue.Take()); }) { IsBackground = true };
            thread.Start();
        }

        public static void WriteLine(string value)
        {
            ConsoleQueue.Add(value);
        }
    }
}
