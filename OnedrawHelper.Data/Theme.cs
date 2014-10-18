using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace OnedrawHelper.Data
{
    public class Theme
    {
        public string Name { get; set; }
        public string SourceScreenName { get; set; }
        public Extractor Extractor { get; set; }
        public string HashTag { get; set; }
    }

    public class Extractor
    {
        public string DateExtractPattern { get; set; }
        public string SubjectsExtractPattern { get; set; }
        public string SubjectsSplitPattern { get; set; }
        public string RulesExtractPattern { get; set; }
        public string RulesSplitPattern { get; set; }
    }
}
