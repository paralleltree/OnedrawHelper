using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedrawHelper.Data
{
    public class Challenge
    {
        public DateTime StartTime { get; set; }
        public IEnumerable<string> Subjects { get; set; }
        public IEnumerable<string> Rules { get; set; }
    }
}
