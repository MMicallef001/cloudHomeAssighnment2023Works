using cloudHomeAssighnment2023.Models;
using Common.Models;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace cloudHomeAssighnment2023.DataAccess
{
    public class FirestoreMoviesRepositary
    {
        FirestoreDb db;

        public FirestoreMoviesRepositary(string project)
        {
            db = FirestoreDb.Create(project);
        }

        public async void AddMovie(Movie m)
        {
            string fileId = Path.GetFileNameWithoutExtension(m.NameOfFile);
            await db.Collection("movies").Document(fileId).SetAsync(m);
        }

        public async Task<List<Movie>> GetMovies()
        {

            List<Movie> movies = new List<Movie>();
            Query allMoviesQuery = db.Collection("movies");
            QuerySnapshot allMoviesQuerySnapshot = await allMoviesQuery.GetSnapshotAsync();
            foreach (DocumentSnapshot documentSnapshot in allMoviesQuerySnapshot.Documents)
            {
                Movie m = documentSnapshot.ConvertTo<Movie>();
                movies.Add(m);
            }

            return movies;
     
        }
        public async Task<List<Movie>> GetMoviesForUser(string name)
        {
            List<Movie> movies = new List<Movie>();
            Query allMoviesQuery = db.Collection("movies");
            QuerySnapshot allMoviesQuerySnapshot = await allMoviesQuery.GetSnapshotAsync();

            foreach (DocumentSnapshot documentSnapshot in allMoviesQuerySnapshot.Documents)
            {
                Movie m = documentSnapshot.ConvertTo<Movie>();
                if (m.Owner == name)
                {
                    movies.Add(m);
                }
            }

            return movies;

        }

        public async Task<Movie> GetMovie(string nameOfFile)
        {
            Query moviesQuery = db.Collection("movies").WhereEqualTo("NameOfFile", nameOfFile);
            QuerySnapshot moviesQuerySnapshot = await moviesQuery.GetSnapshotAsync();

            DocumentSnapshot documentSnapshot = moviesQuerySnapshot.Documents.FirstOrDefault();
            if (documentSnapshot.Exists == false)
            {
                return null;
            }
            else
            {
                Movie result = documentSnapshot.ConvertTo<Movie>();
                return result;
            }
        }

    }
}
