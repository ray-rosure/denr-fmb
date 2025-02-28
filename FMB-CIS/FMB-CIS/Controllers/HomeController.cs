﻿using FMB_CIS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using FMB_CIS.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Extensions;
using System.Collections.Generic;

namespace FMB_CIS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly LocalContext _context;


        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, LocalContext context)
        {
            _logger = logger;
            this._configuration = configuration;
            _context = context;
        }

        /*
        public IActionResult Index()
        {
            if (string.IsNullOrWhiteSpace(User.Identity.Name))
            {             
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Dashboard"); ;
            }
            //return View();
        }*/

        public IActionResult Index(bool success)
        {
            if (string.IsNullOrWhiteSpace(User.Identity.Name))
            {
                if (success == true)
                {
                    ViewBag.message = "Registration success! A confirmation link is sent to your registered email. Click the link and input your password using the link.";
                }
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Dashboard"); ;
            }
            //return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> IndexAsync(LoginViewModel credentials)
        {
            //bool isLoggedIn = new bool();
            if (ModelState.IsValid)
            {
                //CHECK IF THE USER EXIST
                DAL dal = new DAL();
                bool eMailExist = dal.emailExist(credentials.email, _configuration.GetConnectionString("ConnStrng"));


                if (eMailExist == false)
                {
                    ModelState.AddModelError("email", "Email or Password is incorrect.");
                    return View();

                }

                var usrDB = _context.tbl_user.Where(m => m.email == credentials.email).FirstOrDefault();
                var tblTempPass = _context.tbl_user_temp_passwords.Where(m => m.tbl_user_id == usrDB.id).FirstOrDefault();
                //Check if email is verified
                if (tblTempPass != null && tblTempPass.is_active == true) //tblTempPass.is_active means the Temporary Password is active and not yet changing their password. It also mean that the user doesn't confirm their email. tblTempPass is null when user was created before having a code for this.
                {
                    ModelState.AddModelError("email", "Please verify your email first!");
                    return View();
                }
                if (usrDB.is_active == false) //tblTempPass.is_active means the Temporary Password is active and not yet changing their password. It also mean that the user doesn't confirm their email. tblTempPass is null when user was created before having a code for this.
                {
                    ModelState.AddModelError("email", "Your account is disabled. Please check your e-mail or contact the administrator for more information.");
                    return View();
                }
                else
                {
                    string decrPw = EncryptDecrypt.ConvertToDecrypt(dal.selectEncrPassFromEmail(credentials.email, _configuration.GetConnectionString("ConnStrng")));
                    //CHECK IF PASSWORD MATCH
                    if (credentials.password != decrPw)
                    {
                        ModelState.AddModelError("password", "Email or Password is incorrect.");
                        return View();
                    }

                    else
                    {
                        List<string> accessRights = dal.selectAccessRightsFromEmail(credentials.email, _configuration.GetConnectionString("ConnStrng"));
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, credentials.email),
                            new Claim("EmailAdd", credentials.email),
                            new Claim("FullName", dal.selectFullNameFromEmail(credentials.email, _configuration.GetConnectionString("ConnStrng"))),
                            new Claim("userID", dal.selectUserIDFromEmail(credentials.email, _configuration.GetConnectionString("ConnStrng"))),
                            new Claim(ClaimTypes.Role, dal.selectUserRoleFromEmail(credentials.email, _configuration.GetConnectionString("ConnStrng"))),
                            new Claim("userRole", dal.selectUserRoleFromEmail(credentials.email, _configuration.GetConnectionString("ConnStrng"))),
                            new Claim("accessRights", accessRights != null ? string.Join(",", accessRights) : ""),
                            //new Claim(ClaimTypes.Role, "Administrator"),
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties
                        {
                            AllowRefresh = true,
                            // Refreshing the authentication session should be allowed.

                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                            // The time at which the authentication ticket expires. A 
                            // value set here overrides the ExpireTimeSpan option of 
                            // CookieAuthenticationOptions set with AddCookie.

                            IsPersistent = true,
                            // Whether the authentication session is persisted across 
                            // multiple requests. When used with cookies, controls
                            // whether the cookie's lifetime is absolute (matching the
                            // lifetime of the authentication ticket) or session-based.

                            //IssuedUtc = <DateTimeOffset>,
                            // The time at which the authentication ticket was issued.

                            //RedirectUri = <string>
                            // The full path or absolute URI to be used as an http 
                            // redirect response value.
                        };

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                        _logger.LogInformation("User {Email} logged in at {Time}.", credentials.email, DateTime.UtcNow);
                        //isLoggedIn = true;
                        return RedirectToAction("Index", "Dashboard");
                    }
                }
            }
            return View();
        }

        //public async Task Signout(/*string returnUrl = null*/)
        //{
        //    //if (!string.IsNullOrEmpty(ErrorMessage))
        //    //{
        //    //    ModelState.AddModelError(string.Empty, ErrorMessage);
        //    //}

        //    // Clear the existing external cookie
        //    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        //    return View();
        //}
        public ActionResult Signout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index");
        }

        public IActionResult TermsPolicy()
        {
            return View();
        }
    }
}