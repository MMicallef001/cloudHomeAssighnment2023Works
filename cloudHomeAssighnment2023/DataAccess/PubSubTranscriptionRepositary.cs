using cloudHomeAssighnment2023.Models;
using Common.Models;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace cloudHomeAssighnment2023.DataAccess
{
    public class PubSubTranscriptionRepositary
    {
        TopicName topicName;
        Topic topic;
        public PubSubTranscriptionRepositary(string projectId)
        {
            topicName = TopicName.FromProjectTopic(projectId, "Transcriptions");
            if (topicName == null)
            {
                PublisherServiceApiClient publisher = PublisherServiceApiClient.Create();

                try
                {
                    topicName = new TopicName(projectId, "Transcriptions");
                    topic = publisher.CreateTopic(topicName);
                }
                catch (Exception ex)
                {
                    //log
                    throw ex;
                }
            }
        }

        public async Task<string> PushMessage(Movie m)
        {

            PublisherClient publisher = await PublisherClient.CreateAsync(topicName);

            var pubsubMessage = new PubsubMessage
            {
                // The data is any arbitrary ByteString. Here, we're using text.
                Data = ByteString.CopyFromUtf8(JsonConvert.SerializeObject(m)),
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
