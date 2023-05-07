using Google.Cloud.Firestore;
using System.Linq;
using System;
using Common.Models;

namespace TranscribeService.DataAccess
{
    public class UpdateRepositary
    {
        FirestoreDb db;

        public UpdateRepositary(string project)
        {
            db = FirestoreDb.Create(project);
        }

        public async void Update(Movie m)
        {
            Query MoviesQuery = db.Collection("movies").WhereEqualTo("NameOfFile", m.NameOfFile);
            QuerySnapshot booksQuerySnapshot = await MoviesQuery.GetSnapshotAsync();

            DocumentSnapshot documentSnapshot = booksQuerySnapshot.Documents.FirstOrDefault();
            if (documentSnapshot.Exists == false) throw new Exception("Movies does not exist");
            else
            {
                DocumentReference moviesRef = db.Collection("movies").Document(documentSnapshot.Id);
                await moviesRef.SetAsync(m);
            }
        }
    }
}
