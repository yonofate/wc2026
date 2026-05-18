using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Sendo.FileTransfer.Infracstructure;

namespace WorldCupPredictor.Services
{
    public class Wc2026StoredPrediction
    {
        public string ExternalMatchId { get; set; }
        public string UserName { get; set; }
        public bool HomeWin { get; set; }
        public bool AwayWin { get; set; }
        public int? HomeGoalPre { get; set; }
        public int? AwayGoalPre { get; set; }
    }

    public class Wc2026PredictionRepository
    {
        public async Task<IReadOnlyList<Wc2026StoredPrediction>> GetByUserAsync(string userName)
        {
            const string sql = @"SELECT ExternalMatchId, UserName, HomeWin, AwayWin, HomeGoalPre, AwayGoalPre
FROM dbo.Wc2026_UserPrediction WHERE UserName = @userName";
            using (var cnn = SqlHelper.OpenConnection())
            {
                var rows = await cnn.QueryAsync<Wc2026StoredPrediction>(sql, new { userName }).ConfigureAwait(false);
                return rows.ToList();
            }
        }

        public async Task<IReadOnlyList<Wc2026StoredPrediction>> GetByMatchAsync(string externalMatchId)
        {
            const string sql = @"SELECT ExternalMatchId, UserName, HomeWin, AwayWin, HomeGoalPre, AwayGoalPre
FROM dbo.Wc2026_UserPrediction WHERE ExternalMatchId = @id AND (HomeWin = 1 OR AwayWin = 1)";
            using (var cnn = SqlHelper.OpenConnection())
            {
                var rows = await cnn.QueryAsync<Wc2026StoredPrediction>(sql, new { id = externalMatchId }).ConfigureAwait(false);
                return rows.ToList();
            }
        }

        public async Task<Wc2026StoredPrediction> GetAsync(string externalMatchId, string userName)
        {
            const string sql = @"SELECT ExternalMatchId, UserName, HomeWin, AwayWin, HomeGoalPre, AwayGoalPre
FROM dbo.Wc2026_UserPrediction WHERE ExternalMatchId = @id AND UserName = @userName";
            using (var cnn = SqlHelper.OpenConnection())
            {
                var rows = await cnn.QueryAsync<Wc2026StoredPrediction>(sql, new { id = externalMatchId, userName }).ConfigureAwait(false);
                return rows.FirstOrDefault();
            }
        }

        public async Task UpsertPickAsync(string externalMatchId, string userName, bool homeWin, bool awayWin)
        {
            const string sql = @"MERGE dbo.Wc2026_UserPrediction AS t
USING (SELECT @ExternalMatchId AS ExternalMatchId, @UserName AS UserName) AS s
ON t.ExternalMatchId = s.ExternalMatchId AND t.UserName = s.UserName
WHEN MATCHED THEN UPDATE SET HomeWin = @HomeWin, AwayWin = @AwayWin, UpdatedUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (ExternalMatchId, UserName, HomeWin, AwayWin, HomeGoalPre, AwayGoalPre, UpdatedUtc)
VALUES (@ExternalMatchId, @UserName, @HomeWin, @AwayWin, NULL, NULL, SYSUTCDATETIME());";
            using (var cnn = SqlHelper.OpenConnection())
            {
                await cnn.ExecuteAsync(sql, new { ExternalMatchId = externalMatchId, UserName = userName, HomeWin = homeWin, AwayWin = awayWin }).ConfigureAwait(false);
            }
        }

        public async Task UpsertScoreAsync(string externalMatchId, string userName, int homeGoal, int awayGoal)
        {
            const string sql = @"MERGE dbo.Wc2026_UserPrediction AS t
USING (SELECT @ExternalMatchId AS ExternalMatchId, @UserName AS UserName) AS s
ON t.ExternalMatchId = s.ExternalMatchId AND t.UserName = s.UserName
WHEN MATCHED THEN UPDATE SET HomeGoalPre = @HomeGoalPre, AwayGoalPre = @AwayGoalPre, UpdatedUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (ExternalMatchId, UserName, HomeWin, AwayWin, HomeGoalPre, AwayGoalPre, UpdatedUtc)
VALUES (@ExternalMatchId, @UserName, 0, 0, @HomeGoalPre, @AwayGoalPre, SYSUTCDATETIME());";
            using (var cnn = SqlHelper.OpenConnection())
            {
                await cnn.ExecuteAsync(sql, new
                {
                    ExternalMatchId = externalMatchId,
                    UserName = userName,
                    HomeGoalPre = homeGoal,
                    AwayGoalPre = awayGoal
                }).ConfigureAwait(false);
            }
        }

        public static bool IsDatabaseConfigured()
        {
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["DefaultConnection"];
                return cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString);
            }
            catch
            {
                return false;
            }
        }
    }
}
