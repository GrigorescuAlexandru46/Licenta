using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Licenta.Models
{
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        public int QuestionType { get; set; }

        public string Text { get; set; }

        [ForeignKey("Poll")]
        public int PollId { get; set; }

        public virtual Poll Poll { get; set; }
        public virtual ICollection<Answer> Answers { get; set; }
    }
}