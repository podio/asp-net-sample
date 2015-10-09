using System.Web;
using NPoco;

namespace PodioAspNetSample.Utils
{
    public static class DatabaseFactory
    {
        public static Database GetDatabase()
        {
            var db = new Database("PodioAspnetSampleDb");
            return db;
        }
    }
}