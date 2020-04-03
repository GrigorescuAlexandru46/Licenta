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
            Profile ownProfile = GetOwnProfile();


            var pollsList = (from poll in db.Polls
                             where poll.OwnerId == ownProfile.ProfileId
                             select poll
                            ).ToList();

            ViewBag.PollsList = pollsList;
            return View();
        }

        public ActionResult Show(int id)
        {
            var profileController = new ProfileController();
            profileController.ControllerContext = ControllerContext;

            Poll poll = db.Polls.Find(id);

            ViewBag.UserIsAdmin = profileController.UserIsAdmin();
            ViewBag.UserIsProfileOwner = profileController.UserIsProfileOwner(poll.Profile);

            return View(poll);
        }

        public ActionResult New()
        {
            Poll poll = new Poll();

            return View(poll);
        }

        [HttpPost]
        public ActionResult New(Poll poll)
        {
            try
            {
                Profile ownProfile = GetOwnProfile();

                poll.OwnerId = ownProfile.ProfileId;

                db.Polls.Add(poll);
                db.SaveChanges();
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
                return RedirectToAction("Show", "Poll", new { id = newPoll.PollId });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return View();
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
    }
}