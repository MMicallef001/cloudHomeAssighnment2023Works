using Google.Cloud.Firestore;
using System;

namespace Common.Models
{
    [FirestoreData]
    public class Movie
    {

        [FirestoreProperty]
        public string NameOfFile { get; set; }

        [FirestoreProperty]
        public Timestamp uploadDate { get; set; }

        [FirestoreProperty]
        public string Owner { get; set; }

        [FirestoreProperty]
        public string BucketURI { get; set; }

        [FirestoreProperty]
        public string Transcription { get; set; }

        [FirestoreProperty]
        public string TumbnailString { get; set; }

        [FirestoreProperty]
        public bool Status { get; set; }

        [FirestoreProperty]
        public bool SRTGenerated { get; set; }
        public DateTime DtFrom
        {
            get { return uploadDate.ToDateTime(); }
            set { uploadDate = Google.Cloud.Firestore.Timestamp.FromDateTime(value.ToUniversalTime()); }
        }

    }
}
