using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Licenta.Models
{
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        public string Text { get; set; }

        public int PollId { get; set; }

        public virtual Poll Poll { get; set; }
    }
}