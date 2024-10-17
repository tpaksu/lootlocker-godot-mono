﻿using System;
using System.Collections;
using Godot;
using LootLocker;


namespace LootLockerTestConfigurationUtils
{
    #region HTTP Interface
    public class LootLockerAdminRequest
    {
        public static int ActiveGameId;
        private static int _retries = 0;
        private static readonly int MaxRetries = 10;

        public static void Send(string endPoint, LootLockerHTTPMethod httpMethod, string json, Action<LootLockerResponse> onComplete, bool useAuthToken)
        {
            if (_retries > MaxRetries)
            {
                _retries = 0;
                onComplete?.Invoke(new LootLockerResponse{statusCode = 0, success = false, text = "Request exceeded the allowed number of retries", errorData = new LootLockerErrorData{ message = "Request exceeded the allowed number of retries" } });
                return;
            }
            LootLockerConfig.DebugLevel debugLevelSavedState = LootLockerConfig.current.currentDebugLevel;
            LootLockerConfig.current.currentDebugLevel = LootLockerConfig.DebugLevel.AllAsNormal;

            endPoint = endPoint.Replace("#GAMEID#", ActiveGameId.ToString());

            LootLockerServerRequest.CallAPI(endPoint, httpMethod, json, onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
                    if (!serverResponse.success && serverResponse.errorData.retry_after_seconds > 0)
                    {
                        LootLockerCIRetry go = new LootLockerCIRetry();
                        _retries++;
                        go.Retry(serverResponse.errorData.retry_after_seconds.Value, endPoint, httpMethod, json, onComplete, useAuthToken);
                    }
                    LootLockerConfig.current.currentDebugLevel = debugLevelSavedState;
                },
                useAuthToken,
                callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }



    }

    public partial class LootLockerCIRetry : Node
    { 
        public Yielder yielder = null;
        public void Retry(int retryAfter, string endPoint, LootLockerHTTPMethod httpMethod, string json,
            Action<LootLockerResponse> onComplete, bool useAuthToken)
        { 
            yielder.StartCoroutine(RetrySendAfter(retryAfter, endPoint, httpMethod, json, onComplete, useAuthToken));
        }
        private IEnumerator RetrySendAfter(int retryAfter, string endPoint, LootLockerHTTPMethod httpMethod,
            string json, Action<LootLockerResponse> onComplete, bool useAuthToken)
        {
            yield return new WaitForSeconds(retryAfter);
            LootLockerAdminRequest.Send(endPoint, httpMethod, json, onComplete, useAuthToken);
            this.Free();
        }
    }

    #endregion



    #region Endpoints
    public class LootLockerTestConfigurationEndpoints
    {
        public static EndPointClass LoginEndpoint = new EndPointClass("v1/session", LootLockerHTTPMethod.POST);
        public static EndPointClass SignupEndpoint = new EndPointClass("v1/signup", LootLockerHTTPMethod.POST);

        public static EndPointClass CreateGame = new EndPointClass("v1/game", LootLockerHTTPMethod.POST);
        public static EndPointClass DeleteGame = new EndPointClass("v1/game/#GAMEID#", LootLockerHTTPMethod.DELETE);

        public static EndPointClass CreateKey = new EndPointClass("game/#GAMEID#/api_keys", LootLockerHTTPMethod.POST);

        public static EndPointClass UpdatePlatform = new EndPointClass("game/#GAMEID#/platforms/{0}", LootLockerHTTPMethod.PUT);

        public static EndPointClass createLeaderboard = new EndPointClass("game/#GAMEID#/leaderboards", LootLockerHTTPMethod.POST);
        public static EndPointClass updateLeaderboard = new EndPointClass("game/#GAMEID#/leaderboards/{0}", LootLockerHTTPMethod.PUT);
        public static EndPointClass updateLeaderboardSchedule = new EndPointClass("game/#GAMEID#/leaderboard/{0}/schedule", LootLockerHTTPMethod.POST);
        public static EndPointClass addLeaderboardReward = new EndPointClass("game/#GAMEID#/leaderboard/{0}/reward", LootLockerHTTPMethod.POST);

        public static EndPointClass getAssetContexts = new EndPointClass("/v1/game/#GAMEID#/assets/contexts", LootLockerHTTPMethod.GET);
        public static EndPointClass createAsset = new EndPointClass("/v1/game/#GAMEID#/asset", LootLockerHTTPMethod.POST);
        public static EndPointClass createReward = new EndPointClass("game/#GAMEID#/reward", LootLockerHTTPMethod.POST);
    }
    #endregion
}
