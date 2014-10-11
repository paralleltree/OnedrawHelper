using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CoreTweet;

namespace OnedrawHelper.Data
{
    public class Theme
    {
        public string Name { get; set; }
        public Challenge NextChallenge { get; set; }
        public string SourceScreenName { get; set; }
        public Extractor Extractor { get; set; }
        public string HashTag { get; set; }

        public Challenge GetNextChallenge(Tokens tokens)
        {
            var statuses = tokens.Statuses.UserTimeline(screen_name => SourceScreenName, include_rts => false);
            var challenges = statuses.Select(p =>
                new
                {
                    MatchDate = Regex.Match(p.Text, Extractor.DateExtractPattern, RegexOptions.Multiline),
                    MatchSubjects = Regex.Match(p.Text, Extractor.SubjectsExtractPattern, RegexOptions.Multiline),
                    MatchRules = Regex.Match(p.Text, Extractor.RulesExtractPattern, RegexOptions.Multiline),
                    CreatedAt = p.CreatedAt
                })
                .Where(p => p.MatchDate.Success)
                .Select(p =>
                {
                    DateTime start = new DateTime(
                        p.CreatedAt.Year,
                        p.MatchDate.Groups["month"].Value != "" ? int.Parse(p.MatchDate.Groups["month"].Value) : p.CreatedAt.Month,
                        p.MatchDate.Groups["day"].Value != "" ? int.Parse(p.MatchDate.Groups["day"].Value) : p.CreatedAt.Day,
                        int.Parse(p.MatchDate.Groups["hour"].Value),
                        int.Parse(p.MatchDate.Groups["min"].Value), 0);
                    if (start < DateTime.Now) start += TimeSpan.FromDays(1);

                    var subjects = Regex.Split(p.MatchSubjects.Groups["subjects"].Value, Extractor.SubjectsSplitPattern).Where(q => !string.IsNullOrWhiteSpace(q)).ToList();
                    var rules = Regex.Split(p.MatchRules.Groups["rules"].Value, Extractor.RulesSplitPattern).Where(q => !string.IsNullOrWhiteSpace(q)).ToList();
                    return new Challenge() { StartTime = start, Subjects = subjects, Rules = rules };
                });

            return challenges.OrderBy(p => p.StartTime).FirstOrDefault();
        }
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
