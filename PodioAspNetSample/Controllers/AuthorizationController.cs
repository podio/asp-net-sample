using System.Web.Mvc;
using PodioAspNetSample.ViewModels;
using PodioAPI.Exceptions;
using PodioAPI;
using System.Threading.Tasks;
using PodioAspNetSample.Utils;

namespace PodioAspNetSample.Controllers
{
    public class AuthorizationController : Controller
    {
        public Podio PodioClient { get; set; }

        public string RedirectUrl { get; set; }

        public AuthorizationController()
        {
            PodioClient = PodioConnection.GetClient();
            RedirectUrl = PodioConnection.RedirectUrl;
        }

        public ActionResult Index()
        {
            ViewBag.hideLogoutButton = true;
            return View();
        }

        /// <summary>
        /// This is an authentication flow using username and password. Do not use this for authenticating users other than yourself.
        /// For more details see : https://developers.podio.com/authentication/username_password
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> UsernamePasswordAuthentication(UsernamePasswordAuthenticationViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var authInfo = await PodioClient.AuthenticateWithPassword(model.Username, model.Password);
                    Session["UserId"] = authInfo.Ref.Id;

                    return RedirectToAction("Index", "Leads");
                }
                catch (PodioException ex)
                {
                    TempData["podioError"] = ex.Error.ErrorDescription;
                }    
            }

            return View("Index");
        }

        /// <summary>
        /// When you have authorized our application in Podio - you can use the code that podio returns. 
        /// </summary>
        public async Task<ActionResult> HandleAuthorizationResponse(string code, string error_reason, string error, string error_description)
        {
            //If error is empty, that means the authrization is succesfull and the authrization code returns
            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(code))
            {
                var authInfo = await PodioClient.AuthenticateWithAuthorizationCode(code, RedirectUrl);
                Session["UserId"] = authInfo.Ref.Id;
                return RedirectToAction("Index", "Leads");
            }
            else
            {
                TempData["podioError"] = error;
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Start server side flow
        ///  For more details see : https://developers.podio.com/authentication/server_side
        /// </summary>
        public void ServerSideAuthentication()
        {
            Response.Redirect(PodioClient.GetAuthorizeUrl(RedirectUrl));
        }

        [HttpPost]
        public ActionResult Logout()
        {
            ViewBag.hideLogoutButton = true;

            Session.Clear();
            Session.Abandon();

            return View("Index");
        }
    }
}
