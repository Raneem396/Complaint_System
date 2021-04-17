using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ComplaintManagementPortal2.Models;
using PagedList;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;

using System.Globalization;

namespace ComplaintManagementPortal2.Controllers
{
    public class ComplaintsController : Controller
    {
        private ComplaintManagementPortalEntities db = new ComplaintManagementPortalEntities();

        // GET: Complaints     

        #region admin
        [Authorize(Roles = "Admin")]

        public ActionResult Index(string sortOrder, string currentFilter, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.OrderSortParm = String.IsNullOrEmpty(sortOrder) ? "order_desc" : "";
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            var user_id = User.Identity.GetUserId();
         


            var complaints = from s in db.Complaints  select s;
            switch (sortOrder)
            {
                case "name_desc":
                    complaints = complaints.OrderByDescending(s => s.Name);
                    break;
                case "name":
                    complaints = complaints.OrderBy(s => s.Name);
                    break;
                case "order_desc":
                    complaints = complaints.OrderByDescending(s => s.Id);
                    break;
                default:
                    complaints = complaints.OrderBy(s => s.Id);
                    break;
            }
                   

            int pageSize = GlobalConfig.PaginationPageSize;
            int pageNumber = (page ?? 1);
            return View(complaints.ToPagedList(pageNumber, pageSize));
        }

        [ValidateInput(false)]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ComplaintAdminViewModel model = new ComplaintAdminViewModel();
            Complaint complaint = db.Complaints.Find(id);
            if (complaint == null)
            {
                return HttpNotFound();
            }
            model.Complaint = complaint;


            var complaintsStatuses = db.ComplaintsStatuses.OrderBy(x => x.Id).ToList();
            complaintsStatuses.Insert(0, new ComplaintsStatus() { Name = "Select", Id = 0 });
            model.ComplaintsStatus = new SelectList(complaintsStatuses, "Id", "Name");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(ComplaintAdminViewModel model)
        {
           

            if (ModelState.IsValid)
            {
              

                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var complaint = db.Complaints.SingleOrDefault(p => p.Id == model.Complaint.Id);
                        
                        complaint.StatusId = model.Complaint.StatusId;
                       

                        db.SaveChanges();

                        dbContextTransaction.Commit();
                    }
                    catch (Exception ex)
                    {

                        dbContextTransaction.Rollback();
                        throw ex;
                    }
                }

                return RedirectToAction("Index", new { result = "editdone" });
            }

            return View(model);
        }


        #endregion

        #region client
        [Authorize(Roles = "Customer")]
        public ActionResult CustomerIndex(string sortOrder, string currentFilter, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.OrderSortParm = String.IsNullOrEmpty(sortOrder) ? "order_desc" : "";
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            var user_id = User.Identity.GetUserId();



            var complaints = from s in db.Complaints where s.UserId == user_id select s;
            switch (sortOrder)
            {
                case "name_desc":
                    complaints = complaints.OrderByDescending(s => s.Name);
                    break;
                case "name":
                    complaints = complaints.OrderBy(s => s.Name);
                    break;
                case "order_desc":
                    complaints = complaints.OrderByDescending(s => s.Id);
                    break;
                default:
                    complaints = complaints.OrderBy(s => s.Id);
                    break;
            }


            int pageSize = GlobalConfig.PaginationPageSize;
            int pageNumber = (page ?? 1);
            return View(complaints.ToPagedList(pageNumber, pageSize));
        }


        [Authorize(Roles = "Customer")]
        public ActionResult CustomerView(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Complaint complaint = db.Complaints.Find(id);
            if (complaint == null)
            {
                return HttpNotFound();
            }

            ComplaintAdminViewModel model = new ComplaintAdminViewModel();
            model.Complaint = complaint;



            return View(model);
        }


         [Authorize(Roles = "Customer")]
        [Route("SendComplaint", Name = "ComplaintNew")]
        public ActionResult SendComplaint(string language)
        {
            ComplaintAdminViewModel model = new ComplaintAdminViewModel();

            var gender = db.GenderTypes.OrderBy(x => x.Id).ToList();
            //gender.Insert(0, new GenderType() { Name = "Select", Id = 0 });
            model.Gender = new SelectList(gender, "Id", "Name");

            var work = db.WorkTypes.OrderBy(x => x.Id).ToList();
            work.Insert(0, new WorkType() { Name = "Select", Id = 0 });
            model.Work = new SelectList(work, "Id", "Name");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
         [Authorize(Roles = "Customer")]
        [Route("SendComplaint", Name = "ComplaintFormNew")]
        public ActionResult SendComplaint(string language, ComplaintAdminViewModel model)
        {
            
                var user_id = User.Identity.GetUserId();
               

                #region validate images 

                bool hasMainImage = false;
            if (model.mainImage != null && model.mainImage.ContentLength > 0)
            {
                hasMainImage = true;
                string extension = Path.GetExtension(model.mainImage.FileName).ToLower();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".gif")
                {
                    return RedirectToAction("Create", new { result = "invalidimage" });
                }
            }


            #endregion


            if (ModelState.IsValid)
            {
                string mainImageName = "";

                    using (var dbContextTransaction = db.Database.BeginTransaction())
                    {
                        try
                        {

                            if (hasMainImage)
                            {
                                mainImageName = Guid.NewGuid().ToString() + Path.GetExtension(model.mainImage.FileName);
                                model.Complaint.MainImage = mainImageName;
                                model.mainImage.SaveAs(Server.MapPath(GlobalConfig.ComplaintsFolder) + mainImageName);
                            }

                        model.Complaint.UserId = user_id;

                            db.Complaints.Add(model.Complaint);
                            db.SaveChanges();

                            dbContextTransaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            if (hasMainImage)
                                System.IO.File.Delete(Server.MapPath(GlobalConfig.ComplaintsFolder) + mainImageName);

                            dbContextTransaction.Rollback();
                            throw ex;
                        }
                    }


                    return RedirectToAction("Thanks", "Complaints");
                


            }

            var gender = db.GenderTypes.OrderBy(x => x.Id).ToList();
            //gender.Insert(0, new GenderType() { Name = "Select", Id = 0 });
            model.Gender = new SelectList(gender, "Id", "Name");

            var work = db.WorkTypes.OrderBy(x => x.Id).ToList();
            model.Work = new SelectList(work, "Id", "Name");
            return View(model);

        }

         [Authorize(Roles = "Customer")]
        [Route("Complaints/Thanks", Name = "ComplaintThanks")]
        public ActionResult Thanks()
        {
            return View();
        } 
      
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


    }
}