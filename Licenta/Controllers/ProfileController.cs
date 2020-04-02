using Licenta.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using System.Web.Security;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;

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
            Profile ownProfile = GetOwnProfile();

            // If the user owns the given profile
            if (ownProfile.ProfileId == id)
            {
                ViewBag.UserIsProfileOwner = true;
            }
            else
            {
                ViewBag.UserIsProfileOwner = false;
            }

            // If the user is an administrator
            if (User.IsInRole("Administrator"))
            {
                ViewBag.UserIsAdmin = true;
            }
            else
            {
                ViewBag.UserIsAdmin = false;
            }



            return View(profile);
        }

        public ActionResult ShowOwnProfile()
        {
            Profile ownProfile = GetOwnProfile();

            return RedirectToAction("Show", "Profile", new { id = ownProfile.ProfileId });  
        }

        [NonAction]
        public Profile GetOwnProfile()
        {
            string ownUserId = User.Identity.GetUserId();

            var queryProfiles = from profile in db.Profiles
                                where profile.UserId == ownUserId
                                select profile;

            Profile ownProfile = queryProfiles.FirstOrDefault();

            return ownProfile;
        }

        public ActionResult Edit(int id)
        {
            Profile profile = db.Profiles.Find(id);
            return View(profile);
        }

        [HttpPost]
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
                return RedirectToAction("Show", "Profile", new { id = newProfile.ProfileId });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return View();
            }
        }

        [HttpDelete]
        public async Task<ActionResult> Delete(int id)
        {
            Profile profile = db.Profiles.Find(id);
            TempData["Message"] = "The account of " + profile.FirstName + " " + profile.LastName + " has been deleted";

            await DeleteUser(profile.UserId);

            db.Profiles.Remove(profile);
            db.SaveChanges();

            var accountController = new AccountController();
            accountController.ControllerContext = ControllerContext;
            return accountController.LogOff();

            //return RedirectToAction("LogOff", "Account");
        }

        public async Task DeleteUser(string userId)
        {
            var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            //get User Data from Userid
            var user = await UserManager.FindByIdAsync(userId);

            //List Logins associated with user
            var logins = user.Logins;

            //Gets list of Roles associated with current user
            var rolesForUser = await UserManager.GetRolesAsync(userId);

            using (var transaction = db.Database.BeginTransaction())
            {
                foreach (var login in logins.ToList())
                {
                    await UserManager.RemoveLoginAsync(login.UserId, new UserLoginInfo(login.LoginProvider, login.ProviderKey));
                }

                if (rolesForUser.Count() > 0)
                {
                    foreach (var item in rolesForUser.ToList())
                    {
                        // item should be the name of the role
                        var result = await UserManager.RemoveFromRoleAsync(user.Id, item);
                    }
                }

                //Delete User
                await UserManager.DeleteAsync(user);

                transaction.Commit();
            }
        }
    }
}
