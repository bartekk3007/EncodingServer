using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace WCFServiceWebRole1
{
    public class Service1 : IService1
    {
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

        public void Koduj(string nazwa, string tresc)
        {
            var blobKontener = GetKontener("normalne");
            var blob = blobKontener.GetBlockBlobReference(nazwa);
            var bytes = new ASCIIEncoding().GetBytes(tresc);
            var stream = new MemoryStream(bytes);
            blob.UploadFromStream(stream);
            var kolejka = GetKolejka("roboczy");
            kolejka.AddMessage(new CloudQueueMessage($"{nazwa}|{tresc}"));
        }

        public string Pobierz(string nazwa)
        {
            var blobKontener = GetKontener("zaszyfrowane");
            var blob = blobKontener.GetBlockBlobReference(nazwa);
            var stream = new MemoryStream();
            blob.DownloadToStream(stream);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}