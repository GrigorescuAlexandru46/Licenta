using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Licenta.Models
{
    public class Answer
    {
        [Key]
        public int AnswerId { get; set; }

        public string Text { get; set; }

        [ForeignKey("Question")]
        public int QuestionId { get; set; }

        public virtual Question Question { get; set; }
        public virtual ICollection<Submission> Submissions { get; set; }

    }
}