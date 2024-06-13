using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private static CloudBlobContainer GetKontener(string nazwaKontenera)
        {
            var konto = CloudStorageAccount.DevelopmentStorageAccount;
            CloudBlobClient klient = konto.CreateCloudBlobClient();
            CloudBlobContainer kontener = klient.GetContainerReference(nazwaKontenera);
            kontener.CreateIfNotExists();
            return kontener;
        }

        private static CloudQueue GetKolejka(string nazwaKolejki)
        {
            var konto = CloudStorageAccount.DevelopmentStorageAccount;
            CloudQueueClient klient = konto.CreateCloudQueueClient();
            CloudQueue kolejka = klient.GetQueueReference(nazwaKolejki);
            kolejka.CreateIfNotExists();
            return kolejka;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Dziala");
                await Task.Delay(1000);
            }
        }

        private static string ROT13(string tresc)
        {
            return new string(tresc.ToCharArray().Select(s =>
            {
                if(s >= 'a' && s <= 'z')
                {
                    if(s > 'm')
                    {
                        return (char)(s - 13);
                    }
                    else
                    {
                        return (char)(s + 13);
                    }
                }
                else
                {
                    if(s >= 'A' && s <= 'Z')
                    {
                        if(s > 'M')
                        {
                            return (char)(s - 13);
                        }
                        else
                        {
                            return (char)(s + 13);
                        }
                    }
                    else
                    {
                        return (char)s;
                    }
                }
            }).ToArray());
        }

        public override bool OnStart()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 12;
            bool wynik = base.OnStart();
            Trace.TraceInformation("Wystartowano");
            return wynik;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Stopowany");
            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();
            base.OnStop();
            Trace.TraceInformation("Zastopowany");
        }

        public override void Run()
        {
            Trace.TraceInformation("Dziala");
            try
            {
                var kolejka = GetKolejka("roboczy");
                var wiadomosc = kolejka.GetMessage();
                if (wiadomosc != null)
                {
                    Random random = new Random();
                    var rand = random.Next(0, 3);
                    if (rand == 0)
                    {
                        throw new Exception("Kontrolowany wyjatek");
                    }
                    var nowaWiadomosc = ROT13(new string(wiadomosc.AsString.Skip(wiadomosc.AsString.IndexOf("|") + 1).ToArray()));
                    var blobKontener = GetKontener("zaszyfrowane");
                    var blob = blobKontener.GetBlockBlobReference(new string(wiadomosc.AsString.Take(wiadomosc.AsString.IndexOf("|")).ToArray()));
                    var bytes = new ASCIIEncoding().GetBytes(nowaWiadomosc);
                    var stream = new MemoryStream(bytes);
                    blob.UploadFromStream(stream);
                    kolejka.DeleteMessage(wiadomosc);
                }
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }
    }
}