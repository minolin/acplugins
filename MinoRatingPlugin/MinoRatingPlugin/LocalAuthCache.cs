using acPlugins4net;
using MinoRatingPlugin.minoRatingServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinoRatingPlugin
{
    public class LocalAuthCache
    {
        public DateTime LastAuthUpdate { get; set; } = new DateTime(2015, 01, 01);
        const int MaxMessageSize = 65535 * 100;
        private LiveDataDumpClient LiveDataServer { get; set; }
        AcServerPluginManager Log { get; set; }
        public bool Stop { get; set; }
        private int port;

        private object lockobj = new object();

        private Dictionary<string, string> Cache = null;

        public LocalAuthCache(int httpPort, AcServerPluginManager log)
        {
            this.port = httpPort;
            this.Log = log;

            LiveDataServer = new LiveDataDumpClient(
                new BasicHttpBinding()
                {
                    MaxReceivedMessageSize = MaxMessageSize,
                    ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas() { MaxStringContentLength = MaxMessageSize }
                }, new EndpointAddress("http://plugin.minorating.com:805/minorating/12"));
        }


        internal void Run()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {

                    while (!Stop)
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();

                        var dtBeforeUpd = DateTime.Now;
                        var result = LiveDataServer.GetAuthData("token", DateTime.Now, LastAuthUpdate);
                        LastAuthUpdate = dtBeforeUpd;

                        var timeGetAuthData = sw.Elapsed;
                        sw.Restart();
                        
                        lock (lockobj)
                        {
                            // Now we need to merge the new information
                            Merge(result);
                        }

                        if (result.Count > 0)
                            Log.Log("New Auth data: " + result.Count + ", downloaded in " + timeGetAuthData + ", local merge in " + sw.Elapsed);

                        Thread.Sleep(1000 * 60 * 15); // One request per hours should be sufficient
                    }
                }
                catch (Exception ex)
                {
                    Log.Log("Error in local Auth module (GetData)");
                    Log.Log(ex);
                    Stop = true;
                }
            });


            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    if (!HttpListener.IsSupported)
                    {
                        Log.Log("Error: Http listener (for the local auth) is not supported by this operating system");
                        return;
                    }

                    HttpListener listener = new HttpListener();
                    var prefix = "http://localhost:" + port + "/";
                    listener.Prefixes.Add(prefix);
                    listener.Start();
                    Console.WriteLine("Auth now listening on " + prefix);

                    while (!Stop)
                    {
                        HttpListenerContext context = listener.GetContext();
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        if(string.IsNullOrEmpty(request.QueryString["ALLOWED"]))
                        {
                            Log.Log("Auth error: No ALLOWED parameter configured!");
                            continue;
                        }

                        if (string.IsNullOrEmpty(request.QueryString["GUID"]))
                        {
                            Log.Log("Auth error: No GUID parameter configured!");
                            continue;
                        }

                        var gradesAllowed = request.QueryString["ALLOWED"];
                        var steamIdRequested = request.QueryString["GUID"];

                        string responseString = DoAuth(gradesAllowed, steamIdRequested);
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        // Get a response stream and write the response to it.
                        response.ContentLength64 = buffer.Length;
                        using (System.IO.Stream output = response.OutputStream)
                        {
                            output.Write(buffer, 0, buffer.Length);
                        }

                    }

                    listener.Stop();
                }
                catch (Exception ex)
                {
                    Log.Log("Error in local Auth module");
                    Log.Log(ex);
                    Stop = true;
                }
            });
        }

        private void Merge(Dictionary<string, string> delta)
        {
            // Easy part: Cache is empty:
            lock (lockobj)
            {
                if (Cache == null)
                    Cache = delta;
                else
                {
                    foreach (var d in delta)
                    {
                        if (Cache.ContainsKey(d.Key))
                            Cache[d.Key] = d.Value;
                        else
                            Cache.Add(d.Key, d.Value);
                    }
                }
            }
        }

        public string DoAuth(string targetgrade, string steamIdRequested)
        {
            if (targetgrade.Contains("A") && !targetgrade.Contains("B"))
                targetgrade = targetgrade.Replace("A", "AB");
            else if (targetgrade.Contains("B") && !targetgrade.Contains("A"))
                targetgrade = targetgrade.Replace("B", "AB");

            var hash = Hash(steamIdRequested);
            string grade = "N";

            Cache.TryGetValue(hash, out grade);

            // Special case: Grade X would mean this was a SHA1-Collision, so we have to request that one live
            if(grade == "X")
            {
                var request = WebRequest.Create("http://plugin.minorating.com:805/minodata/auth/" + targetgrade + "/?GUID=" + steamIdRequested);
                using (var httpWebResponse = request.GetResponse() as HttpWebResponse)
                {
                    if (httpWebResponse != null)
                    {
                        using (var streamReader = new System.IO.StreamReader(httpWebResponse.GetResponseStream()))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }

                return "DENY|Internal AUTH server problem, please report this to the server admins";
            }

            if (targetgrade.Contains(grade))
                return "OK|Welcome!";

            return "DENY|You need a www.minorating.com Grade of " + targetgrade + " to join (you are " + grade + ")";
        }

        public static string Hash(string driverguid)
        {
            System.Security.Cryptography.SHA1 sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            byte[] textToHash = Encoding.Default.GetBytes(driverguid);
            byte[] result = sha1.ComputeHash(textToHash);

            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in result)
            {
                s.Append(b.ToString("x2").ToLower());
            }

            return s.ToString();
        }
    }
}
