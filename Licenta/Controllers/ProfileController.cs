using Licenta.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Linq;
using System.Web.Mvc;
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
            var profilesList = (from profile in db.Profiles
                                select profile
                               ).ToList();

            ViewBag.ProfilesList = profilesList;

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"].ToString();
            }
            return View();
        }
        public ActionResult Show(int id)
        {
            Profile profile = db.Profiles.Find(id);

            ViewBag.UserIsAdmin = UserIsAdmin();
            ViewBag.UserIsProfileOwner = UserIsProfileOwner(profile);

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"].ToString();
            }
            return View(profile);
        }

        public ActionResult ShowOwnProfile()
        {
            Profile ownProfile = GetOwnProfile();


            if (TempData.ContainsKey("Message"))
            {
                TempData["Message"] = TempData["Message"];
            }
            return RedirectToAction("Show", "Profile", new { id = ownProfile.ProfileId });  
        }

        public ActionResult ShowPolls(int id)
        {
            Profile profile = db.Profiles.Find(id);

            var pollsList = (from poll in db.Polls
                             where poll.OwnerId == profile.ProfileId
                             select poll
                            ).ToList();

            ViewBag.UserIsProfileOwner = UserIsProfileOwner(profile);
            ViewBag.UserIsAdmin = UserIsAdmin();
            ViewBag.PollsList = pollsList;
            ViewBag.Profile = profile;

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"].ToString();
            }
            return View();
        }

        public ActionResult ShowOwnPolls()
        {
            Profile ownProfile = GetOwnProfile();

            return RedirectToAction("ShowPolls", new { id = ownProfile.ProfileId });
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

                TempData["Message"] = "Profile successfully edited!";
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
        
            await DeleteUser(profile.UserId);

            db.Profiles.Remove(profile);
            db.SaveChanges();

            var accountController = new AccountController();
            accountController.ControllerContext = ControllerContext;

            TempData["Message"] = "Profile successfully deleted!";
            return accountController.LogOff();
        }

        public ActionResult ConfirmDelete(int id)
        {
            Profile profile = db.Profiles.Find(id);

            return View(profile);
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

        [NonAction]
        public bool UserIsAdmin()
        {
            // If the user is an administrator
            if (User.IsInRole("Administrator"))
            {
                return true;
            }

            return false;
        }

        [NonAction]
        public bool UserIsProfileOwner(Profile profileToCheck)
        {
            Profile ownProfile = GetOwnProfile();

            // If the user owns the given profile
            if (ownProfile.ProfileId == profileToCheck.ProfileId)
            {
                return true;
            }

            return false;
        }
    }
}
