using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Oauth2.v2;
using Google.Apis.Plus.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using SansSoussi.Models;

namespace SansSoussi.Controllers
{
    [RequireHttps]
    public class AccountController : Controller
    {

        public static string ApplicationName = "app2-s8";
        public static string ClientId = "228129901842-gq9a8n1csk6e9vlrco0ic7onvevdv89j.apps.googleusercontent.com";
        public static string ClientSecret = "bZrzG8O4FaPZPZrCykK7cg6a";
        FileDataStore Oauth = new FileDataStore("Google_Oaut2");
    
        public static string[] Scopes =  {
                PlusService.Scope.PlusMe,
                PlusService.Scope.UserinfoEmail,
                PlusService.Scope.UserinfoProfile
            };

        public IFormsAuthenticationService FormsService { get; set; }
        public IMembershipService MembershipService { get; set; }

        protected override void Initialize(RequestContext requestContext)
        {
            if (FormsService == null) { FormsService = new FormsAuthenticationService(); }
            if (MembershipService == null) { MembershipService = new AccountMembershipService(); }

            base.Initialize(requestContext);
        }

        // **************************************
        // URL: /Account/LogOn
        // **************************************

        public ActionResult LogOn()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {

                UserCredential credential = null;

                try
                {
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        new ClientSecrets
                        {
                            ClientId = ClientId,
                            ClientSecret = ClientSecret
                        },
                        Scopes,
                        "user",
                        CancellationToken.None,
                        Oauth
                        ).Result;
                    
                    
                    var plusService = new PlusService(
                    new BaseClientService.Initializer
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = "app2-s8"
                    }
                    );
                    var userProfile = plusService.People.Get("me").Execute();
                    var userEmail = userProfile.Emails[0].Value;
                    
                    if (MembershipService.ValidateUser(userProfile.DisplayName, userProfile.Id)) {
                        FormsService.SignIn(userProfile.DisplayName, false /* createPersistentCookie */);
                        //Encode the username in base64
                        byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(userProfile.DisplayName);
                        HttpCookie authCookie = new HttpCookie("username", System.Convert.ToBase64String(toEncodeAsBytes));
                        HttpContext.Response.Cookies.Add(authCookie);
                        return RedirectToAction("Index", "Home");
                    } else
                    {
                        MembershipCreateStatus createStatus = MembershipService.CreateUser(userProfile.DisplayName, userProfile.Id, userEmail);
                        if (createStatus == MembershipCreateStatus.Success)
                        {
                            FormsService.SignIn(userProfile.DisplayName, false /* createPersistentCookie */);
                            //Encode the username in base64
                            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(userProfile.DisplayName);
                            HttpCookie authCookie = new HttpCookie("username", System.Convert.ToBase64String(toEncodeAsBytes));
                            HttpContext.Response.Cookies.Add(authCookie);
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ModelState.AddModelError("", AccountValidation.ErrorCodeToString(createStatus));
                        }
                    }
                }
                catch(Exception ex)
                {
                    credential = null;
                    ModelState.AddModelError("", ex.ToString());
                }

      
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

       
        // **************************************
        // URL: /Account/LogOff
        // **************************************

        public ActionResult LogOff()
        {
            Oauth.ClearAsync();
            FormsService.SignOut();

            return RedirectToAction("Index", "Home");
        }

        // **************************************
        // URL: /Account/Register
        // **************************************

        public ActionResult Register()
        {
            ViewBag.PasswordLength = MembershipService.MinPasswordLength;
            return View();
        }

        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                MembershipCreateStatus createStatus = MembershipService.CreateUser(model.UserName, model.Password, model.Email);

                if (createStatus == MembershipCreateStatus.Success)
                {
                    FormsService.SignIn(model.UserName, false /* createPersistentCookie */);
                    //Encode the username in base64
                    byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(model.UserName);
                    HttpCookie authCookie = new HttpCookie("username", System.Convert.ToBase64String(toEncodeAsBytes));
                    HttpContext.Response.Cookies.Add(authCookie);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", AccountValidation.ErrorCodeToString(createStatus));
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.PasswordLength = MembershipService.MinPasswordLength;
            return View(model);
        }

        // **************************************
        // URL: /Account/ChangePassword
        // **************************************

        [Authorize]
        public ActionResult ChangePassword()
        {
            ViewBag.PasswordLength = MembershipService.MinPasswordLength;
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                if (MembershipService.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword))
                {
                    return RedirectToAction("ChangePasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.PasswordLength = MembershipService.MinPasswordLength;
            return View(model);
        }

        // **************************************
        // URL: /Account/ChangePasswordSuccess
        // **************************************

        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }

    }
}
