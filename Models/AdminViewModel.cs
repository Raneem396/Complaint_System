using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ComplaintManagementPortal2.Models
{


    public class ComplaintAdminViewModel
    {
        public Complaint Complaint { get; set; }
        public List<Complaint> Complaints { get; set; }
        public HttpPostedFileBase mainImage { get; set; }
        public SelectList Gender { get; set; }
        public SelectList Work { get; set; }
        public SelectList ComplaintsStatus { get; set; }



        [Required]
        [StringLength(500)]
        [Display(Name = "name")]
        public string Name;

        [Required]
        [Display(Name = "Age")]
        public int Age;

        [Required]
        [Display(Name = "GenderId")]
        [Range(1, int.MaxValue, ErrorMessage = "your Gender is required")]
        public int GenderId;

        [Required]
        [Display(Name = "WorkId")]
        [Range(1, int.MaxValue, ErrorMessage = "your work is required")]
        public int WorkId;

        [Required]
        [Display(Name = "YourComplaint")]
        [DataType(DataType.MultilineText)]
        public string YourComplaint;

    }

}