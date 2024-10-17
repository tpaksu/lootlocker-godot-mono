using Godot;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace LootLockerTests.PlayMode
{
    public class LeaderboardDetailsTest
    {

        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;
        private string leaderboardKey;

        [SetUp]
        public IEnumerator Setup()
        {
            TestCounter++;
            configCopy = LootLockerConfig.current;

            if (!LootLockerConfig.ClearSettings())
            {
                GD.PrintErr("Could not clear LootLocker config");
            }

            // Create game
            bool gameCreationCallCompleted = false;
            LootLockerTestGame.CreateGame(testName: "GuestSessionTest" + TestCounter + " ", onComplete: (success, errorMessage, game) =>
            {
                if (!success)
                {
                    gameCreationCallCompleted = true;
                    GD.PrintErr(errorMessage);
                    SetupFailed = true;
                }
                gameUnderTest = game;
                gameCreationCallCompleted = true;
            });
            yield return new WaitUntil(() => gameCreationCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }
            gameUnderTest?.SwitchToStageEnvironment();

            // Enable guest platform
            bool enableGuestLoginCallCompleted = false;
            gameUnderTest?.EnableGuestLogin((success, errorMessage) =>
            {
                if (!success)
                {
                    GD.PrintErr(errorMessage);
                    SetupFailed = true;
                }
                enableGuestLoginCallCompleted = true;
            });
            yield return new WaitUntil(() => enableGuestLoginCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }
            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Successfully created test game and initialized LootLocker");

            bool guestSessionCompleted = false;
            LootLockerGuestSessionResponse actualResponse = null;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                actualResponse = response;
                SetupFailed |= !actualResponse.success;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            if (SetupFailed)
            {
                yield break;
            }

            leaderboardKey = "gl_leaderboard";
            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Global Leaderboard",
                key = leaderboardKey,
                direction_method = LootLockerLeaderboardSortDirection.descending.ToString(),
                enable_game_api_writes = true,
                has_metadata = true,
                overwrite_score_on_submit = false,
                type = "player"
            };

            bool leaderboardCreated = false;
            bool leaderboardSuccess = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                leaderboardSuccess = response.success;
                SetupFailed |= !leaderboardSuccess;
                leaderboardCreated = true;

            });
            yield return new WaitUntil(() => leaderboardCreated);

            Assert.IsTrue(leaderboardSuccess, "Failed to create leaderboard");
        }

        [TearDown]
        public IEnumerator TearDown()
        {
            if (gameUnderTest != null)
            {
                bool gameDeletionCallCompleted = false;
                gameUnderTest.DeleteGame(((success, errorMessage) =>
                {
                    if (!success)
                    {
                        GD.PrintErr(errorMessage);
                    }

                    gameUnderTest = null;
                    gameDeletionCallCompleted = true;
                }));
                yield return new WaitUntil(() => gameDeletionCallCompleted);
            }

            LootLockerConfig.CreateNewSettings(configCopy.apiKey, configCopy.game_version, configCopy.domainKey,
                configCopy.currentDebugLevel, configCopy.allowTokenRefresh);
        }

        [CoroutineTest]
        public IEnumerator Leaderboard_ListSchedule_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string expectedCronExpression = "0 0 * * *";
            var request = new UpdateLootLockerLeaderboardScheduleRequest
            {
                cron_expression = expectedCronExpression
            };

            bool leaderboardScheduleUpdated = false;
            var leaderboard = gameUnderTest.GetLeaderboardByKey(leaderboardKey);
            leaderboard.UpdateLeaderboardSchedule(request, (success, errorMessage, response) =>
            {
                leaderboardScheduleUpdated = true;
            });
            yield return new WaitUntil(() => leaderboardScheduleUpdated);

            //When
            LootLockerLeaderboardDetailResponse actualResponse = null;
            bool leaderboardDataCompleted = false;
            LootLockerSDKManager.GetLeaderboardData(leaderboardKey, (response) =>
            {
                actualResponse = response;
                leaderboardDataCompleted = true;
            });
            yield return new WaitUntil(()=> leaderboardDataCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Could not get leaderboardData");
            Assert.That(actualResponse.schedule.cron_expression, Is.EqualTo(expectedCronExpression), "The submitted cron expression was not set on the leaderboard");
        }

        [CoroutineTest]
        public IEnumerator Leaderboard_ListRewards_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Give
            bool leaderboardRewardUpdateComplete = false;
            bool leaderboardRewardUpdateSuccess = false;
            var leaderboard = gameUnderTest.GetLeaderboardByKey(leaderboardKey);
            leaderboard.AddLeaderboardReward((response) =>
            {
                leaderboardRewardUpdateSuccess = response.success;
                leaderboardRewardUpdateComplete = true;

            });
            yield return new WaitUntil(() => leaderboardRewardUpdateComplete);

            Assert.IsTrue(leaderboardRewardUpdateSuccess, "Failed to create reward");

            //When
            LootLockerLeaderboardDetailResponse actualResponse = null;
            bool leaderboardDataCompleted = false;
            LootLockerSDKManager.GetLeaderboardData(leaderboardKey, (response) =>
            {
                actualResponse = response;
                leaderboardDataCompleted = true;
            });
            yield return new WaitUntil(() => leaderboardDataCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Could not get leaderboardData");
            Assert.IsNotEmpty(actualResponse.rewards, "No rewards found!");
        }

    }
}
