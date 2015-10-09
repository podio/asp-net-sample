using NPoco;
using PodioAspNetSample.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PodioAspNetSample.Models
{
    [PrimaryKey("UserId", AutoIncrement = false)]
    public class PodioOAuthData
    {
        public int UserId { get; set; }

        public string OAuthJsonData { get; set; }

        public void Upsert()
        {
            var db = Utils.DatabaseFactory.GetDatabase();
            if(!db.Exists<PodioOAuthData>(this.UserId))
            {
                db.Insert(this);
            }
            else
            {
                db.Update(this);
            }
        }

        public PodioOAuthData GetbyUserId(int userId)
        {
            var db = Utils.DatabaseFactory.GetDatabase();
            return db.SingleOrDefaultById<PodioOAuthData>(userId);
        }
    }
}