using Common.Models;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using TranscribeService.Models;

namespace TranscribeService.DataAccess
{
    public class PubSubRepositary
    {
        TopicName topicName;
        Topic topic;
        public PubSubRepositary(string projectId)
        {
            topicName = TopicName.FromProjectTopic(projectId, "SRTs");
            if (topicName == null)
            {
                PublisherServiceApiClient publisher = PublisherServiceApiClient.Create();

                try
                {
                    topicName = new TopicName(projectId, "SRTs");
                    topic = publisher.CreateTopic(topicName);
                }
                catch (Exception ex)
                {
                    //log
                    throw ex;
                }
            }
        }

        public async Task<string> PushMessage(List<SubtitleEntry> se, string doc)
        {

            PublisherClient publisher = await PublisherClient.CreateAsync(topicName);

            var dataObject = new
            {
                SubtitleEntries = se,
                Document = doc
            };

            var pubsubMessage = new PubsubMessage
            {
                // The data is any arbitrary ByteString. Here, we're using text.
                Data = ByteString.CopyFromUtf8(JsonConvert.SerializeObject(dataObject)),
                // The attributes provide metadata in a string-to-string dictionary.
                Attributes =
                {
                    { "priority", "normal" }
                }
            };
            string message = await publisher.PublishAsync(pubsubMessage);
            return message;
        }
    }
}
