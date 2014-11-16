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

    public partial class VENDOR
    {
        public string Id { get; set; }


        [Required]
        [Display(Name = "First Name", Prompt = "Enter First Name", Description = "Vendor First Name")]
        public string FIRSTNAME { get; set; }

        [Required]
        [Display(Name = "Last Name", Prompt = "Enter Last Name", Description = "Vendor Last Name")]
        public string LASTNAME { get; set; }

        [Required]
        [Display(Name = "Organization", Prompt = "Enter Organization Name", Description = "Vendor's Organization")]
        public string ORGANIZATION { get; set; }

        [Display(Name = "Sanctioned")]
        public bool SANCTIONED { get; set; }

        [Display(Name = "Status")]
        public string VENDSTATUS { get; set; }

        public string LOG { get; set; }

        //[Required]
        [Display(Name = "W9", Prompt = "Please upload your W9 in PDF format", Description = "Vendor's W9 form")]
        public string W9 { get; set; }
    
        public virtual AspNetUser AspNetUser { get; set; }
    }
}
