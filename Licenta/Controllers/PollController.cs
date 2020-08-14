using Licenta.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

            var pollsState = new Dictionary<int, bool>();

            foreach (Poll poll in pollsList)
            {
                pollsState.Add(poll.PollId, PollIsActive(poll));
            }

            ViewBag.PollsList = pollsList;
            ViewBag.PollsState = pollsState;

            if (!UserIsAdmin())
            {
                return View();
            }
            else
            {
                TempData["Message"] = "Only an admin can access that page";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Show(int id)
        {
            Poll poll = db.Polls.Find(id);

            ViewBag.UserIsAdmin = UserIsAdmin();
            ViewBag.UserIsProfileOwner = UserIsProfileOwner(poll.Profile);

            ViewBag.PollIsActive = PollIsActive(poll);

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"].ToString();
            }
            return View(poll);
        }

        [NonAction]
        public bool PollIsActive(Poll poll)
        {
            var activePollList = (from activePoll in db.ActivePolls
                                   where activePoll.PollId == poll.PollId
                                   select activePoll
                                    ).ToList();

            return activePollList.Count > 0 ? true : false;
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
                    question.QuestionType = ConvertStringToInt(form["Question" + i + "_Type"]);
                    question.PollId = poll.PollId;

                    db.Questions.Add(question);
                    db.SaveChanges();

                    if (question.QuestionType != 3)
                    {
                        for (int j = 1; j <= answersCount; j++)
                        {
                            Answer answer = new Answer();

                            answer.Text = form["Question" + i + "_Answer" + j];
                            answer.QuestionId = question.QuestionId;

                            db.Answers.Add(answer);
                            db.SaveChanges();
                        }
                    }
                    else
                    {
                        Answer answer = new Answer();

                        answer.Text = "CustomAnswerText";
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
                    question.QuestionType = ConvertStringToInt(form["Question" + i + "_Type"]);
                    question.PollId = newPoll.PollId;

                    db.Questions.Add(question);
                    db.SaveChanges();

                    if (question.QuestionType != 3)
                    {
                        for (int j = 1; j <= answersCount; j++)
                        {
                            Answer answer = new Answer();

                            answer.Text = form["Question" + i + "_Answer" + j];
                            answer.QuestionId = question.QuestionId;

                            db.Answers.Add(answer);
                            db.SaveChanges();
                        }
                    }
                    else
                    {
                        Answer answer = new Answer();

                        answer.Text = "CustomAnswerText";
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

        public ActionResult ConfirmStart(int id)
        {
            Poll poll = db.Polls.Find(id);

            if (UserIsAdmin() || UserIsProfileOwner(poll.Profile))
            {
                return View(poll);
            }
            else
            {
                TempData["Message"] = "Only the creator can start the poll";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult ConfirmStop(int id)
        {
            Poll poll = db.Polls.Find(id);

            if (UserIsAdmin() || UserIsProfileOwner(poll.Profile))
            {
                return View(poll);
            }
            else
            {
                TempData["Message"] = "Only the creator can stop the poll";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public ActionResult Start(int id)
        {
            Poll poll = db.Polls.Find(id);

            if (UserIsAdmin() || UserIsProfileOwner(poll.Profile))
            {
                ActivePoll activePoll = new ActivePoll();

                activePoll.PollId = poll.PollId;
                db.ActivePolls.Add(activePoll);
                db.SaveChanges();

                return RedirectToAction("FinishStart", "Poll", new { id = poll.PollId });
            }
            else
            {
                TempData["Message"] = "Only the creator can start the poll";
                return RedirectToAction("Index", "Home");
            } 
        }

        [HttpPost]
        public ActionResult Stop(int id)
        {
            Poll poll = db.Polls.Find(id);


            if (UserIsAdmin() || UserIsProfileOwner(poll.Profile))
            {
                var foundActivePoll = (from activePoll in db.ActivePolls
                                      where activePoll.PollId == id
                                      select activePoll
                                     ).FirstOrDefault();

                db.ActivePolls.Remove(foundActivePoll);
                db.SaveChanges();

                return RedirectToAction("FinishStop", "Poll", new { id = poll.PollId });
            }
            else
            {
                TempData["Message"] = "Only the creator can stop the poll";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult FinishStart(int id)
        {
            Poll poll = db.Polls.Find(id);

            if (UserIsAdmin() || UserIsProfileOwner(poll.Profile))
            {
                return View(poll);
            }
            else
            {
                TempData["Message"] = "Only the creator can start the poll";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult FinishStop(int id)
        {
            Poll poll = db.Polls.Find(id);

            if (UserIsAdmin() || UserIsProfileOwner(poll.Profile))
            {
                return View(poll);
            }
            else
            {
                TempData["Message"] = "Only the creator can stop the poll";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Fill(int id)
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

        [HttpPost]
        public ActionResult Fill(Poll poll, FormCollection form)
        {
            try
            {
                int questionCount = ConvertStringToInt(form["QuestionCount"]);
                int submissionId = GetNextAvailableSubmissionId();

                for (int i = 1; i <= questionCount; i++)
                {
                    int questionType = ConvertStringToInt(form["Question" + i + "_Type"]);

                    if (questionType == 1)
                    {
                        int answerCount = ConvertStringToInt(form["Question" + i + "_AnswerCount"]);
                        string answerText = form["Question" + i + "_Answer"];

                        for (int j = 1; j <= answerCount; j++)
                        {
                            if (answerText.Equals(form["Question" + i + "_Answer" + j + "_Text"]))
                            {
                                Submission submission = new Submission();

                                submission.SubmissionId = submissionId;
                                submission.AnswerId = ConvertStringToInt(form["Question" + i + "_Answer" + j + "_Id"]);
                                submission.SubmitDate = DateTime.Now;
                                submission.Text = answerText;
                                submission.QuestionType = questionType;
                                submission.PollId = poll.PollId;

                                db.Submissions.Add(submission);
                                break;
                            }
                        } 
                    }
                    else if (questionType == 2)
                    {
                        int answerCount = ConvertStringToInt(form["Question" + i + "_AnswerCount"]);

                        for (int j = 1; j <= answerCount; j++)
                        {
                            if (form.AllKeys.Contains("Question" + i + "_Answer" + j + "_Checkbox")) // if checked
                            {
                                Submission submission = new Submission();

                                submission.SubmissionId = submissionId;
                                submission.AnswerId = ConvertStringToInt(form["Question" + i + "_Answer" + j + "_Id"]);
                                submission.SubmitDate = DateTime.Now;
                                submission.Text = form["Question" + i + "_Answer" + j + "_Text"];
                                submission.QuestionType = questionType;
                                submission.PollId = poll.PollId;

                                db.Submissions.Add(submission);
                            }
                        } 
                    }
                    else if (questionType == 3)
                    {
                        Submission submission = new Submission();

                        submission.SubmissionId = submissionId;
                        submission.AnswerId = ConvertStringToInt(form["Question" + i + "_Answer_Id"]);
                        submission.SubmitDate = DateTime.Now;
                        submission.Text = form["Question" + i + "_CustomAnswer"];
                        submission.QuestionType = questionType;
                        submission.PollId = poll.PollId;

                        db.Submissions.Add(submission);
                    }
                }

                db.SaveChanges();

                TempData["Message"] = "Answers successfully sent!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                TempData["Message"] = "Error. Answers could not be sent!";
                return RedirectToAction("Fill", "Poll", new { id = poll.PollId });
            }
        }

        public ActionResult Results(int id)
        {
            Poll poll = db.Polls.Find(id);

            if(UserIsAdmin() || UserIsProfileOwner(poll.Profile))
            {
                var submissionList = (from submissionEntity in db.Submissions
                                      where submissionEntity.PollId == id
                                      select submissionEntity
                                     ).ToList();

                var answerSelectedCountMap = new Dictionary<int, int>();
                foreach (Question question in poll.Questions)
                {
                    foreach (Answer answer in question.Answers)
                    {
                         answerSelectedCountMap.Add(answer.AnswerId, 0);   
                    }
                }

                foreach(Submission submission in submissionList)
                {
                    answerSelectedCountMap[submission.AnswerId]++;
                }

                var customAnswerMap = new Dictionary<int, Dictionary<string, int>>();
                foreach (Question question in poll.Questions)
                {
                    if (question.QuestionType == 3)
                    {
                        Dictionary<string, int> answerDetailsMap = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

                        foreach (var submission in submissionList)
                        {
                            if (submission.Answer.Question.QuestionId == question.QuestionId)
                            {
                                string answerText = submission.Text;

                                if (answerDetailsMap.ContainsKey(answerText))
                                {
                                    answerDetailsMap[answerText]++;
                                }
                                else
                                {
                                    answerDetailsMap.Add(answerText, 1);
                                }
                            }
                        }

                        //var sortedCustomAnswerMap = from entry in customAnswerMap orderby entry.Value descending select entry;
                        var sortedAnswerDetailsMap = answerDetailsMap.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                        //customAnswerMap.Add(question.QuestionId, answerDetailsMap);
                        customAnswerMap.Add(question.QuestionId, sortedAnswerDetailsMap);
                    }
                }

                Dictionary<int, List<Submission>> groupedSubmissions = new Dictionary<int, List<Submission>>();
                foreach (Submission submission in submissionList)
                {
                    if (groupedSubmissions.ContainsKey(submission.SubmissionId))
                    {
                        groupedSubmissions[submission.SubmissionId].Add(submission);
                    }
                    else
                    {
                        groupedSubmissions.Add(submission.SubmissionId, new List<Submission>() { submission });
                    }
                }

                Dictionary<Tuple<int, int>, int> answerCombinationCountList = new Dictionary<Tuple<int, int>, int>();
                foreach (KeyValuePair<int, List<Submission>> entry in groupedSubmissions)
                {
                    List<Submission> groupedSubmissionList = entry.Value;

                    foreach (Submission submission1 in groupedSubmissionList)
                    {
                        foreach (Submission submission2 in groupedSubmissionList)
                        {
                            if (submission1.AnswerId != submission2.AnswerId && submission1.Answer.QuestionId != submission2.Answer.QuestionId)
                            {
                                Tuple<int, int> combination = new Tuple<int, int>(submission1.AnswerId, submission2.AnswerId);

                                if (answerCombinationCountList.ContainsKey(combination)) {
                                    answerCombinationCountList[combination]++;
                                }
                                else
                                {
                                    answerCombinationCountList.Add(combination, 1);
                                }
                            }
                        }
                    }
                }
                
                ViewBag.AnswerSelectedCountMap = answerSelectedCountMap;
                ViewBag.SubmissionList = submissionList;
                ViewBag.CustomAnswerMap = customAnswerMap;
                ViewBag.AnswerCombinationCountList = answerCombinationCountList;

                return View(poll);
            }
            else
            {
                TempData["Message"] = "Only the creator can see the results of the poll";
                return RedirectToAction("Index", "Home");
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
        public int GetNextAvailableSubmissionId()
        {
            var querySubmissionIds = from submission in db.Submissions
                                     select submission.SubmissionId;

            if (querySubmissionIds.Any())
            {
                return ConvertStringToInt(querySubmissionIds.Max().ToString()) + 1;
            }

            return 1;
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