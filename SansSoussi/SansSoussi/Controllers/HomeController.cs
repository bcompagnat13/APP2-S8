using System;
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
        // BUGFIX 1: Insere des pop-ups (XSS-S)
        // <a onmouseover="alert(1)" href="#"> Salut pop-up12!</a>
        public ActionResult Comments(string comment)
        {
            string status = "success";
            try
            {
                //Get current user from default membership provider
                MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
                if (user != null)
                {
                    // Sanitize to prevent XSS-S
                    comment = SanitizeComment(comment);

                    //add new comment to db
                    // Now using Sql Parameters to prevent SQLi
                    SqlCommand cmd = new SqlCommand(
                        "insert into Comments (UserId, CommentId, Comment) Values ('" + user.ProviderUserKey + "','" + Guid.NewGuid() + "',@comment)", _dbConnection);
                    SqlParameter param = new SqlParameter("@comment", System.Data.SqlDbType.NVarChar, 115);
                    param.Value = comment;
                    cmd.Parameters.Add(param);

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

        // BUGFIX 1: Retourne le emails de tous les users
        // GET: ' UNION ALL SELECT email FROM dbo.aspnet_Membership;--

        // BUGFIX 2: Supprime un compte
        // GET: http://localhost:1033/home/Search?searchData=%%27UNION%20ALL%20SELECT%20email%20FROM%20dbo.aspnet_Membership;--
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
                    // Handling SQLi
                    SqlCommand cmd = new SqlCommand("Select Comment from Comments where UserId = '" + user.ProviderUserKey + "' and Comment like @searchData", _dbConnection);
                    SqlParameter param = new SqlParameter("@searchData", System.Data.SqlDbType.NVarChar, 16);
                    param.Value = "%"+searchData+"%";
                    cmd.Parameters.Add(param);

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
