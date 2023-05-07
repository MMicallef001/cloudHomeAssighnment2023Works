using cloudHomeAssighnment2023.DataAccess;
using cloudHomeAssighnment2023.Models;
using Common.Models;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace cloudHomeAssighnment2023.Controllers
{
    public class TransfersController : Controller
    {
        FirestoreMoviesRepositary _moviesRepo;
        FirestoreHistoryRepositary _historyRepo;
        PubSubTranscriptionRepositary _pubSub;
        public TransfersController(FirestoreMoviesRepositary moviesRepo, FirestoreHistoryRepositary firestoreHistory, PubSubTranscriptionRepositary pubSub)
        {
            _moviesRepo = moviesRepo;
            _historyRepo = firestoreHistory;
            _pubSub = pubSub;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult Upload()
        { return View(); }


        [HttpPost]
        [Authorize]
        public IActionResult Upload(Movie m, IFormFile file, IFormFile thumbnail, [FromServices] IConfiguration config, System.DateTime uploadDate)
        {
            if (ModelState.IsValid)
            {
                string bucketName = config["bucket"].ToString();
                if (file != null)
                {
                    var storage = StorageClient.Create();
                    using var fileStream = file.OpenReadStream();

                    string newFilename = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(file.FileName);

                    m.NameOfFile = newFilename;

                    storage.UploadObject(bucketName, newFilename, null, fileStream);

                    using var ms = new MemoryStream();
                    thumbnail.CopyTo(ms);
                    var imageBytes = ms.ToArray();

                    m.TumbnailString = Convert.ToBase64String(imageBytes);


                    m.BucketURI = $"https://storage.googleapis.com/{bucketName}/{newFilename}";

                }
                m.uploadDate = Google.Cloud.Firestore.Timestamp.FromDateTime(uploadDate.ToUniversalTime());
                m.Status = false;
                m.Owner = User.Identity.Name;
                m.Transcription = "";

                DateTime dateTime = DateTime.UtcNow;
                Timestamp timestamp = Timestamp.FromDateTime(dateTime);

                m.uploadDate = timestamp;



                _moviesRepo.AddMovie(m);
                TempData["success"] = "Movie was added successfully in database";




            }else
            {
                string jsonWarnings = JsonConvert.SerializeObject(ModelState.Values);
            }

                return View();
            
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List()
        {
            List<Movie> movies = new List<Movie>();
            movies = await _moviesRepo.GetMoviesForUser(User.Identity.Name);
            return View(movies);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Transcribe(string nameOfFile)
        {
            Movie movie = await _moviesRepo.GetMovie(nameOfFile);
            await _pubSub.PushMessage(movie);
            return RedirectToAction("list");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Download(string nameOfFile, [FromServices] IConfiguration config, System.DateTime DownloadDate)
        {
            string bucketName = config["bucket"].ToString();

            // Create a client object to interact with the bucket
            var storage = StorageClient.Create();

            // Download the file to a MemoryStream
            var stream = new MemoryStream();

            storage.DownloadObject(bucketName, nameOfFile, stream);

            stream.Position = 0;
            // Reset the stream position to the beginning
            History h = new History();

            h.nameOfFile = nameOfFile;

            DateTime dateTime = DateTime.UtcNow;
            Timestamp timestamp = Timestamp.FromDateTime(dateTime);
            h.DownloadDate = timestamp;

            _historyRepo.AddHistory(h);

            // Return the file            
            return File(stream, "application/octet-stream", nameOfFile);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DownloadTranscription(string nameOfFile)
        {
            Movie movie = await _moviesRepo.GetMovie(nameOfFile);

            byte[] transcription = System.Text.Encoding.UTF8.GetBytes(movie.Transcription);
            MemoryStream memoryStream = new MemoryStream(transcription);

            string fileName = Path.GetFileNameWithoutExtension(nameOfFile)+".txt";

            return File(memoryStream, "text/plain", fileName);

        }

        [HttpGet]
        [Authorize]
        public IActionResult DownloadSRT(string nameOfFile)
        {

            string fileName = Path.GetFileNameWithoutExtension(nameOfFile) + ".srt";

            // Create a client object to interact with the bucket
            var storage = StorageClient.Create();

            // Download the file to a MemoryStream
            var stream = new MemoryStream();


            storage.DownloadObject("srtbucketcloudassighnement", fileName, stream);

            stream.Position = 0;
            // Reset the stream position to the beginning


            // Return the file            
            return File(stream, "application/octet-stream", fileName);
        }


    }
}

