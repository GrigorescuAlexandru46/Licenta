using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Licenta.Models
{
    public class Poll
    {
        [Key]
        public int PollId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        [ForeignKey("Profile")]
        public int OwnerId { get; set; }

        public DateTime CreationDate { get; set; }

        public virtual ICollection<Question> Questions { get; set; }
        public virtual Profile Profile { get; set; }
    }
}