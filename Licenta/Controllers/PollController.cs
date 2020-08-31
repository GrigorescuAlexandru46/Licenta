using Licenta.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                poll.Description = form["Description"];
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
                newPoll.Description = form["Description"];
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
            ViewBag.PollIsActive = PollIsActive(poll);
            ViewBag.PollIpAdresses = GetPollIpAdresses(poll);

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
                                submission.Latitude = double.Parse(form["GeoBytesLatitude"]);
                                submission.Longitude = double.Parse(form["GeoBytesLongitude"]);
                                submission.Country = form["GeoBytesCountry"];
                                submission.City = form["GeoBytesCity"];

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
                                submission.Latitude = double.Parse(form["GeoBytesLatitude"]);
                                submission.Longitude = double.Parse(form["GeoBytesLongitude"]);
                                submission.Country = form["GeoBytesCountry"];
                                submission.City = form["GeoBytesCity"];

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
                        submission.Latitude = double.Parse(form["GeoBytesLatitude"]);
                        submission.Longitude = double.Parse(form["GeoBytesLongitude"]);
                        submission.Country = form["GeoBytesCountry"];
                        submission.City = form["GeoBytesCity"];

                        db.Submissions.Add(submission);
                    }
                }

                SubmissionIpAddress submissionIpAddress = new SubmissionIpAddress();
                submissionIpAddress.PollId = poll.PollId;
                submissionIpAddress.IpAddress = form["GeoBytesIpAddress"];
                db.SubmissionIpAddresses.Add(submissionIpAddress);

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

        
        [NonAction]
        public Dictionary<string, int> createInnerCustomAnswerMap(Question question)
        {
            Dictionary<string, int> innerCustomAnswerMap = new Dictionary<string, int>();

            List<Submission> selectedCustomAnswers = (from sub in db.Submissions
                                         where sub.QuestionType == 3 && sub.Answer.Question.QuestionId == question.QuestionId
                                         select sub
                                        ).ToList();

            foreach (Submission sub in selectedCustomAnswers)
            {
                bool foundSimilarString = false;

                foreach (KeyValuePair<string, int> entry in innerCustomAnswerMap)
                {
                    if (StringsAreSimilar(sub.Text, entry.Key))
                    {
                        innerCustomAnswerMap[entry.Key]++;
                        foundSimilarString = true;
                        break;
                    }
                }

                if (foundSimilarString == false)
                {
                    innerCustomAnswerMap.Add(FormatString(sub.Text), 1);
                }
            }

            return innerCustomAnswerMap;
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
                        customAnswerMap.Add(question.QuestionId, createInnerCustomAnswerMap(question));
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

                Dictionary<Tuple<int, int>, int> answerCombinationCountMap = new Dictionary<Tuple<int, int>, int>();
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

                                if (answerCombinationCountMap.ContainsKey(combination)) {
                                    answerCombinationCountMap[combination]++;
                                }
                                else
                                {
                                    answerCombinationCountMap.Add(combination, 1);
                                }
                            }
                        }
                    }
                }

                foreach (Question question1 in poll.Questions)
                {
                    if (question1.QuestionType != 3)
                    {
                        foreach (Answer answer1 in question1.Answers)
                        {
                            foreach (Question question2 in poll.Questions)
                            {
                                if (question2.QuestionType != 3)
                                {
                                    foreach (Answer answer2 in question2.Answers)
                                    {
                                        if (question1.QuestionId != question2.QuestionId && answer1.AnswerId != answer2.AnswerId)
                                        {
                                            Tuple<int, int> combination = new Tuple<int, int>(answer1.AnswerId, answer2.AnswerId);

                                            if (!answerCombinationCountMap.ContainsKey(combination))
                                            {
                                                answerCombinationCountMap.Add(combination, 0);
                                            }
                                        }
                                    }
                                    
                                }
                            }
                        }
                    }
                    
                }


                int submissionCount = (from sub in db.Submissions
                                      where sub.PollId == poll.PollId
                                      group sub by sub.SubmissionId into groupedSubs
                                      select groupedSubs
                                     ).Count();

                Dictionary<int, double> answerPercentageMap = new Dictionary<int, double>();
                foreach (Question question in poll.Questions)
                {
                    foreach (Answer answer in question.Answers)
                    {
                        if (question.QuestionType != 3)
                        {
                            double percentage = (double)answerSelectedCountMap[answer.AnswerId] / submissionCount * 100;
                            answerPercentageMap.Add(answer.AnswerId, percentage);
                        }
                    }
                }

                Dictionary<int, Dictionary<string, double>> customAnswerPercentageMap = new Dictionary<int, Dictionary<string, double>>();
                foreach (KeyValuePair<int, Dictionary<string, int>> entry in customAnswerMap)
                {
                    Dictionary<string, double> innerDictionary = new Dictionary<string, double>();
                    foreach(KeyValuePair<string, int> innerEntry in entry.Value)
                    {
                        innerDictionary.Add(innerEntry.Key, (double)innerEntry.Value / submissionCount * 100);
                    }

                    customAnswerPercentageMap.Add(entry.Key, innerDictionary);
                }

                Dictionary<Tuple<int, int>, double> answerCombinationPercentageMap = new Dictionary<Tuple<int, int>, double>();
                foreach (KeyValuePair<Tuple<int, int>, int> entry in answerCombinationCountMap)
                {
                    Tuple<int, int> combination = new Tuple<int, int>(entry.Key.Item1, entry.Key.Item2);
                    double percentage = (double)entry.Value / submissionCount * 100;

                    answerCombinationPercentageMap.Add(combination, percentage);
                }

                Dictionary<Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>>> answerObjectsMapChart = new Dictionary<Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>>>();
                foreach (KeyValuePair<int, List<Submission>> entry in groupedSubmissions)
                {
                    Submission submission = entry.Value.ElementAt(0);
                    double latitude = Math.Round(submission.Latitude, 3);
                    double longitude = Math.Round(submission.Longitude, 3);

                    Tuple<double, double> centerCoords = null;
                    bool foundCenter = false;
                    foreach (KeyValuePair<Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>>> coordsEntry in answerObjectsMapChart)
                    {
                        if (Math.Abs(coordsEntry.Key.Item1 - latitude) <= 4 && Math.Abs(coordsEntry.Key.Item2 - longitude) <= 4)
                        {
                            centerCoords = new Tuple<double, double>(coordsEntry.Key.Item1, coordsEntry.Key.Item2);
                            foundCenter = true;
                            break;
                        }
                    }

                    // if we didnt find anything valid in the foreach above, create new center
                    if (foundCenter == false)
                    {
                        centerCoords = new Tuple<double, double>(latitude, longitude);
                        answerObjectsMapChart.Add(centerCoords, new Dictionary<int, Dictionary<string, Dictionary<string, object>>>());
                    }

                    Dictionary<int, Dictionary<string, Dictionary<string, object>>> innerDictionary = answerObjectsMapChart[centerCoords];
                    foreach (Submission sub in entry.Value)
                    {
                        if (sub.QuestionType != 3)
                        {
                            if (innerDictionary.ContainsKey(sub.Answer.QuestionId))
                            {
                                Dictionary<string, Dictionary<string, object>> extraInnerDictionary = innerDictionary[sub.Answer.QuestionId];

                                if (extraInnerDictionary.ContainsKey(sub.Text))
                                {
                                    extraInnerDictionary[sub.Text]["value"] = (int)extraInnerDictionary[sub.Text]["value"] + 1;
                                    extraInnerDictionary[sub.Text]["country"] = sub.Country;
                                    extraInnerDictionary[sub.Text]["city"] = sub.City;
                                }
                                else
                                {
                                    extraInnerDictionary.Add(sub.Text, new Dictionary<string, object>());
                                    extraInnerDictionary[sub.Text]["value"] = 1;
                                    extraInnerDictionary[sub.Text]["country"] = sub.Country;
                                    extraInnerDictionary[sub.Text]["city"] = sub.City;
                                }
                            }
                            else
                            {
                                Dictionary<string, Dictionary<string, object>> extraInnerDictionary = new Dictionary<string, Dictionary<string, object>>();
                                extraInnerDictionary.Add(sub.Text, new Dictionary<string, object>());
                                extraInnerDictionary[sub.Text]["value"] = 1;
                                extraInnerDictionary[sub.Text]["country"] = sub.Country;
                                extraInnerDictionary[sub.Text]["city"] = sub.City;

                                innerDictionary.Add(sub.Answer.QuestionId, extraInnerDictionary);
                            }
                        }
                    }
                }

                foreach (KeyValuePair<Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>>> entry in answerObjectsMapChart)
                {
                    foreach (Question question in poll.Questions)
                    {
                        if (question.QuestionType != 3)
                        {
                            foreach (Answer answer in question.Answers)
                            {
                                if (!entry.Value[question.QuestionId].ContainsKey(answer.Text))
                                {
                                    entry.Value[question.QuestionId].Add(answer.Text, new Dictionary<string, object>());
                                    entry.Value[question.QuestionId][answer.Text]["value"] = 0;
                                    entry.Value[question.QuestionId][answer.Text]["country"] = "None";
                                    entry.Value[question.QuestionId][answer.Text]["city"] = "None";
                                }
                            }
                        }
                    }
                }

                int submissionsCounter = 0;
                Dictionary<string, int> submissionDatesMap = new Dictionary<string, int>();
                foreach (KeyValuePair<int, List<Submission>> entry in groupedSubmissions)
                {
                    string submitDateString = entry.Value.First().SubmitDate.ToString("yyyy-MM-dd HH:mm");
                    submissionsCounter++;

                    if (submissionDatesMap.ContainsKey(submitDateString))
                    {
                        submissionDatesMap[submitDateString] = submissionsCounter;
                    }
                    else
                    {
                        submissionDatesMap.Add(submitDateString, submissionsCounter);
                    }   
                }

                ViewBag.SubmissionCount = submissionCount;
                ViewBag.AnswerSelectedCountMap = answerSelectedCountMap;
                ViewBag.SubmissionList = submissionList;
                ViewBag.CustomAnswerMap = customAnswerMap;
                ViewBag.AnswerCombinationCountList = answerCombinationCountMap;
                ViewBag.AnswerPercentageMap = answerPercentageMap;
                ViewBag.CustomAnswerPercentageMap = customAnswerPercentageMap;
                ViewBag.AnswerCombinationPercentageMap = answerCombinationPercentageMap;
                ViewBag.AnswerObjectsMapChart = answerObjectsMapChart;
                ViewBag.SubmissionDatesMap = submissionDatesMap;

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

        [HttpPost]
        public ActionResult UpdateResults(int id)
        {
            Poll poll = db.Polls.Find(id);

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

            foreach (Submission submission in submissionList)
            {
                answerSelectedCountMap[submission.AnswerId]++;
            }

            var customAnswerMap = new Dictionary<int, Dictionary<string, int>>();
            foreach (Question question in poll.Questions)
            {
                if (question.QuestionType == 3)
                {
                    customAnswerMap.Add(question.QuestionId, createInnerCustomAnswerMap(question));
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

                            if (answerCombinationCountList.ContainsKey(combination))
                            {
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

            foreach (Question question1 in poll.Questions)
            {
                if (question1.QuestionType != 3)
                {
                    foreach (Answer answer1 in question1.Answers)
                    {
                        foreach (Question question2 in poll.Questions)
                        {
                            if (question2.QuestionType != 3)
                            {
                                foreach (Answer answer2 in question2.Answers)
                                {
                                    if (question1.QuestionId != question2.QuestionId && answer1.AnswerId != answer2.AnswerId)
                                    {
                                        Tuple<int, int> combination = new Tuple<int, int>(answer1.AnswerId, answer2.AnswerId);

                                        if (!answerCombinationCountList.ContainsKey(combination))
                                        {
                                            answerCombinationCountList.Add(combination, 0);
                                        }
                                    }
                                }

                            }
                        }
                    }
                }

            }

            int submissionCount = (from sub in db.Submissions
                                   where sub.PollId == poll.PollId
                                   group sub by sub.SubmissionId into groupedSubs
                                   select groupedSubs
                                     ).Count();

            Dictionary<int, double> answerPercentageMap = new Dictionary<int, double>();
            foreach (Question question in poll.Questions)
            {
                foreach (Answer answer in question.Answers)
                {
                    if (question.QuestionType != 3)
                    {
                        double percentage = (double)answerSelectedCountMap[answer.AnswerId] / submissionCount * 100;
                        answerPercentageMap.Add(answer.AnswerId, percentage);
                    }
                }
            }

            Dictionary<int, Dictionary<string, double>> customAnswerPercentageMap = new Dictionary<int, Dictionary<string, double>>();
            foreach (KeyValuePair<int, Dictionary<string, int>> entry in customAnswerMap)
            {
                Dictionary<string, double> innerDictionary = new Dictionary<string, double>();
                foreach (KeyValuePair<string, int> innerEntry in entry.Value)
                {
                    innerDictionary.Add(innerEntry.Key, (double)innerEntry.Value / submissionCount * 100);
                }

                customAnswerPercentageMap.Add(entry.Key, innerDictionary);
            }

            Dictionary<Tuple<int, int>, double> answerCombinationPercentageMap = new Dictionary<Tuple<int, int>, double>();
            foreach (KeyValuePair<Tuple<int, int>, int> entry in answerCombinationCountList)
            {
                Tuple<int, int> combination = new Tuple<int, int>(entry.Key.Item1, entry.Key.Item2);
                double percentage = (double)entry.Value / submissionCount * 100;

                answerCombinationPercentageMap.Add(combination, percentage);
            }

            Dictionary<Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>>> answerObjectsMapChart = new Dictionary<Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>>>();
            foreach (KeyValuePair<int, List<Submission>> entry in groupedSubmissions)
            {
                Submission submission = entry.Value.ElementAt(0);
                double latitude = Math.Round(submission.Latitude, 3);
                double longitude = Math.Round(submission.Longitude, 3);

                Tuple<double, double> centerCoords = null;
                bool foundCenter = false;
                foreach (KeyValuePair<Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>>> coordsEntry in answerObjectsMapChart)
                {
                    if (Math.Abs(coordsEntry.Key.Item1 - latitude) <= 4 && Math.Abs(coordsEntry.Key.Item2 - longitude) <= 4)
                    {
                        centerCoords = new Tuple<double, double>(coordsEntry.Key.Item1, coordsEntry.Key.Item2);
                        foundCenter = true;
                        break;
                    }
                }

                // if we didnt find anything valid in the foreach above, create new center
                if (foundCenter == false)
                {
                    centerCoords = new Tuple<double, double>(latitude, longitude);
                    answerObjectsMapChart.Add(centerCoords, new Dictionary<int, Dictionary<string, Dictionary<string, object>>>());
                }

                Dictionary<int, Dictionary<string, Dictionary<string, object>>> innerDictionary = answerObjectsMapChart[centerCoords];
                foreach (Submission sub in entry.Value)
                {
                    if (sub.QuestionType != 3)
                    {
                        if (innerDictionary.ContainsKey(sub.Answer.QuestionId))
                        {
                            Dictionary<string, Dictionary<string, object>> extraInnerDictionary = innerDictionary[sub.Answer.QuestionId];

                            if (extraInnerDictionary.ContainsKey(sub.Text))
                            {
                                extraInnerDictionary[sub.Text]["value"] = (int)extraInnerDictionary[sub.Text]["value"] + 1;
                                extraInnerDictionary[sub.Text]["country"] = sub.Country;
                                extraInnerDictionary[sub.Text]["city"] = sub.City;
                            }
                            else
                            {
                                extraInnerDictionary.Add(sub.Text, new Dictionary<string, object>());
                                extraInnerDictionary[sub.Text]["value"] = 1;
                                extraInnerDictionary[sub.Text]["country"] = sub.Country;
                                extraInnerDictionary[sub.Text]["city"] = sub.City;
                            }
                        }
                        else
                        {
                            Dictionary<string, Dictionary<string, object>> extraInnerDictionary = new Dictionary<string, Dictionary<string, object>>();
                            extraInnerDictionary.Add(sub.Text, new Dictionary<string, object>());
                            extraInnerDictionary[sub.Text]["value"] = 1;
                            extraInnerDictionary[sub.Text]["country"] = sub.Country;
                            extraInnerDictionary[sub.Text]["city"] = sub.City;

                            innerDictionary.Add(sub.Answer.QuestionId, extraInnerDictionary);
                        }
                    }
                }
            }

            foreach (KeyValuePair<Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>>> entry in answerObjectsMapChart)
            {
                foreach (Question question in poll.Questions)
                {
                    if (question.QuestionType != 3)
                    {
                        foreach (Answer answer in question.Answers)
                        {
                            if (!entry.Value[question.QuestionId].ContainsKey(answer.Text))
                            {
                                entry.Value[question.QuestionId].Add(answer.Text, new Dictionary<string, object>());
                                entry.Value[question.QuestionId][answer.Text]["value"] = 0;
                                entry.Value[question.QuestionId][answer.Text]["country"] = "None";
                                entry.Value[question.QuestionId][answer.Text]["city"] = "None";
                            }
                        }
                    }
                }
            }

            int submissionsCounter = 0;
            Dictionary<string, int> submissionDatesMap = new Dictionary<string, int>();
            foreach (KeyValuePair<int, List<Submission>> entry in groupedSubmissions)
            {
                string submitDateString = entry.Value.First().SubmitDate.ToString("yyyy-MM-dd HH:mm");
                submissionsCounter++;

                if (submissionDatesMap.ContainsKey(submitDateString))
                {
                    submissionDatesMap[submitDateString] = submissionsCounter;
                }
                else
                {
                    submissionDatesMap.Add(submitDateString, submissionsCounter);
                }
            }

            string submissionDatesMapJson = "{";
            foreach (KeyValuePair<string, int> entry in submissionDatesMap)
            {
                submissionDatesMapJson += Surround(entry.Key) + ": " + entry.Value;

                if (entry.Key != submissionDatesMap.Last().Key)
                {
                    submissionDatesMapJson += ", ";
                }
            }
            submissionDatesMapJson += "}";

            // Answers count
            string answerSelectedCountMapJson = "{";
            foreach (KeyValuePair<int, int> entry in answerSelectedCountMap)
            {
                answerSelectedCountMapJson += Surround(entry.Key) + ": " + entry.Value;

                if (entry.Key != answerSelectedCountMap.Last().Key)
                {
                    answerSelectedCountMapJson += ", ";
                }
            }
            answerSelectedCountMapJson += "}";

            // Answers percentage
            string answerPercentageMapJson = "{";
            foreach (KeyValuePair<int, double> entry in answerPercentageMap)
            {
                answerPercentageMapJson += Surround(entry.Key) + ": " + entry.Value;

                if (entry.Key != answerPercentageMap.Last().Key)
                {
                    answerPercentageMapJson += ", ";
                }
            }
            answerPercentageMapJson += "}";

            // Combinations count
            string answerCombinationCountListJson = "{";
            foreach (KeyValuePair<Tuple<int, int>, int> entry in answerCombinationCountList)
            {
                answerCombinationCountListJson += Surround(entry.Key.Item1 + "-" + entry.Key.Item2) + ": " + entry.Value;

                if (entry.Key != answerCombinationCountList.Last().Key)
                {
                    answerCombinationCountListJson += ", ";
                }
            }
            answerCombinationCountListJson += "}";

            // Custom answers count
            string customAnswerMapJson = "{";
            foreach (KeyValuePair<int, Dictionary<string, int>> entry in customAnswerMap)
            {
                customAnswerMapJson += Surround(entry.Key) + ": {";
                foreach (KeyValuePair<string, int> internalEntry in entry.Value)
                {
                    customAnswerMapJson += Surround(internalEntry.Key) + ": " + internalEntry.Value;

                    if (internalEntry.Key != entry.Value.Last().Key)
                    {
                        customAnswerMapJson += ", ";
                    }
                }
                customAnswerMapJson += "}";

                if (entry.Key != customAnswerMap.Last().Key)
                {
                    customAnswerMapJson += ", ";
                }
            }
            customAnswerMapJson += "}";

            // Custom answers percentage
            string customAnswerPercentageMapJson = "{";
            foreach (KeyValuePair<int, Dictionary<string, double>> entry in customAnswerPercentageMap)
            {
                customAnswerPercentageMapJson += Surround(entry.Key) + ": {";
                foreach (KeyValuePair<string, double> internalEntry in entry.Value)
                {
                    customAnswerPercentageMapJson += Surround(internalEntry.Key) + ": " + internalEntry.Value;

                    if (internalEntry.Key != entry.Value.Last().Key)
                    {
                        customAnswerPercentageMapJson += ", ";
                    }
                }
                customAnswerPercentageMapJson += "}";

                if (entry.Key != customAnswerPercentageMap.Last().Key)
                {
                    customAnswerPercentageMapJson += ", ";
                }
            }
            customAnswerPercentageMapJson += "}";

            // Combinations percentage
            string answerCombinationPercentageMapJson = "{";
            foreach (KeyValuePair<Tuple<int, int>, double> entry in answerCombinationPercentageMap)
            {
                answerCombinationPercentageMapJson += Surround(entry.Key.Item1 + "-" + entry.Key.Item2) + ": " + entry.Value;

                if (entry.Key != answerCombinationPercentageMap.Last().Key)
                {
                    answerCombinationPercentageMapJson += ", ";
                }
            }
            answerCombinationPercentageMapJson += "}";


            //Dictionary < Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>> >
            string answerObjectsMapChartJson = "{";
            foreach(KeyValuePair<Tuple<double, double>, Dictionary<int, Dictionary<string, Dictionary<string, object>>>> entry in answerObjectsMapChart)
            {
                answerObjectsMapChartJson += Surround("(" + entry.Key.Item1 + "," + entry.Key.Item2 + ")") + ": {";

                Dictionary<int, Dictionary<string, Dictionary<string, object>>> innerDictionary = answerObjectsMapChart[entry.Key];
                foreach (KeyValuePair<int, Dictionary<string, Dictionary<string, object>>> innerEntry in innerDictionary)
                {
                    answerObjectsMapChartJson += Surround(innerEntry.Key) + ": {";

                    Dictionary<string, Dictionary<string, object>> extraInnerDictionary = innerDictionary[innerEntry.Key];
                    foreach (KeyValuePair<string, Dictionary<string, object>> extraInnerEntry in extraInnerDictionary)
                    {
                        answerObjectsMapChartJson += Surround(extraInnerEntry.Key) + ": {";

                        Dictionary<string, object> superExtraInnerDictionary = extraInnerDictionary[extraInnerEntry.Key];
                        answerObjectsMapChartJson += Surround("value") + ": " + superExtraInnerDictionary["value"] + ",";
                        answerObjectsMapChartJson += Surround("country") + ": " + Surround((string)superExtraInnerDictionary["country"]) + ",";
                        answerObjectsMapChartJson += Surround("city") + ": " + Surround((string)superExtraInnerDictionary["city"]);

                        answerObjectsMapChartJson += "}";
                        if (extraInnerEntry.Key != extraInnerDictionary.Last().Key)
                        {
                            answerObjectsMapChartJson += ",";
                        }
                    }

                    answerObjectsMapChartJson += "}";
                    if (innerEntry.Key != innerDictionary.Last().Key)
                    {
                        answerObjectsMapChartJson += ",";
                    }
                }

                answerObjectsMapChartJson += "}";
                if (entry.Key != answerObjectsMapChart.Last().Key)
                {
                    answerObjectsMapChartJson += ",";
                }
            }

            answerObjectsMapChartJson += "}";

            try
            {
                // Combine all jsons into one
                string json = "{" +
                                Surround("submissionCount") + ": " + submissionCount + ", " +
                                Surround("answerSelectedCountMapJson") + ": " + answerSelectedCountMapJson + ", " +
                                Surround("answerCombinationCountListJson") + ": " + answerCombinationCountListJson + ", " +
                                Surround("customAnswerMapJson") + ": " + customAnswerMapJson + ", " +
                                Surround("answerPercentageMapJson") + ": " + answerPercentageMapJson + ", " +
                                Surround("customAnswerPercentageMapJson") + ": " + customAnswerPercentageMapJson + ", " +
                                Surround("answerCombinationPercentageMapJson") + ": " + answerCombinationPercentageMapJson + ", " +
                                Surround("answerObjectsMapChartJson") + ": " + answerObjectsMapChartJson + ", " +
                                Surround("submissionDatesMapJson") + ": " + submissionDatesMapJson +
                              "}";

                return Json(json);

            } catch (Exception e)
            {
                Console.WriteLine(e);
                TempData["Message"] = "An error occured while trying to update the results.";
                return RedirectToAction("Index", "Home");
            }
        }

        [NonAction]
        public bool IpExistsInPoll(string ipAddress, Poll poll)
        {
            var submissionsWithGivenIp = (from sub in db.SubmissionIpAddresses
                                          where sub.PollId == poll.PollId && sub.IpAddress == ipAddress
                                          select sub
                                         ).ToList();

            return submissionsWithGivenIp.Count > 0 ? true : false;
        }

        [NonAction]
        public List<string> GetPollIpAdresses(Poll poll)
        {
            var pollIpAdresses = (from sub in db.SubmissionIpAddresses
                                          where sub.PollId == poll.PollId
                                          select sub.IpAddress
                                         ).ToList();

            return pollIpAdresses;
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
        public string FormatString(string s)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            s = s.Trim();
            s = textInfo.ToTitleCase(s.ToLower());

            return s;
        }

        [NonAction]
        public bool StringsAreSimilar(string s1, string s2)
        {
            //s1 = "Michael JacksonSmith";
            //s2 = "JacksonMichaelSmith";;

            if (string.Equals(s1, s2)) {
                return true;
            }
            if (string.Equals(s1.ToLower(), s2.ToLower()))
            {
                return true;
            }

            char[] separators = { ' ', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}', '\n', '/', '\\', '\t', '|', '!', '?', '`', '~', '@', '#', '$', '%', '^', '&', '*', '\"' };
            string[] separatorsString = { " ", ",", ".", ";", ":", "(", ")", "[", "]", "{", "}", "\n", "/", "\\", "\t", "|", "!", "?", "`", "~", "@", "#", "$", "%", "^", "&", "*", "\"" };

            string stringWithNoSeparators1 = s1.ToLower();
            string stringWithNoSeparators2 = s2.ToLower();
            foreach (string c in separatorsString)
            {
                stringWithNoSeparators1 = stringWithNoSeparators1.Replace(c, string.Empty);
                stringWithNoSeparators2 = stringWithNoSeparators2.Replace(c, string.Empty);
            }

            if (string.Equals(stringWithNoSeparators1, stringWithNoSeparators2)) {
                return true;
            }

            string[] tokens1 = s1.ToLower().Split(separators);
            string[] tokens2 = s2.ToLower().Split(separators);
            bool fail1 = false;
            bool fail2 = false;

            foreach (string token1 in tokens1)
            {
                if (!stringWithNoSeparators2.Contains(token1))
                {
                    fail1 = true;
                }
            }

            foreach (string token2 in tokens2)
            {
                if (!stringWithNoSeparators1.Contains(token2))
                {
                    fail2 = true;
                }
            }

            if (fail1 && fail2)
            {
                return false;
            }

            return true;
        }

        public static int ComputeLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
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

        [NonAction]
        public string Surround(int text)
        {
            return "\"" + text + "\"";
        }
    }
}