using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Google.Cloud.Storage.V1;

namespace PubSubFunction
{
    public class Function : ICloudEventFunction<MessagePublishedData>
    {
        private readonly ILogger<Function> _logger;

        public Function(ILogger<Function> logger) => _logger = logger;
        public Task HandleAsync(CloudEvent cloudEvent, MessagePublishedData data, CancellationToken cancellationToken)
        {
            _logger.LogInformation("PubSub Function started");
    
            var jsonFromMessage = data.Message?.TextData;

            _logger.LogInformation($"Data Received is {jsonFromMessage}");

            // Deserialize the JSON object into a dynamic object
            dynamic receivedData = JsonConvert.DeserializeObject(jsonFromMessage);

            // Extract the SubtitleEntries and Document from the deserialized object
            List<SubtitleEntry> subtitleEntries = JsonConvert.DeserializeObject<List<SubtitleEntry>>(receivedData.SubtitleEntries.ToString());
            string FileNameNoExtension = receivedData.Document;


            _logger.LogInformation($"FileNameNoExtension is {FileNameNoExtension}");
            FirestoreDb db = FirestoreDb.Create("cloudhomeassignment-385408");

            DocumentReference docRef = db.Collection("movies").Document(FileNameNoExtension);

            
            var srtContent = new StringBuilder();

            foreach (var entry in subtitleEntries)
            {
                srtContent.AppendLine($"{entry.Index}");
                srtContent.AppendLine($"{entry.StartTime:hh\\:mm\\:ss\\,fff} --> {entry.EndTime:hh\\:mm\\:ss\\,fff}");
                srtContent.AppendLine(entry.Text);
                srtContent.AppendLine();
            }
            
            byte[] srtBytes = System.Text.Encoding.UTF8.GetBytes(srtContent.ToString());
            MemoryStream srtStream = new MemoryStream(srtBytes);
            srtStream.Position = 0;


            Dictionary<string, object> update = new Dictionary<string, object>
            {
                { "SRTGenerated", true}
            };

            var storage = StorageClient.Create();

            string newFilename = FileNameNoExtension + ".srt";

            storage.UploadObject("srtbucketcloudassighnement", newFilename, null, srtStream);

            var t = docRef.SetAsync(update, SetOptions.MergeAll);
            //code other things so that they are executed meanwhile

            t.Wait();
            return Task.CompletedTask;
        }
    }
}

//gcloud functions deploy srt-pubsub-function --allow-unauthenticated --entry-point PubSubFunction.Function --region us-central1 --runtime dotnet6 --trigger-topic SRTs