using Google.Cloud.Firestore;
using System;

namespace cloudHomeAssighnment2023.Models
{
    [FirestoreData]
    public class History
    {
        [FirestoreProperty]
        public Timestamp DownloadDate { get; set; }

        [FirestoreProperty]
        public string nameOfFile { get; set; }


        public DateTime DownloadDateFrom
        {
            get { return DownloadDate.ToDateTime(); }
            set { DownloadDate = Google.Cloud.Firestore.Timestamp.FromDateTime(value.ToUniversalTime()); }
        }
    }
}
