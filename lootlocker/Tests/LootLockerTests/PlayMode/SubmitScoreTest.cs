using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using Godot;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace LootLockerTests.PlayMode
{

    public class SubmitScoreTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;
        private static readonly string GlobalPlayerLeaderboardKey = "gl_player_leaderboard";
        private static readonly string GlobalGenericLeaderboardKey = "gl_generic_leaderboard";

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
                GD.Print("Started a guest session ");
                actualResponse = response;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            if (!actualResponse.success)
            {
                SetupFailed = true;
                yield break;
            }

            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Global Player Leaderboard",
                key = GlobalPlayerLeaderboardKey,
                direction_method = "descending",
                enable_game_api_writes = true,
                has_metadata = true,
                overwrite_score_on_submit = false,
                type = "player"
            };

            bool leaderboardCreated = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                if (!response.success)
                {
                    SetupFailed = true;
                }
                leaderboardCreated = true;
            });
            yield return new WaitUntil(() => leaderboardCreated);

            createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Global Generic Leaderboard",
                key = GlobalGenericLeaderboardKey,
                direction_method = "descending",
                enable_game_api_writes = true,
                has_metadata = true,
                overwrite_score_on_submit = false,
                type = "generic"
            };

            leaderboardCreated = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                if (!response.success)
                {
                    SetupFailed = true;
                }
                leaderboardCreated = true;
            });
            yield return new WaitUntil(() => leaderboardCreated);
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
        public void SubmitScore_SubmitToPlayerLeaderboard_Succeeds_Test()
        {
            new Yielder().StartCoroutine(SubmitScore_SubmitToPlayerLeaderboard_Succeeds());
        }
        public IEnumerator SubmitScore_SubmitToPlayerLeaderboard_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            int submittedScore = (int)new RandomNumberGenerator().RandfRange(0, 100) + 1;

            //When
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, submittedScore, GlobalPlayerLeaderboardKey, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "SubmitScore failed");
            Assert.That(actualResponse.score, Is.EqualTo(submittedScore), "Score was not as submitted");
        }

        [CoroutineTest]
        public void SubmitScore_SubmitToGenericLeaderboard_Succeeds_Test()
        {
            new Yielder().StartCoroutine(SubmitScore_SubmitToGenericLeaderboard_Succeeds());
        }
        public IEnumerator SubmitScore_SubmitToGenericLeaderboard_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string memberID = LootLockerTestConfigurationUtilities.GetRandomNoun() + LootLockerTestConfigurationUtilities.GetRandomVerb();
            int submittedScore = (int)new RandomNumberGenerator().RandfRange(0, 100) + 1;

            //When
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(memberID, submittedScore, GlobalGenericLeaderboardKey, (response) =>
            {

                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "SubmitScore failed");
            Assert.That(actualResponse.score, Is.EqualTo(submittedScore), "Score was not as submitted");
        }

        [CoroutineTest]
        public void SubmitScore_AttemptSubmitOnOverwriteScore_DoesNotUpdateScoreWhenScoreIsLower_Test()
        {
            new Yielder().StartCoroutine(SubmitScore_AttemptSubmitOnOverwriteScore_DoesNotUpdateScoreWhenScoreIsLower());
        }
        public IEnumerator SubmitScore_AttemptSubmitOnOverwriteScore_DoesNotUpdateScoreWhenScoreIsLower()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            var actualScore = (int)new RandomNumberGenerator().RandfRange(2, 100);

            LootLockerSDKManager.SubmitScore(null, actualScore, GlobalPlayerLeaderboardKey, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);
            Assert.IsTrue(actualResponse.success, "Failed to submit score");

            //When
            LootLockerSubmitScoreResponse secondResponse = null;
            bool secondScoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, actualScore - 1, GlobalPlayerLeaderboardKey, (response) =>
            {
                secondResponse = response;
                secondScoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => secondScoreSubmittedCompleted);

            //Then
            Assert.IsTrue(secondResponse.success, "SubmitScore failed");
            Assert.That(secondResponse.score, Is.EqualTo(actualResponse.score), "Score got updated, even though it was smaller");
        }

        [CoroutineTest]
        public void SubmitScore_AttemptSubmitOnOverwriteScore_UpdatesScoreWhenScoreIsHigher_Test()
        {
            new Yielder().StartCoroutine(SubmitScore_AttemptSubmitOnOverwriteScore_UpdatesScoreWhenScoreIsHigher());
        }
        public IEnumerator SubmitScore_AttemptSubmitOnOverwriteScore_UpdatesScoreWhenScoreIsHigher()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            var actualScore = (int)new RandomNumberGenerator().RandfRange(0, 100);

            LootLockerSDKManager.SubmitScore(null, actualScore, GlobalPlayerLeaderboardKey, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);
            Assert.IsTrue(actualResponse.success, "Failed to submit score");

            //When
            LootLockerSubmitScoreResponse secondResponse = null;
            bool secondScoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, actualScore + 1, GlobalPlayerLeaderboardKey, (response) =>
            {
                secondResponse = response;
                secondScoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => secondScoreSubmittedCompleted);

            //Then
            Assert.IsTrue(secondResponse.success, "SubmitScore failed");
            Assert.That(secondResponse.score, Is.EqualTo(actualResponse.score + 1), "Score did not get updated, even though it was higher");
        }

        [CoroutineTest]
        public void SubmitScore_SubmitOnOverwriteScoreWhenOverwriteIsAllowed_UpdatesScore_Test()
        {
            new Yielder().StartCoroutine(SubmitScore_SubmitOnOverwriteScoreWhenOverwriteIsAllowed_UpdatesScore());
        }

        public IEnumerator SubmitScore_SubmitOnOverwriteScoreWhenOverwriteIsAllowed_UpdatesScore()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string leaderboardKey = "overwrites_enabled_leaderboard";
            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Overwrites Enabled Leaderboard",
                key = leaderboardKey,
                direction_method = LootLockerLeaderboardSortDirection.descending.ToString(),
                enable_game_api_writes = true,
                has_metadata = false,
                overwrite_score_on_submit = true,
                type = "player"
            };

            bool leaderboardCreated = false;
            bool leaderboardSuccess = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                leaderboardSuccess = response.success;
                leaderboardCreated = true;

            });
            yield return new WaitUntil(() => leaderboardCreated);

            Assert.IsTrue(leaderboardSuccess, "Failed to create leaderboard");

            LootLockerSubmitScoreResponse actualResponse = null;
            bool submitScoreCompleted = false;
            var actualScore = (int)new RandomNumberGenerator().RandfRange(50, 100);
            LootLockerSDKManager.SubmitScore(null, actualScore, leaderboardKey, (response) =>
            {
                actualResponse = response;
                submitScoreCompleted = true;
            });
            yield return new WaitUntil(() => submitScoreCompleted);
            Assert.IsTrue(actualResponse.success, "Failed to submit score");

            //When
            LootLockerSubmitScoreResponse secondResponse = null;
            bool secondScoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, actualScore - 1, leaderboardKey, (response) =>
            {
                secondResponse = response;
                secondScoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => secondScoreSubmittedCompleted);

            //Then
            Assert.That(secondResponse.score, Is.Not.EqualTo(actualResponse.score), "Score got updated, even though it was smaller");
            Assert.IsTrue(secondResponse.success, "SubmitScore failed");
        }

        [CoroutineTest]
        public void SubmitScore_SubmitToLeaderboardWithMetadata_Succeeds_Test()
        {
            new Yielder().StartCoroutine(SubmitScore_SubmitToLeaderboardWithMetadata_Succeeds());
        }

        public IEnumerator SubmitScore_SubmitToLeaderboardWithMetadata_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string submittedMetadata = "Random Message";

            //When
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;

            LootLockerSDKManager.SubmitScore(null, (int)new RandomNumberGenerator().RandfRange(0, 100), GlobalPlayerLeaderboardKey, submittedMetadata, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "SubmitScore failed");
            Assert.That(actualResponse.metadata, Is.EqualTo(submittedMetadata), "Metadata was not as expected");
        }

        [CoroutineTest]
        public void SubmitScore_SubmitMetadataToLeaderboardWithoutMetadata_IgnoresMetadata_Test()
        {
            new Yielder().StartCoroutine(SubmitScore_SubmitMetadataToLeaderboardWithoutMetadata_IgnoresMetadata());
        }
        public IEnumerator SubmitScore_SubmitMetadataToLeaderboardWithoutMetadata_IgnoresMetadata()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string leaderboardKey = "non_metadata_leaderboard";
            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Non Metadata Leaderboard",
                key = leaderboardKey,
                direction_method = LootLockerLeaderboardSortDirection.descending.ToString(),
                enable_game_api_writes = true,
                has_metadata = false,
                overwrite_score_on_submit = false,
                type = "player"
            };

            bool leaderboardCreated = false;
            bool leaderboardSuccess = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                leaderboardSuccess = response.success;
                leaderboardCreated = true;

            });
            yield return new WaitUntil(() => leaderboardCreated);

            Assert.IsTrue(leaderboardSuccess, "Failed to create leaderboard");

            string submittedMetadata = "Random Message";

            //When
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, (int)new RandomNumberGenerator().RandfRange(0, 100), leaderboardKey, submittedMetadata, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "SubmitScore failed");
            Assert.IsEmpty(actualResponse.metadata, "Metadata was not empty");
        }

    }
}
