using cloudHomeAssighnment2023.Models;
using Google.Cloud.Firestore;
using System.IO;
using System.Threading.Tasks;

namespace cloudHomeAssighnment2023.DataAccess
{
    public class FirestoreHistoryRepositary
    {
        FirestoreDb db;

        public FirestoreHistoryRepositary(string project)
        {
            db = FirestoreDb.Create(project);

        }

        public async Task AddHistory(History h)
        {
            string fileName = Path.GetFileNameWithoutExtension(h.nameOfFile);
            await db.Collection($"movies/{fileName}/dowloads").Document().SetAsync(h);

        }
    }
}
