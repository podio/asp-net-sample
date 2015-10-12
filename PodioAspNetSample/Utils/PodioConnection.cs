using PodioAPI;
using System;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using PodioAPI.Utils.Authentication;
using Newtonsoft.Json;
using PodioAspNetSample.Models;

namespace PodioAspNetSample.Utils
{
    public class PodioConnection
    {
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
        public static string RedirectUrl { get; set; }

        public static bool IsAuthenticated
        { 
            get 
            {
                Podio podio = GetClient();
                //Check if there is a stored access token already present.
                return podio.IsAuthenticated();
            } 
        }

        static PodioConnection()
        {
            ClientId = ConfigurationManager.AppSettings["PodioClientId"];
            ClientSecret = ConfigurationManager.AppSettings["PodioClientSecret"];

            if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
                throw new Exception("ClientId and ClientSecret not set. Please set that in Web.config");

            // Set RedirectUrl to /Authorization/HandleAuthorizationResponse action. To use in server side auth flow
            UrlHelper urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
            RedirectUrl = urlHelper.Action("HandleAuthorizationResponse", "Authorization", new object { }, "http");
        }

        /// <summary>
        /// Return an instance of Podio object
        /// </summary>
        /// <returns></returns>
        public static Podio GetClient()
        {
           /* SessionAuthStore is an implementation of PodioAPI.Utils.IAuthStore interface (stores OAuth data in DB)
            You can do your own implementation of IAuthStore interface if you need to store access token in database or whereever,
            and pass it in when you initialize Podio class
           */
            var dbAuthStore = new DbOAuthTokenStore();

            var podio = new Podio(ClientId, ClientSecret, dbAuthStore);
            if(HttpContext.Current.Session["UserId"] != null)
            {
                // If a userId is found in session, get the Sored token from database and set it to Podio object.
                var podioAuthData = new PodioOAuthData();
                var currentUserId = int.Parse(HttpContext.Current.Session["UserId"].ToString());

                var data = podioAuthData.GetbyUserId(currentUserId);
                if(data != null)
                {
                    var podioOAuthObject = JsonConvert.DeserializeObject<PodioOAuth>(data.OAuthJsonData);
                    podio.OAuth = podioOAuthObject;
                }
            }

            return podio;
        }
    }
}