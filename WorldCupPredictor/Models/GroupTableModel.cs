using System;
using System.Collections.Generic;

namespace WorldCupPredictor.Models
{
    public class UserAccountTemp
    {
        public string UserName { get; set; }
        public string Deparment { get; set; }
        public string FullName { get; set; }
        public string Company { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public string Error { get; set; }
    }

    public class UserAccount
    {
        public string UserName { get; set; }
        public string Deparment { get; set; }
        public string FullName { get; set; }
        public string Company { get; set; }
    }
    public class GroupTableModel
    {
        public string Stage { get; set; }
        public List<Match> Matches { get; set; }
        public List<MatchCalc> MatchCalcs { get; set; }
    }

    public class RankingModelByUser
    {
        public string UserName { get; set; }
        public int RankPoint { get; set; }
        public int RankMoney { get; set; }
    }   

    public class MatchCalc
    {
        public int MatchId { get; set; }        
        public string StageName { get; set; }        
        public string TeamName { get; set; }
        public string TeamUrl { get; set; }
        public string TeamCountry { get; set; }
        public int MP { get; set; }
        public int W { get; set; }
        public int D { get; set; }
        public int L { get; set; }
        public int GF { get; set; }
        public int GA { get; set; }
        public int GD { get; set; }
        public int Point { get; set; }
    }

    public class PredictionModel
    {
        public DateTime Date { get; set; }
        public List<Match> Matches { get; set; }
    }

    public class ChoosingUser
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public bool IsStar { get; set; }
        public bool IsDollar { get; set; }
        public bool StarDisabled { get; set; }
    }

    public class PredictDetailModel
    {        
        public Match Match { get; set; }
        public List<ChoosingUser> HomeWins { get; set; }
        public List<ChoosingUser> AwayWins { get; set; }
        public List<ChoosingUser> NotPredicts { get; set; }
    }

    public class Match
    {
        public int MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public string HomeName { get; set; }
        public string HomeUrl { get; set; }
        public string HomeCountry { get; set; }
        public float HomeHandicap { get; set; }
        public bool HomeWin { get; set; }
        public string AwayName { get; set; }
        public string AwayUrl { get; set; }
        public string AwayCountry { get; set; }
        public float AwayHandicap { get; set; }
        public bool AwayWin { get; set; }
        public int HomeGoal { get; set; }
        public int AwayGoal { get; set; }
        public string StageName { get; set; }
        public string VenueName { get; set; }
        public string VenueUrl { get; set; }
        public string VenueStadium { get; set; }
        public bool IsOpen { get; set; }
        public bool IsExpire { get; set; }        
        public decimal MoneyPerWin { get; set; }
        public decimal MoneyPerDraw { get; set; }
        public decimal MoneyPerLoose { get; set; }
        public bool IsStar { get; set; }
        public bool IsDollar { get; set; }
        public int WinPoint { get; set; } = 3;
        public int StarPoint { get; set; }
        public int ScorePoint { get; set; }
        public decimal StarMoney { get; set; }
        public decimal Dollar { get; set; }
        public bool StarDisabled { get; set; }
        public bool IsScore { get; set; }
        public int? HomeGoalPre { get; set; }
        public int? AwayGoalPre { get; set; }
    }

    public class UserPredictModel
    {
        public string UserName { get; set; }
        public int Win { get; set; }
        public int WinPercent { get; set; }
        public int Loose { get; set; }
        public int LoosePercent { get; set; }
        public int Draw { get; set; }
        public int DrawPercent { get; set; }
        public decimal CalcMoney { get; set; }
        public int NotPredict { get; set; }
        public UserAccount UserAccount { get; set; }
        public List<UserPredict> UserPredicts { get; set; }
        public StarSummaryModel StarSummay { get; set; }
    }
    public class UserPredict
    {
        public int No { get; set; }        
        public int MatchId { get; set; }        
        public string HomeName { get; set; }        
        public float HomeHandicap { get; set; }        
        public bool HomeWin { get; set; }
        public int HomeGoal { get; set; }
        public string AwayName { get; set; }        
        public bool AwayWin { get; set; }        
        public float AwayHandicap { get; set; }        
        public int AwayGoal { get; set; }        
        public int Point { get; set; }
        public int CalcMoney { get; set; }
        public bool IsExpire { get; set; }
        public bool IsStar { get; set; }
        public bool IsDollar { get; set; }
        public bool StarDisabled { get; set; }
        public bool IsScore { get; set; }
        public int HomeGoalPre { get; set; }
        public int AwayGoalPre { get; set; }

        public bool IsWin
        {
            private set { }
            get
            {
                return IsExpire == true && ((HomeGoal + HomeHandicap) > (AwayGoal + AwayHandicap) && HomeWin) || ((HomeGoal + HomeHandicap) < (AwayGoal + AwayHandicap) && AwayWin);
            }
        }

        public bool IsLoose
        {
            private set { }
            get
            {
                return IsExpire == true && ((HomeGoal + HomeHandicap) < (AwayGoal + AwayHandicap) && HomeWin) || ((HomeGoal + HomeHandicap) > (AwayGoal + AwayHandicap) && AwayWin);
            }
        }

        public bool IsDraw
        {
            private set { }
            get
            {
                return IsExpire == true && (HomeWin==true || AwayWin==true) && ((HomeGoal + HomeHandicap) == (AwayGoal + AwayHandicap));
            }
        }

        public bool IsWinScore
        {
            private set { }
            get
            {
                return IsExpire == true && IsScore==true && (HomeGoal == HomeGoalPre && AwayGoal == AwayHandicap);
            }
        }
    }

    public class RankingModel
    {
        public int No { get; set; }
        public int RankUser { get; set; }
        public int LastRank { get; set; }
        public string UserName { get; set; }
        public string Deparment { get; set; }
        public string FullName { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public int TotalPoint { get; set; }
        public int LastPoint { get; set; }
        public int TotalWin { get; set; }
        public int TotalDraw { get; set; }
        public int TotalPredict { get; set; }
        public int TotalWinScore { get; set; }
        public DateTime LastPredictTime { get; set; }
        public int DiffRank => TotalPoint==0? 0: this.LastRank - this.RankUser;
    }

    public class MyRanking: RankingModel
    {
        public int PointNeedToRank1 { get; set; }
        public int PointNeedToRank24 { get; set; }
    }

    public class PredictionCalcModel
    {
        public string UserName { get; set; }
        public int Point { get; set; }
        public bool HomeWin { get; set; }
        public bool AwayWin { get; set; }
        public decimal CalcMoney { get; set; }
    }

    public class SponsorModel
    {
        public int No { get; set; }
        public int RankUser { get; set; }
        public int LastRank { get; set; }
        public string Deparment { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Company { get; set; }
        public decimal TotalMoney { get; set; }
        public decimal LastTotalMoney { get; set; }
    }    

    public class StarSummaryModel
    {
        public int Total { get; set; } = 3;
        public int Used { get; set; } = 0;
        public int NotUsed => this.Total - this.Used;
    }

    public class Stage
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MatchInStage
    {
        public int MatchId { get; set; }
        public int? HomeTeamId { get; set; }
        public int? AwayTeamId { get; set; }
        public DateTime? KickOff { get; set; }      
    }

    public class SelectItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}