using PodioAPI.Utils.Authentication;
using Newtonsoft.Json;
using PodioAspNetSample.Models;

namespace PodioAspNetSample
{
    public class DbOAuthTokenStore : IAuthStore
    {
        //Store the PodioOAuth object in the db
        public void Set(PodioOAuth podioOAuth)
        {
            var jsonData = JsonConvert.SerializeObject(podioOAuth);
            var podioOAuthData = new PodioOAuthData
            {
                UserId = (int)podioOAuth.Ref.Id,
                OAuthJsonData = jsonData
            };

            podioOAuthData.Upsert();
        }
    }
}