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
using Google.Cloud.Speech.V1;
using System;
using TranscribeService.DataAccess;
using System.Text;
using TranscribeService.Models;
using Google.Type;
using Microsoft.Extensions.Logging;
using MediaToolkit.Model;
using MediaToolkit;

namespace TranscribeService.Controllers
{
    public class TranscribeController : Controller
    {
        private readonly ILogger<TranscribeController> _logger;
        UpdateRepositary _updateRepo;
        PubSubRepositary _pubSub;
        public TranscribeController(UpdateRepositary updateRepo, PubSubRepositary pubSub, ILogger<TranscribeController> logger)
        {
            _updateRepo = updateRepo;
            _pubSub = pubSub;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Index started");

            string projectId = "cloudhomeassignment-385408";
            string subscriptionId = "Transcriptions-sub";
            bool acknowledge = true;

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

            _logger.LogInformation("subscriptions have been read :" + Transcriptions);

            // Run for 5 seconds.
            await Task.Delay(5000);
            await subscriber.StopAsync(CancellationToken.None);
            // Lets make sure that the start task finished successfully after the call to stop.
            await startTask;

            //evaluate the messages list

            foreach (var msg in Transcriptions.Distinct().ToList())
            {
                _logger.LogInformation("In for loop");

                var actualMessage = msg.Split(": ")[1];
                Movie m = JsonConvert.DeserializeObject<Movie>(actualMessage);

                _logger.LogInformation("m deserialized : "+ m);
                string bucketName = "cloudhomeassighnmentbucket2023";

                // Create a client object to interact with the bucket
                var storage = StorageClient.Create();

                // Download the file to a MemoryStream
                MemoryStream stream = new MemoryStream();

                storage.DownloadObject(bucketName, m.NameOfFile, stream);

                _logger.LogInformation("File downloaded");

                stream.Position = 0;
                // Reset the stream position to the beginning

                string tempFilePath = Path.GetTempFileName();

                _logger.LogInformation("tempPath : " + tempFilePath);

                // Write the stream to the temporary file
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    stream.WriteTo(fileStream);
                }

                _logger.LogInformation("stream written to temo file");

                // Load the temporary file into a WaveStream
                var waveStreamMp4 = new MediaFoundationReader(tempFilePath);

                var waveStreamWav = WaveFormatConversionStream.CreatePcmStream(waveStreamMp4);

                var wavStream = new MemoryStream();
                WaveFileWriter.WriteWavFileToStream(wavStream, waveStreamWav);
                wavStream.Position = 0;

                _logger.LogInformation("temp file deleted");
                System.IO.File.Delete(tempFilePath);//check if works


                string tempWavPath = Path.GetTempFileName();
                string tempFlacPath = Path.ChangeExtension(tempWavPath, ".flac");

                using (var tempWavStream = new FileStream(tempWavPath, FileMode.Create, FileAccess.Write))
                {
                    wavStream.CopyTo(tempWavStream);
                }

                _logger.LogInformation("wav file written to .flac stream");

                var wavInputFile = new MediaFile { Filename = tempWavPath };
                var flacOutputFile = new MediaFile { Filename = tempFlacPath };

                using (var engine = new Engine())
                {
                    engine.Convert(wavInputFile, flacOutputFile);
                }

                _logger.LogInformation("converting input file to flac output");

                var flacStream = new MemoryStream();
                using (var tempFlacStream = new FileStream(tempFlacPath, FileMode.Open, FileAccess.Read))
                {
                    tempFlacStream.CopyTo(flacStream);
                }

                _logger.LogInformation("copying tempflacfile to flacstream");

                flacStream.Position = 0;

                System.IO.File.Delete(tempWavPath);
                System.IO.File.Delete(tempFlacPath);

                _logger.LogInformation("deleted the temp files");


                string flacFile = Path.GetFileNameWithoutExtension(m.NameOfFile);

                flacFile = flacFile + ".flac";


                bucketName = "flacbucketcloud";

                storage = StorageClient.Create();

                _logger.LogInformation("uploading file");

                storage.UploadObject(bucketName, flacFile, null, flacStream);

                _logger.LogInformation("uploaded file");

                flacStream.Position = 0;


                string BucketURI = $"gs://{bucketName}/{flacFile}";

                _logger.LogInformation("setting speech API");

                var speech = SpeechClient.Create();
                var config = new RecognitionConfig
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Flac,
                    AudioChannelCount = 2,
                    LanguageCode = LanguageCodes.English.UnitedStates,
                    EnableWordTimeOffsets = true
                };
                var audio = RecognitionAudio.FromStorageUri(BucketURI);

                var response = speech.Recognize(config, audio);

                string transcription = "";

                _logger.LogInformation("Api set");

                /*

                foreach (var result in response.Results)
                {
                    foreach (var alternative in result.Alternatives)
                    {
                        transcription = transcription + " " + alternative.Transcript;
                        //Console.WriteLine(alternative.Transcript);
                    }
                }
                */

                _logger.LogInformation("looping through subtitile entries");


                var subtitleEntries = new List<SubtitleEntry>();
                int entryIndex = 1;

                foreach (var result in response.Results)
                {
                    foreach (var alternative in result.Alternatives)
                    {
                        foreach (var wordInfo in alternative.Words)
                        {
                            transcription = transcription + " " + alternative.Transcript;

                            var entryStartTime = TimeSpan.FromSeconds(wordInfo.StartTime.Seconds + wordInfo.StartTime.Nanos / 1e9);
                            var entryEndTime = TimeSpan.FromSeconds(wordInfo.EndTime.Seconds + wordInfo.EndTime.Nanos / 1e9);

                            var subtitleEntry = new SubtitleEntry
                            {
                                Index = entryIndex,
                                StartTime = entryStartTime,
                                EndTime = entryEndTime,
                                Text = wordInfo.Word
                            };
                            subtitleEntries.Add(subtitleEntry);

                            entryIndex++;
                        }
                    }
                }
                _logger.LogInformation("out of loops");


                m.Status = true;
                m.Transcription = transcription;


                _updateRepo.Update(m);
                _logger.LogInformation("updated the movie record");

                storage.DeleteObject(bucketName, flacFile);
                _logger.LogInformation("deleted file in flac bucket");

                string docId = Path.GetFileNameWithoutExtension(m.NameOfFile);

                _pubSub.PushMessage(subtitleEntries, docId); //push works 
                _logger.LogInformation("pub sub sending message");

            }
            return Content("Messages read and processed from queue: " + messageCount.ToString());
        }
    }
}
