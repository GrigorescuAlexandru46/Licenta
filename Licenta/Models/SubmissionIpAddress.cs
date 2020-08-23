using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Licenta.Models
{
    public class SubmissionIpAddress
    {
        [Key]
        public int Id { get; set; }

        public int PollId { get; set; }

        public string IpAddress { get; set; }
    }
}