﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Web.Security;
using System.Text.RegularExpressions;

using SansSoussi.Filter;

namespace SansSoussi.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private const int COUNT_BY_MINUTE = 15;

        private string SanitizeComment(string text)
        {
            return Regex.Replace(text, @"[^\sA-z0-9,.!]", " ", RegexOptions.IgnoreCase);
        }

        SqlConnection _dbConnection;
        public HomeController()
        {
             _dbConnection = new SqlConnection(WebConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString);
        }

        [Throttle(TimeUnit = TimeUnit.Minute, Count = COUNT_BY_MINUTE)]
        public ActionResult Index()
        {
            ViewBag.Message = "Parce que marcher devrait se faire SansSoussi";

            return View();
        }

        [Throttle(TimeUnit = TimeUnit.Minute, Count = COUNT_BY_MINUTE)]
        public ActionResult Comments()
        {
            List<string> comments = new List<string>();

            //Get current user from default membership provider
            MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
            if (user != null)
            {
                SqlCommand cmd = new SqlCommand("Select Comment from Comments where UserId ='" + user.ProviderUserKey + "'", _dbConnection);
                _dbConnection.Open();
                SqlDataReader rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    comments.Add(SanitizeComment(rd.GetString(0)));
                }

                rd.Close();
                _dbConnection.Close();
            }
            return View(comments);
        }

        [HttpPost]
        [ValidateInput(true)]
        [Throttle(TimeUnit = TimeUnit.Minute, Count = COUNT_BY_MINUTE)]
        public ActionResult Comments(string comment)
        {
            string status = "success";
            try
            {
                //Get current user from default membership provider
                MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
                if (user != null)
                {
                    //add new comment to db
                    SqlCommand cmd = new SqlCommand(
                        "insert into Comments (UserId, CommentId, Comment) Values ('" + user.ProviderUserKey + "','" + Guid.NewGuid() + "','" + SanitizeComment(comment) + "')",
                    _dbConnection);
                    _dbConnection.Open();

                    cmd.ExecuteNonQuery();
                }
                else
                {
                    throw new Exception("Vous devez vous connecter");
                }
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }
            finally
            {
                _dbConnection.Close();
            }

            return Json(status);
        }

        [Throttle(TimeUnit = TimeUnit.Minute, Count = COUNT_BY_MINUTE)]
        public ActionResult Search(string searchData)
        {
            List<string> searchResults = new List<string>();

            //Get current user from default membership provider
            MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
            if (user != null)
            {
                if (!string.IsNullOrEmpty(searchData))
                {
                    SqlCommand cmd = new SqlCommand("Select Comment from Comments where UserId = '" + user.ProviderUserKey + "' and Comment like '%" + searchData + "%'", _dbConnection);
                    _dbConnection.Open();
                    SqlDataReader rd = cmd.ExecuteReader();


                    while (rd.Read())
                    {
                        searchResults.Add(SanitizeComment(rd.GetString(0)));
                    }

                    rd.Close();
                    _dbConnection.Close();
                }
            }
            return View(searchResults);
        }

        [HttpGet]
        [Throttle(TimeUnit = TimeUnit.Minute, Count = COUNT_BY_MINUTE)]
        public ActionResult Emails()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Throttle(TimeUnit = TimeUnit.Minute, Count = COUNT_BY_MINUTE)]
        public ActionResult Emails(object form)
        {
            List<string> searchResults = new List<string>();

            HttpCookie cookie = HttpContext.Request.Cookies["username"];
            
            List<string> cookieString = new List<string>();

            //Decode the cookie from base64 encoding
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(cookie.Value);
            string strCookieValue = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);

            //get user role base on cookie value
            string[] roles = Roles.GetRolesForUser(strCookieValue);

            bool isAdmin = roles.Contains("admin");

            if (isAdmin)
            {
                SqlCommand cmd = new SqlCommand("Select Email from aspnet_Membership", _dbConnection);
                _dbConnection.Open();
                SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    searchResults.Add(SanitizeComment(rd.GetString(0)));
                }
                rd.Close();
                _dbConnection.Close();
            }


            return Json(searchResults);
        }

        [Throttle(TimeUnit = TimeUnit.Minute, Count = COUNT_BY_MINUTE)]
        public ActionResult About()
        {
            return View();
        }
    }
}
