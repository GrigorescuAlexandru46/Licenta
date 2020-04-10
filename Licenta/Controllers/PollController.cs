using Licenta.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
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
                Poll poll = new Poll();
                int questionCount = ConvertStringToInt(form["Questions_Count"]);

                poll.Name = form["Name"];
                poll.CreationDate = ConvertStringToDateTime(form["CreationDate"]);
                poll.OwnerId = GetOwnProfile().ProfileId;

                db.Polls.Add(poll);
                db.SaveChanges();

                for (int i = 1; i <= questionCount; i++)
                {
                    Question question = new Question();
                    int answersCount = ConvertStringToInt(form["Question" + i + "_AnswersCount"]);

                    question.Text = form["Question" + i + "_Text"];
                    question.PollId = poll.PollId;

                    db.Questions.Add(question);
                    db.SaveChanges();

                    for (int j = 1; j <= answersCount; j++)
                    {
                        Answer answer = new Answer();

                        answer.Text = form["Question" + i + "_Answer" + j];
                        answer.QuestionId = question.QuestionId;

                        db.Answers.Add(answer);
                        db.SaveChanges();
                    }

                    poll.Questions.Add(question);    
                }

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

            ViewBag.QuestionsJson = ConvertPollToValidQuestionsJson(poll);

            return View(poll);
        }

        [HttpPost]
        public ActionResult Edit(FormCollection form)
        {
            try
            { 
                Poll oldPoll = db.Polls.Find(ConvertStringToInt(form["PollId"]));
                db.Polls.Remove(oldPoll);

                Poll newPoll = new Poll();

                int questionCount = ConvertStringToInt(form["Questions_Count"]);

                newPoll.Name = form["Name"];
                newPoll.CreationDate = ConvertStringToDateTime(form["CreationDate"]);
                newPoll.OwnerId = GetOwnProfile().ProfileId;

                db.Polls.Add(newPoll);
                db.SaveChanges();

                for (int i = 1; i <= questionCount; i++)
                {
                    Question question = new Question();
                    int answersCount = ConvertStringToInt(form["Question" + i + "_AnswersCount"]);

                    question.Text = form["Question" + i + "_Text"];
                    question.PollId = newPoll.PollId;

                    db.Questions.Add(question);
                    db.SaveChanges();

                    for (int j = 1; j <= answersCount; j++)
                    {
                        Answer answer = new Answer();

                        answer.Text = form["Question" + i + "_Answer" + j];
                        answer.QuestionId = question.QuestionId;

                        db.Answers.Add(answer);
                        db.SaveChanges();
                    }

                    newPoll.Questions.Add(question);
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

        [NonAction]
        public int ConvertStringToInt(string s)
        {
            try
            {
                return Int32.Parse(s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }
        }

        [NonAction]
        public DateTime ConvertStringToDateTime(string s)
        {
            try
            {
                DateTime dateTime = DateTime.ParseExact(s, "dd-MMM-y h:mm:ss tt", null);
                return dateTime;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return DateTime.Now;
            }
        }

        [NonAction]
        public string ConvertPollToValidQuestionsJson(Poll poll)
        {
            try
            {
                string jsonQuestions;

                jsonQuestions = "[";

                foreach (Question question in poll.Questions)
                {
                    jsonQuestions += CreateJsonQuestion(question);

                    if (question != poll.Questions.Last())
                    {
                        jsonQuestions += ", ";
                    }

                }

                jsonQuestions += "]";

                return jsonQuestions;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        [NonAction]
        public string CreateJsonQuestion(Question question)
        {
            string jsonQuestion;

            jsonQuestion = "{" +
                              Surround("Text") + " : " + Surround(question.Text) + ", " +
                              Surround("Answers") + " : " + CreateJsonAnswers(question.Answers) +
                           "}";

            return jsonQuestion;
        }

        [NonAction]
        public string CreateJsonAnswers(ICollection<Answer> answers)
        {
            string jsonAnswers;

            jsonAnswers = "[";

            foreach (Answer answer in answers)
            {
                jsonAnswers += CreateJsonOneAnswer(answer);

                if (answer != answers.Last())
                {
                    jsonAnswers += ", ";
                }
            }

            jsonAnswers += "]";

            return jsonAnswers;
        }

        [NonAction]
        public string CreateJsonOneAnswer(Answer answer)
        {
            string jsonAnswer = "";

            jsonAnswer = "{" +
                            Surround("Text") + " : " + Surround(answer.Text) +
                         "}";

            return jsonAnswer;
        }

        [NonAction]
        public string Surround(string text)
        {
            return "\"" + text + "\"";
        }
    }
}