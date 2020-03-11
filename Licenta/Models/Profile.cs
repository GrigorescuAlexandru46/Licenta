using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Licenta.Models
{
    public class Profile
    {
        [Key]
        public int ProfileId { get; set; }

        [Required]
        [StringLength(30, ErrorMessage = "The {0} must have between {2} and {1} characters .", MinimumLength = 2)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [MinLength(3)]
        public string Description { get; set; }


    }

    public class ProfileDBContext : DbContext
    {
        public ProfileDBContext() : base("DefaultConnection") { }
        public DbSet<Profile> Profiles { get; set; }
    }
}