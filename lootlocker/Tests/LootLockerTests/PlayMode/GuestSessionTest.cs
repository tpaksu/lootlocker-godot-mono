using System;
using System.Collections;
using Godot;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace LootLockerTests.PlayMode
{
    public class GuestSessionTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;

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
        public IEnumerator StartGuestSession_WithPlayerAsIdentifier_Fails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // string pattern = @"Each player must have a unique identifier assigned";

            // Regex regex = new Regex(pattern);

            // LogAssert.Expect(LogType.Error, regex);

            //Given
            string playerAsIdentifier = "player";

            //When
            LootLockerGuestSessionResponse actualResponse = null;
            bool guestSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(playerAsIdentifier, (response) =>
            {
                actualResponse = response;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);

            //Then
            Assert.IsFalse(actualResponse.success, "Guest Session with 'player' as Identifier started despite it being disallowed");
        }

        [CoroutineTest]
        public IEnumerator StartGuestSession_WithoutIdentifier_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            //When
            bool guestSessionCompleted = false;
            LootLockerGuestSessionResponse actualResponse = null;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                actualResponse = response;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Guest Session without Identifier failed to start");
            Assert.IsFalse(string.IsNullOrEmpty(actualResponse.player_identifier), "No player_identifier found in response");
        }

        [CoroutineTest]
        public IEnumerator StartGuestSession_WithProvidedIdentifier_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string providedIdentifier = Guid.NewGuid().ToString();

            //When
            LootLockerGuestSessionResponse actualResponse = null;
            bool guestSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(providedIdentifier, (response) =>
            {

                actualResponse = response;
                guestSessionCompleted = true;

            });
            yield return new WaitUntil(() => guestSessionCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Guest Session with random Identifier failed to start");
            Assert.That(actualResponse.player_identifier, Is.EqualTo(providedIdentifier), "response player_identifier did not match the expected identifier");
        }

        [CoroutineTest]
        public IEnumerator EndGuestSession_Succeeds()
        {
            //Given
            bool startGuestSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                startGuestSessionCompleted = true;
            });
            yield return new WaitUntil(() => startGuestSessionCompleted);

            LootLockerSessionResponse actualResponse = null;

            //When
            bool endGuestSessionCompleted = false;
            LootLockerSDKManager.EndSession((response) =>
            {
                actualResponse = response;
                endGuestSessionCompleted = true;
            });
            yield return new WaitUntil(() => endGuestSessionCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "GuestSession was not ended correctly");
        }

        [CoroutineTest]
        public IEnumerator StartGuestSession_WithStoredIdentifier_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            LootLockerGuestSessionResponse actualResponse = null;
            string expectedIdentifier = null;
            bool firstSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession((startSessionResponse) =>
            {
                expectedIdentifier = startSessionResponse.player_identifier;

                LootLockerSDKManager.EndSession((endSessionResponse) =>
                {
                    firstSessionCompleted = true;
                });
            });
            yield return new WaitUntil(() => firstSessionCompleted);

            //When
            bool secondSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                actualResponse = response;
                secondSessionCompleted = true;
            });
            yield return new WaitUntil(() => secondSessionCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Guest Session failed to start");
            Assert.That(actualResponse.player_identifier, Is.EqualTo(expectedIdentifier), "Guest Session using stored Identifier failed to work");
        }

    }
}
