using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerPinger
{
    public class ServerPinger
    {
        public Uri RequestUri { get; set; }
        public TimeSpan TimeDelay { get; set; }
        public RequestLogger Log { get; set; }

        public ServerPinger()
        {   
        }

        public ServerPinger(Uri request, TimeSpan delay, RequestLogger log)
        {
            RequestUri = request;
            TimeDelay = delay;
            Log = log;
        }

        public async Task BeginPinging(CancellationToken cancelToken)
        {
            if (RequestUri == null)
            {
                throw new ArgumentNullException("RequestUri");
            }

            while (!cancelToken.IsCancellationRequested)
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(RequestUri, cancelToken);
                    if (Log == null) continue;

                    if (response.IsSuccessStatusCode)
                    {
                        Log.LogSuccessfulRequest(RequestUri);
                    }
                    else
                    {
                        Log.LogSuccessfulRequest(RequestUri);
                    }
                }

                if (TimeDelay > TimeSpan.Zero)
                {
                    Thread.Sleep(TimeDelay);
                }
            }

            // Goes until cancelled, then returns
        }

    }
}
