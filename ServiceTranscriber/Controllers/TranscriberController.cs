using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Common.Models;
using Google.Cloud.Storage.V1;
using System.IO;
using NAudio.Wave;
using NAudio.Mixer;
using NAudio.Codecs;
using NAudio.Flac;
using NAudio.Vorbis;
using NAudio;


namespace TranscriberService.Controllers
{
    public class TranscriberController : Controller
    {
        public async Task<IActionResult> Index()
        {
            string projectId = "cloudhomeassignment-385408";
            string subscriptionId = "Transcriptions-sub";
            bool acknowledge = false;

            SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);
            SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName);
            // SubscriberClient runs your message handle function on multiple
            // threads to maximize throughput.
            int messageCount = 0;

            List<string> Transcriptions = new List<string>();


            Task startTask = subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
            {
                string text = System.Text.Encoding.UTF8.GetString(message.Data.ToArray());
                Transcriptions.Add($"{message.MessageId}: {text}");

                Interlocked.Increment(ref messageCount);
                //if(acknowledge == true) { return SubscriberClient.Reply.Ack} else {return SubscriberClient.Reply.Nack}

                return Task.FromResult(acknowledge ? SubscriberClient.Reply.Ack : SubscriberClient.Reply.Nack);
            });
            // Run for 5 seconds.
            await Task.Delay(5000);
            await subscriber.StopAsync(CancellationToken.None);
            // Lets make sure that the start task finished successfully after the call to stop.
            await startTask;


            //evaluate the messages list

            foreach (var msg in Transcriptions.Distinct().ToList())
            {
                var actualMessage = msg.Split(": ")[1];
                Movie m = JsonConvert.DeserializeObject<Movie>(actualMessage);


                string bucketName = "cloudhomeassighnmentbucket2023";

                // Create a client object to interact with the bucket
                var storage = StorageClient.Create();

                // Download the file to a MemoryStream
                MemoryStream stream = new MemoryStream();

                storage.DownloadObject(bucketName, m.NameOfFile, stream);

                stream.Position = 0;
                // Reset the stream position to the beginning

                string tempFilePath = Path.GetTempFileName();

                // Write the stream to the temporary file
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    stream.WriteTo(fileStream);
                }

                // Load the temporary file into a WaveStream
                var waveStreamMp4 = new MediaFoundationReader(tempFilePath);

                var waveStreamWav = WaveFormatConversionStream.CreatePcmStream(waveStreamMp4);

                var wavStream = new MemoryStream();
                WaveFileWriter.WriteWavFileToStream(wavStream, waveStreamWav);
                wavStream.Position = 0;

                //System.IO.File.Delete(tempFilePath);//check if works


                return File(waveStreamWav, "application/octet-stream", m.NameOfFile);


            }
            return Content("Messages read and processed from queue: " + messageCount.ToString());
        }
    }
}
