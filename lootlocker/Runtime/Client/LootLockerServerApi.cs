using System.Collections.Generic;
using System;
using System.Text;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using Godot;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;

namespace LootLocker.LootLockerEnums
{
    public enum LootLockerCallerRole { User, Admin, Player, Base };
}

namespace LootLocker
{
    [EditorBrowsable()]
    public partial class LootLockerServerApi : Node
    {
        private static LootLockerServerApi _instance;
        private const int MaxRetries = 3;
        private int _tries;
        public Yielder yielder = null;

        public static LootLockerServerApi Instantiate()
        {
            if (_instance == null)
            {
                _instance = new LootLockerServerApi();
            }

            return _instance;
        }

        public static async Task SendRequest(LootLockerServerRequest request, Action<LootLockerResponse> OnServerResponse = null)
        {
            if (_instance == null)
            {
                Instantiate();
            }

            await _instance._SendRequest(request, OnServerResponse);
        }

        private async Task<object> _SendRequest(LootLockerServerRequest request, Action<LootLockerResponse> OnServerResponse = null)
        {
            return await yielder.StartAsyncCoroutine(coroutine());
            async IAsyncEnumerator<object> coroutine()
            {
                //Always wait 1 frame before starting any request to the server to make sure the requester code has exited the main thread.
                yield return null;

                System.Net.Http.HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(LootLockerConfig.current.clientSideRequestTimeOut);

                //Build the URL that we will hit based on the specified endpoint, query params, etc
                string url = BuildUrl(request.endpoint, request.queryParams, request.callerRole);

                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("ServerRequest " + request.httpMethod + " URL: " + url);

                using (HttpRequestMessage webRequest = CreateWebRequest(url, request))
                {
                    bool timedOut = false, completed = false;
                    HttpResponseMessage webResponse = new();
                    long startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    try
                    {
                        webResponse = await client.SendAsync(webRequest);
                        completed = true;
                    }
                    // Filter by InnerException.
                    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                    {
                        timedOut = true;
                    }

                    if (!completed && timedOut)
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("Exceeded maxTimeOut waiting for a response from " + request.httpMethod + " " + url);
                        OnServerResponse?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>(request.endpoint + " timed out."));
                        yield break;
                    }

                    string ResponseContent = await webResponse.Content.ReadAsStringAsync();

                    LogResponse(request, (long)webResponse.StatusCode, ResponseContent, startTime, webResponse.ReasonPhrase);

                    if (WebRequestSucceeded(webResponse) && !timedOut)
                    {
                        OnServerResponse?.Invoke(new LootLockerResponse
                        {
                            statusCode = (int)webResponse.StatusCode,
                            success = true,
                            text = await webResponse.Content.ReadAsStringAsync(),
                            errorData = null
                        });
                        yield break;
                    }

                    if (ShouldRetryRequest((long)webResponse.StatusCode, _tries))
                    {
                        _tries++;
                        RefreshTokenAndCompleteCall(request, (value) => { _tries = 0; OnServerResponse?.Invoke(value); });
                        yield break;
                    }

                    _tries = 0;
                    LootLockerResponse response = new LootLockerResponse
                    {
                        statusCode = (int)webResponse.StatusCode,
                        success = false,
                        text = ResponseContent
                    };

                    try
                    {
                        response.errorData = LootLockerJson.DeserializeObject<LootLockerErrorData>(ResponseContent);
                    }
                    catch (Exception)
                    {
                        if (ResponseContent.StartsWith("<"))
                        {
                            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("JSON Starts with <, info: \n    statusCode: " + response.statusCode + "\n    body: " + response.text);
                        }
                        response.errorData = null;
                    }
                    // Error data was not parseable, populate with what we know
                    if (response.errorData == null)
                    {
                        response.errorData = new LootLockerErrorData((int)webResponse.StatusCode, ResponseContent);
                    }

                    string RetryAfterHeader = webResponse.Headers.RetryAfter?.ToString();
                    if (!string.IsNullOrEmpty(RetryAfterHeader))
                    {
                        response.errorData.retry_after_seconds = Int32.Parse(RetryAfterHeader);
                    }

                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(response.errorData.ToString());
                    OnServerResponse?.Invoke(response);
                }
            }
        }
        #region Private Methods

        private static bool ShouldRetryRequest(long statusCode, int timesRetried)
        {
            return (statusCode == 401 || statusCode == 403) && LootLockerConfig.current.allowTokenRefresh && CurrentPlatform.Get() != Platforms.Steam && timesRetried < MaxRetries;
        }

        private static void LogResponse(LootLockerServerRequest request, long statusCode, string responseBody, float startTime, string webRequestError)
        {
            if (statusCode == 0 && string.IsNullOrEmpty(responseBody) && !string.IsNullOrEmpty(webRequestError))
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Web request failed, request to " +
                    request.endpoint + " completed in " +
                    (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - startTime).ToString("n4") +
                    " secs.\nWeb Request Error: " + webRequestError);
                return;
            }

            try
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Server Response: " +
                    statusCode + " " +
                    request.endpoint + " completed in " +
                    (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - startTime).ToString("n4") +
                    " secs.\nResponse: " +
                    LootLockerObfuscator
                        .ObfuscateJsonStringForLogging(responseBody));
            }
            catch
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.httpMethod.ToString());
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.endpoint);
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(LootLockerObfuscator.ObfuscateJsonStringForLogging(responseBody));
            }
        }

        private static string GetUrl(LootLockerCallerRole callerRole)
        {
            switch (callerRole)
            {
                case LootLockerCallerRole.Admin:
                    return LootLockerConfig.current.adminUrl;
                case LootLockerCallerRole.User:
                    return LootLockerConfig.current.userUrl;
                case LootLockerCallerRole.Player:
                    return LootLockerConfig.current.playerUrl;
                case LootLockerCallerRole.Base:
                    return LootLockerConfig.current.baseUrl;
                default:
                    return LootLockerConfig.current.url;
            }
        }

        private bool WebRequestSucceeded(HttpResponseMessage webResponse)
        {
            return webResponse.IsSuccessStatusCode;
        }

        private static readonly Dictionary<string, string> BaseHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json; charset=UTF-8" },
            { "Content-Type", "application/json; charset=UTF-8" },
            { "Access-Control-Allow-Credentials", "true" },
            { "Access-Control-Allow-Headers", "Accept, X-Access-Token, X-Application-Name, X-Request-Sent-Time" },
            { "Access-Control-Allow-Methods", "GET, POST, DELETE, PUT, OPTIONS, HEAD" },
            { "Access-Control-Allow-Origin", "*" },
            { "LL-Instance-Identifier", System.Guid.NewGuid().ToString() }
        };

        private void RefreshTokenAndCompleteCall(LootLockerServerRequest cachedRequest, Action<LootLockerResponse> onComplete)
        {
            switch (CurrentPlatform.Get())
            {
                case Platforms.Guest:
                    {
                        LootLockerSDKManager.StartGuestSession(async response =>
                        {
                            await CompleteCall(cachedRequest, response, onComplete);
                        });
                        return;
                    }
                case Platforms.WhiteLabel:
                    {
                        LootLockerSDKManager.StartWhiteLabelSession(async response =>
                        {
                            await CompleteCall(cachedRequest, response, onComplete);
                        });
                        return;
                    }
                case Platforms.AppleGameCenter:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshAppleGameCenterSession(async response =>
                            {
                                await CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.AppleSignIn:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshAppleSession(async response =>
                            {
                                await CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.Epic:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshEpicSession(async response =>
                            {
                                await CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.Google:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshGoogleSession(async response =>
                            {
                                await CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.Remote:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshRemoteSession(async response =>
                            {
                                await CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.NintendoSwitch:
                case Platforms.Steam:
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired and token refresh is not supported for {CurrentPlatform.GetFriendlyString()}");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.PlayStationNetwork:
                case Platforms.XboxOne:
                case Platforms.AmazonLuna:
                    {
                        var sessionRequest = new LootLockerSessionRequest(LootLockerConfig.current.deviceID);
                        LootLockerAPIManager.Session(sessionRequest, async (response) =>
                        {
                            await CompleteCall(cachedRequest, response, onComplete);
                        });
                        return;
                    }
                case Platforms.None:
                default:
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"Token refresh for platform {CurrentPlatform.GetFriendlyString()} not supported");
                        onComplete?.Invoke(LootLockerResponseFactory.NetworkError<LootLockerResponse>($"Token refresh for platform {CurrentPlatform.GetFriendlyString()} not supported", 401));
                        return;
                    }
            }
        }

        private static bool ShouldRefreshUsingRefreshToken(LootLockerServerRequest cachedRequest)
        {
            // The failed request isn't a refresh session request but we have a refresh token stored, so try to refresh the session automatically before failing
            return (string.IsNullOrEmpty(cachedRequest.jsonPayload) || !cachedRequest.jsonPayload.Contains("refresh_token")) && !string.IsNullOrEmpty(LootLockerConfig.current.refreshToken);
        }

        private async Task CompleteCall(LootLockerServerRequest cachedRequest, LootLockerSessionResponse sessionRefreshResponse, Action<LootLockerResponse> onComplete)
        {
            if (!sessionRefreshResponse.success)
            {
                LootLockerLogger.GetForLogLevel()("Session refresh failed");
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                return;
            }

            if (cachedRequest.retryCount >= 4)
            {
                LootLockerLogger.GetForLogLevel()("Session refresh failed");
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                return;
            }

            cachedRequest.extraHeaders["x-session-token"] = LootLockerConfig.current.token;
            await _SendRequest(cachedRequest, onComplete);
            cachedRequest.retryCount++;
        }

        private System.Net.Http.HttpRequestMessage CreateWebRequest(string url, LootLockerServerRequest request)
        {
            System.Net.Http.HttpRequestMessage httpRequestMessage;
            switch (request.httpMethod)
            {
                case LootLockerHTTPMethod.UPLOAD_FILE:
                    httpRequestMessage = new(HttpMethod.Post, url);
                    httpRequestMessage.Content = request.form;
                    break;
                case LootLockerHTTPMethod.UPDATE_FILE:
                    // Workaround for WebRequest with PUT HTTP verb not having form fields
                    httpRequestMessage = new(HttpMethod.Put, url);
                    httpRequestMessage.Content = request.form;
                    break;
                case LootLockerHTTPMethod.POST:
                case LootLockerHTTPMethod.PATCH:
                // Defaults are fine for PUT
                case LootLockerHTTPMethod.PUT:

                    if (request.payload == null && request.upload != null)
                    {
                        httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
                        string boundary = "" + DateTime.Now.Ticks.ToString("x");
                        httpRequestMessage.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);

                        StringBuilder sb = new StringBuilder();
                        sb.Append("--");
                        sb.Append(boundary);
                        sb.Append("\r\n");
                        sb.Append("Content-Disposition: form-data; filename=\"");
                        sb.Append(request.uploadName);
                        sb.Append("\"");
                        sb.Append("\r\n");
                        sb.Append("Content-Type: ");
                        sb.Append(request.uploadType);
                        sb.Append("\r\n");
                        sb.Append("\r\n");

                        string postHeader = sb.ToString();
                        byte[] postHeaderBytes = Encoding.UTF8.GetBytes(postHeader);

                        // Build the trailing boundary string as a byte array
                        // ensuring the boundary appears on a line by itself
                        byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

                        long length = postHeaderBytes.Length + request.upload.Length + boundaryBytes.Length;
                        httpRequestMessage.Headers.Add("Content-Length", length.ToString());

                        Stream requestStream = new MemoryStream();
                        // Write out our post header
                        requestStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);
                        // Write out the file contents
                        requestStream.Write(request.upload);
                        // Write out the trailing boundary
                        requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                        httpRequestMessage.Content = new StreamContent(requestStream);
                    }
                    else
                    {
                        string json = (request.payload != null && request.payload.Count > 0) ? LootLockerJson.SerializeObject(request.payload) : request.jsonPayload;
                        GD.Print("Content: {0}", json);
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("REQUEST BODY = " + LootLockerObfuscator.ObfuscateJsonStringForLogging(json));
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(string.IsNullOrEmpty(json) ? "{}" : json);
                        httpRequestMessage = new(request.httpMethod.ToHttpMethod(), url);
                        httpRequestMessage.Content = new ByteArrayContent(bytes);
                    }

                    break;

                case LootLockerHTTPMethod.OPTIONS:
                case LootLockerHTTPMethod.HEAD:
                case LootLockerHTTPMethod.GET:
                    // Defaults are fine for GET
                    httpRequestMessage = new(HttpMethod.Get, url);
                    break;

                case LootLockerHTTPMethod.DELETE:
                    // Defaults are fine for DELETE
                    httpRequestMessage = new(HttpMethod.Delete, url);
                    break;
                default:
                    throw new System.Exception("Invalid HTTP Method");
            }

            if (BaseHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in BaseHeaders)
                {
                    if (pair.Key == "Content-Type" && request.upload != null) continue;

                    httpRequestMessage.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
                }
            }

            if (!string.IsNullOrEmpty(LootLockerConfig.current?.sdk_version))
            {
                httpRequestMessage.Headers.Add("LL-SDK-Version", LootLockerConfig.current.sdk_version);
            }

            if (request.extraHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in request.extraHeaders)
                {
                    httpRequestMessage.Headers.Add(pair.Key, pair.Value);
                }
            }

            return httpRequestMessage;
        }

        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams = null, LootLockerCallerRole callerRole = LootLockerCallerRole.User)
        {
            string ep = endpoint.StartsWith("/") ? endpoint.Trim() : "/" + endpoint.Trim();

            return (GetUrl(callerRole) + ep + GetQueryStringFromDictionary(queryParams)).Trim();
        }

        private string GetQueryStringFromDictionary(Dictionary<string, string> queryDict)
        {
            if (queryDict == null || queryDict.Count == 0) return string.Empty;

            string query = "?";

            foreach (KeyValuePair<string, string> pair in queryDict)
            {
                if (query.Length > 1)
                    query += "&";

                query += pair.Key + "=" + pair.Value;
            }

            return query;
        }
        #endregion
    }
}
