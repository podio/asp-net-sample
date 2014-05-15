using PodioAPI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PodioAPI.Utils.Authentication;

namespace PodioAspNetSample.Models
{
    public class PodioConnection
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUrl { get; set; }

        public  bool IsAuthenticated
        { 
            get 
            {
                Podio podio = GetClient();

                //Check if there is a stored access token already present. By default this api client store access token in session
                return podio.IsAuthenticated();
            } 
        }

        public PodioConnection()
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
        public Podio GetClient()
        {
           /* SessionAuthStore is an implementation of PodioAPI.Utils.IAuthStore interface (stores OAuth data in Session) which is provided by Podio-dotnet client libray out of box. 
            You can do your own implementation of IAuthStore interface if you need to store access token in database or whereever,
            and pass it in when you initialize Podio class
           */
            var sessionAuthStore = new SessionAuthStore();

            var podio = new Podio(ClientId, ClientSecret);
            return podio;
        }
    }
}