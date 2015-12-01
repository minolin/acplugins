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
        private LiveDataDumpClient LiveDataServer { get; set; } = new LiveDataDumpClient(new BasicHttpBinding(), new EndpointAddress("http://plugin.minorating.com:805/minorating/12"));
        AcServerPluginManager Log { get; set; }
        public bool Stop { get; set; }
        private int port;

        private object lockobj = new object();

        private Dictionary<string, string> Cache = null;

        public LocalAuthCache(int httpPort, AcServerPluginManager log)
        {
            this.port = httpPort;
            this.Log = log;
        }


        internal void Run()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    
                    while (!Stop)
                    {
                        var result = LiveDataServer.GetAuthData("token", DateTime.Now, LastAuthUpdate);
                        lock (lockobj)
                        {
                            // Now we need to merge the new information
                            Merge(result);
                        }

                        Thread.Sleep(1000 * 60 * 60); // One request per hours should be sufficient
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
            // TODO
        }

        private string DoAuth(string targetgrade, string steamIdRequested)
        {
            if (targetgrade.Contains("A") && !targetgrade.Contains("B"))
                targetgrade = targetgrade.Replace("A", "AB");
            else if (targetgrade.Contains("B") && !targetgrade.Contains("A"))
                targetgrade = targetgrade.Replace("B", "AB");

            var hash = Hash(steamIdRequested);
            string grade = "N";

            Cache.TryGetValue(hash, out grade);

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
