﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Livet;
using OnedrawHelper.Data;
using CoreTweet;

namespace OnedrawHelper.Models
{
    public class ThemeModel : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */
        public Theme Source { get; private set; }
        private Challenge _nextChallenge;
        public Challenge NextChallenge
        {
            get { return _nextChallenge; }
            set
            {
                _nextChallenge = value;
                RaisePropertyChanged();
                if (this.ChallengeUpdated != null) this.ChallengeUpdated(this, null);
            }
        }
        public event EventHandler ChallengeUpdated;


        public ThemeModel(Theme source)
        {
            this.Source = source;
        }


        public async Task UpdateNextChallengeAsync(Tokens tokens)
        {
            var c = await GetNextChallengeAsync(tokens);
            if (c != null && (NextChallenge == null || c.StartTime < NextChallenge.StartTime))
                NextChallenge = c;
        }

        public async Task<Challenge> GetNextChallengeAsync(Tokens tokens)
        {
            var statuses = await tokens.Statuses.UserTimelineAsync(screen_name => Source.SourceScreenName, include_rts => false);
            var challenges = statuses.Select(p =>
                new
                {
                    MatchDate = Regex.Match(p.Text, Source.Extractor.DateExtractPattern, RegexOptions.Multiline),
                    MatchSubjects = Regex.Match(p.Text, Source.Extractor.SubjectsExtractPattern, RegexOptions.Multiline),
                    MatchRules = Regex.Match(p.Text, Source.Extractor.RulesExtractPattern, RegexOptions.Multiline),
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

                    var subjects = Regex.Split(p.MatchSubjects.Groups["subjects"].Value, Source.Extractor.SubjectsSplitPattern).Where(q => !string.IsNullOrWhiteSpace(q)).ToList();
                    var rules = Regex.Split(p.MatchRules.Groups["rules"].Value, Source.Extractor.RulesSplitPattern).Where(q => !string.IsNullOrWhiteSpace(q)).ToList();
                    return new Challenge() { StartTime = start, Subjects = subjects, Rules = rules };
                });

            return challenges.OrderBy(p => p.StartTime).FirstOrDefault();
        }

    }
}