using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedrawHelper.Data
{
    public class Theme
    {
        public string Name { get; set; }
        public Challenge NextChallenge { get; set; }
        public string SourceScreenName { get; set; }
        public string ExtractionPattern { get; set; }
        public string HashTag { get; set; }
    }
}
