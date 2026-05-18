using System;
using System.Collections.Generic;

namespace WorldCupPredictor.Models
{
    public class Wc2026DayGroup
    {
        public DateTime Date { get; set; }
        public List<Wc2026MatchVm> Matches { get; set; }
    }

    public class Wc2026MatchVm
    {
        public string Id { get; set; }
        public DateTime KickoffUtc { get; set; }
        public string Status { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public string HomeCrestUrl { get; set; }
        public string AwayCrestUrl { get; set; }
        public string StageLabel { get; set; }

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        public bool UserHomeWin { get; set; }
        public bool UserAwayWin { get; set; }
        public int? UserHomeGoalPre { get; set; }
        public int? UserAwayGoalPre { get; set; }

        public bool IsFinished =>
            string.Equals(Status, "FINISHED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Status, "AWARDED", StringComparison.OrdinalIgnoreCase);

        public bool IsPredictionClosed =>
            IsFinished
            || string.Equals(Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Status, "LIVE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Status, "IN_PLAY", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Status, "PAUSED", StringComparison.OrdinalIgnoreCase)
            || DateTime.UtcNow >= KickoffUtc;
    }

    public class Wc2026DetailVm
    {
        public Wc2026MatchVm Match { get; set; }
        public int HomePickCount { get; set; }
        public int AwayPickCount { get; set; }
        public int TotalPickCount { get; set; }
    }

    public class Wc2026UserSummaryVm
    {
        public string UserName { get; set; }
        public int Win { get; set; }
        public int Draw { get; set; }
        public int Loose { get; set; }
        public int WinPercent { get; set; }
        public int DrawPercent { get; set; }
        public int LoosePercent { get; set; }
        public int NotPredict { get; set; }
        public List<Wc2026UserRowVm> Rows { get; set; }
    }

    public class Wc2026UserRowVm
    {
        public int No { get; set; }
        public string MatchId { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public bool HomeWin { get; set; }
        public bool AwayWin { get; set; }
        public int? HomeGoalPre { get; set; }
        public int? AwayGoalPre { get; set; }
        public int? HomeGoal { get; set; }
        public int? AwayGoal { get; set; }
        public bool IsExpire { get; set; }

        public bool IsWin =>
            IsExpire && HomeGoal.HasValue && AwayGoal.HasValue
            && (((HomeGoal.Value > AwayGoal.Value) && HomeWin) || ((HomeGoal.Value < AwayGoal.Value) && AwayWin));

        public bool IsLoose =>
            IsExpire && HomeGoal.HasValue && AwayGoal.HasValue
            && (((HomeGoal.Value < AwayGoal.Value) && HomeWin) || ((HomeGoal.Value > AwayGoal.Value) && AwayWin));

        public bool IsDraw =>
            IsExpire && (HomeWin || AwayWin) && HomeGoal.HasValue && AwayGoal.HasValue && HomeGoal.Value == AwayGoal.Value;

        public bool IsWinScore =>
            IsExpire && HomeGoal.HasValue && AwayGoal.HasValue
            && HomeGoalPre.HasValue && AwayGoalPre.HasValue
            && HomeGoal == HomeGoalPre && AwayGoal == AwayGoalPre;
    }
}
