using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Licenta.Models
{
    public class ActivePoll
    {
        [Key]
        public int Pk { get; set; }

        public int PollId { get; set; }
    }
}