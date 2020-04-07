using Licenta.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Administrator, User")]
    public class PollController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        public ActionResult Index()
        {
            var pollsList = (from poll in db.Polls
                             select poll
                            ).ToList();

            ViewBag.PollsList = pollsList;

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"].ToString();
            }
            return View();
        }

        public ActionResult Show(int id)
        {
            Poll poll = db.Polls.Find(id);

            ViewBag.UserIsAdmin = UserIsAdmin();
            ViewBag.UserIsProfileOwner = UserIsProfileOwner(poll.Profile);


            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"].ToString();
            }
            return View(poll);
        }

        public ActionResult New()
        {
            Poll poll = new Poll();

            return View(poll);
        }

        [HttpPost]
        public ActionResult New(FormCollection form)
        {
            try
            {
                TempData["Message"] = "Poll successfully created!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return View();
            }
        }

        public ActionResult Edit(int id)
        {
            Poll poll = db.Polls.Find(id);
            return View(poll);
        }

        [HttpPost]
        public ActionResult Edit(int id, Poll newPoll)
        {
            try
            {
                Poll oldPoll = db.Polls.Find(id);
                if (TryUpdateModel(oldPoll))
                {
                    oldPoll.Name = newPoll.Name;

                    db.SaveChanges();
                }

                TempData["Message"] = "Poll successfully edited!";
                return RedirectToAction("Show", "Poll", new { id = newPoll.PollId });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return View();
            }
        }

        [HttpDelete]
        public ActionResult Delete(int id)
        {
            Poll poll = db.Polls.Find(id);

            db.Polls.Remove(poll);
            db.SaveChanges();

            TempData["Message"] = "Poll successfully deleted!";
            return RedirectToAction("Index", "Home");
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