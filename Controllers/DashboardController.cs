using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ComplaintManagementPortal2.Models;

using System.Data;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Data.Entity;
using System.Web.UI.WebControls;
using System.Web.UI;


namespace ComplaintManagementPortal2.Controllers
{
    [Authorize(Roles = "Admin,Customer")]

    public class DashboardController : Controller
    {
        //private Database db = new Database();
        // GET: Dashboard
        public ActionResult Index()
        {
            return View();
        }
    }
}