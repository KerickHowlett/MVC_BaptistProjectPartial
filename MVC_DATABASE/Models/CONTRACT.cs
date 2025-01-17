//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MVC_DATABASE.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    
    public partial class CONTRACT
    {
        [Display(Name = "Contract Id")]
        public int CONTRACTID { get; set; }

        public string Id { get; set; }
        [Display(Name = "Template Id")]
        public int TEMPLATEID { get; set; }
        [Display(Name = "RFP Id")]
        public int RFPID { get; set; }
        [Display(Name = "Contract")]
        public string CONTRACT_PATH { get; set; }
        [Display(Name = "Created")]
        public System.DateTime CREATED { get; set; }

        [Range(typeof(DateTime), "12/30/2000", "12/30/9999",
        ErrorMessage = "Value for {0} must be between {1} and {2}")]
        [Display(Name = "Expiration Date")]
        public System.DateTime EXPIRES { get; set; }
        [Display(Name = "Additional Documents")]
        public string DOCUMENTPATH { get; set; }
    
        public virtual AspNetUser AspNetUser { get; set; }
        public virtual TEMPLATE TEMPLATE { get; set; }
    }
}
