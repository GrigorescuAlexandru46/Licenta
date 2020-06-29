using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Licenta.Models
{
    public class Submission
    {
        [Key]
        [Column(Order = 1)]
        public int SubmissionId { get; set; }

        [Key]
        [Column(Order = 2)] 
        public int AnswerId { get; set; }

        public string Text { get; set; }

        public int QuestionType { get; set; }

        public int PollId { get; set; }

        public DateTime SubmitDate { get; set; }

        [ForeignKey("AnswerId")]
        public virtual Answer Answer { get; set; }
    }
}