using System;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using FMB_CIS.Data;
using FMB_CIS.Models;
using System.Security.Cryptography;


namespace FMB_CIS.Controllers
{
   
    public class DashboardController : Controller
    {

        private readonly LocalContext _context;
        private readonly IConfiguration _configuration;
        
        public DashboardController(IConfiguration configuration, LocalContext context)
        {
            this._configuration = configuration;
            _context = context;
        }

        [Authorize]
        public IActionResult Index()
        {
            ViewModel mymodel = new ViewModel();
            int userID = Convert.ToInt32(((ClaimsIdentity)User.Identity).FindFirst("userID").Value);
            string userRole = ((ClaimsIdentity)User.Identity).FindFirst("userRole").Value;

            //CHECK IF USER STATUS IS APPROVED OR NOT
            bool? userStatus = _context.tbl_user.Where(u => u.id == userID).Select(u => u.status).SingleOrDefault();

            if (userStatus == false)
            {
                return RedirectToAction("EditAccount", "AccountManagement");
            }
            else
            { 
                if (((ClaimsIdentity)User.Identity).FindFirst("userID").Value != null)
                {    

                    if(userRole.Contains("Owner") == true || userRole.Contains("Seller") == true || userRole.Contains("Importer") == true)
                    {

                    //OWNED CHAINSAWS
                    var ChainsawList = _context.tbl_chainsaw.ToList();
                    var ChainsawOwnedList = ChainsawList.Where(m => m.user_id == userID /*&& m.status == "Seller"*/).ToList();

                        //HISTORY
                        var applicationlist = from a in _context.tbl_application
                                              where a.tbl_user_id == userID
                                              select a;

                    
                        var applicationtypelist = _context.tbl_application_type;

                        var applicationMod = from a in applicationlist
                                             join appt in applicationtypelist on a.tbl_application_type_id equals appt.id
                                             join pT in _context.tbl_permit_type on a.tbl_application_type_id equals pT.id
                                             join pS in _context.tbl_permit_status on a.status equals pS.id
                                             where a.tbl_user_id == userID
                                             select new ApplicationModel { id = a.id, application_type = appt.name, permit_type = pT.name, permit_status = pS.status };
                        mymodel.tbl_Chainsaws = ChainsawOwnedList;
                        mymodel.applicationModels = applicationMod;


                        return View(mymodel);
                    }
                    else if(userRole.Contains("CENRO") == true)
                    {
                        var applicationlist = _context.tbl_application;
                        var applicationtypelist = _context.tbl_application_type;

                    var applicationMod = from a in applicationlist
                                         join appt in applicationtypelist on a.tbl_application_type_id equals appt.id
                                         join pT in _context.tbl_permit_type on a.tbl_application_type_id equals pT.id
                                         join pS in _context.tbl_permit_status on a.status equals pS.id
                                         where a.tbl_user_id == userID
                                         select new ApplicationModel{id = a.id, application_type = appt.name, permit_type = pT.name, permit_status = pS.status};
                    //mymodel.tbl_Chainsaws = ChainsawOwnedList;
                    mymodel.applicationModels = applicationMod;


                        return View(mymodel);
                    }
                    else if(userRole.Contains("CENRO") == true)
                    {
                        var applicationlist = _context.tbl_application;
                        var applicationtypelist = _context.tbl_application_type;

                        var applicationMod = from a in applicationlist
                                             join appt in applicationtypelist on a.tbl_application_type_id equals appt.id
                                             join pT in _context.tbl_permit_type on a.tbl_application_type_id equals pT.id
                                             join pS in _context.tbl_permit_status on a.status equals pS.id
                                             select new ApplicationModel { 
                                                 id = a.id, 
                                                 application_type = appt.name, 
                                                 permit_type = pT.name, 
                                                 permit_status = pS.status, 
                                                 FullName = a.supplier_fname + " " + a.supplier_mname + " " + a.supplier_lname + " " + a.supplier_suffix,
                                                 Email = a.supplier_email
                                             };
                        mymodel.applicationModels = applicationMod;

                        return View(mymodel);
                    }                
                }
            }
            return View();
            
        }

        public IActionResult UserHistory()
        {
            var ChainsawList = _context.tbl_chainsaw.ToList();
            int user_id = 1;
            var ChainsawOwnedList = ChainsawList.Where(m => m.user_id == user_id).ToList();


            //return ChainsawOwnedList;
            return View();
        }


    }
}
