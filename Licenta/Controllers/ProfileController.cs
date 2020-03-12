using Licenta.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Administrator, User")]
    public class ProfileController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Show(int id)
        {
            Profile profile = db.Profiles.Find(id);
            return View(profile);
        }

        public ActionResult Edit(int id)
        {
            Profile profile = db.Profiles.Find(id);
            return View(profile);
        }

        [HttpPut]
        public ActionResult Edit(int id, Profile newProfile)
        {
            try
            {
                Profile oldProfile = db.Profiles.Find(id);
                if (TryUpdateModel(oldProfile))
                {
                    oldProfile.Age = newProfile.Age;
                    oldProfile.Description = newProfile.Description;
                    oldProfile.FirstName = newProfile.FirstName;
                    oldProfile.LastName = newProfile.LastName;
                    
                    db.SaveChanges();
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception e)
            {
                return View();
            }
        }
    }
}
