using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System;

public class TwitterLogin : MonoBehaviour
{
    [System.Serializable]
    public class TwitterUser
    {
        public string id_str;
        // Add other properties you want to deserialize from the JSON response
    }

    private string _consumerKey = "9wOMHhKvovMCjDujbj0kCsYPw";
    private string _consumerSecret = "mvVt0NzfPugYTnkwTUCbikv7hMTSpyJP5eq2oSfS9IGSOKb6S6";

    string requestTokenURL = "https://api.twitter.com/oauth/request_token";
    string authorizeURL = "https://api.twitter.com/oauth/authorize";
    string accessTokenURL = "https://api.twitter.com/oauth/access_token";
    string verifyCredentialsURL = "https://api.twitter.com/1.1/account/verify_credentials.json";

    string oauthToken;
    string oauthVerifier;
    string oauthTokenSecret;

    public void Init(string consumerKey, string consumerSecret)
    {
        _consumerKey = consumerKey;
        _consumerSecret = consumerSecret;
    }

    public void Login()
    {
        StartCoroutine(CoLogin());
    }

    IEnumerator CoLogin()
    {
        // Step 1: Get a request token
        string authorizationHeader = GetAuthorizationHeader(requestTokenURL, "POST", null);
        Debug.Log($"Authorization Header: {authorizationHeader}");
        UnityWebRequest request = UnityWebRequest.PostWwwForm(requestTokenURL, "");
        request.SetRequestHeader("Authorization", authorizationHeader);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            Dictionary<string, string> responseParams = ParseResponseParameters(response);

            oauthToken = responseParams["oauth_token"];
            oauthTokenSecret = responseParams["oauth_token_secret"];

            // Step 2: Open popup window for Twitter authorization
            OpenPopup(authorizeURL + "?oauth_token=" + oauthToken);
        }
        else
        {
            Debug.LogError("Request Token Request Failed: " + request.error);
        }
    }

    void OpenPopup(string url)
    {
        Application.ExternalEval($"window.open('{url}', '_blank', 'width=600,height=400')");
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Check for OAuth verifier when application regains focus
        if (hasFocus)
        {
            StartCoroutine(CheckForOAuthVerifier());
        }
    }

    IEnumerator CheckForOAuthVerifier()
    {
        // Step 3: Poll for OAuth verifier
        while (string.IsNullOrEmpty(oauthVerifier))
        {
            yield return null;
        }

        // Step 4: Exchange request token and oauth_verifier for access token
        StartCoroutine(ExchangeRequestTokenForAccessToken());
    }

    IEnumerator ExchangeRequestTokenForAccessToken()
    {
        string authorizationHeader = GetAuthorizationHeader(accessTokenURL, "POST", new Dictionary<string, string>
        {
            { "oauth_verifier", oauthVerifier }
        });
        UnityWebRequest request = UnityWebRequest.PostWwwForm(accessTokenURL, "");
        request.SetRequestHeader("Authorization", authorizationHeader);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            Dictionary<string, string> responseParams = ParseResponseParameters(response);

            string accessToken = responseParams["oauth_token"];
            string accessTokenSecret = responseParams["oauth_token_secret"];

            // Step 5: Use access token to verify credentials and get user information
            StartCoroutine(GetTwitterUserID(accessToken, accessTokenSecret));
        }
        else
        {
            Debug.LogError("Access Token Request Failed: " + request.error);
        }
    }

    IEnumerator GetTwitterUserID(string accessToken, string accessTokenSecret)
    {
        string authorizationHeader = GetAuthorizationHeader(verifyCredentialsURL, "GET", null, accessToken, accessTokenSecret);
        UnityWebRequest request = UnityWebRequest.Get(verifyCredentialsURL);
        request.SetRequestHeader("Authorization", authorizationHeader);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            var userData = JsonConvert.DeserializeObject<TwitterUser>(response);

            Debug.Log("Twitter User ID: " + userData.id_str);
        }
        else
        {
            Debug.LogError("Verify Credentials Request Failed: " + request.error);
        }
    }

    string GetAuthorizationHeader(string url, string method, Dictionary<string, string> parameters, string token = null, string tokenSecret = null)
    {
        // Construct OAuth parameters
        Dictionary<string, string> oauthParams = new Dictionary<string, string>
        {
            { "oauth_consumer_key", _consumerKey },
            { "oauth_signature_method", "HMAC-SHA1" },
            { "oauth_timestamp", ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString() },
            { "oauth_nonce", Guid.NewGuid().ToString("N") },
            { "oauth_version", "1.0" }
        };

        // Add token if available
        if (!string.IsNullOrEmpty(token))
            oauthParams.Add("oauth_token", token);

        // Combine all parameters (OAuth and additional)
        Dictionary<string, string> allParams = new Dictionary<string, string>(oauthParams);
        if (parameters != null)
        {
            foreach (var kvp in parameters)
            {
                if (!allParams.ContainsKey(kvp.Key))
                    allParams.Add(kvp.Key, kvp.Value);
            }
        }

        // Sort parameters alphabetically by key
        List<string> sortedParams = new List<string>();
        foreach (var kvp in allParams.OrderBy(p => p.Key))
        {
            sortedParams.Add($"{UnityWebRequest.EscapeURL(kvp.Key)}={UnityWebRequest.EscapeURL(kvp.Value)}");
        }

        // Concatenate parameters
        string paramString = string.Join("&", sortedParams.ToArray());

        // Construct signature base string
        string signatureBaseString = $"{method.ToUpper()}&{UnityWebRequest.EscapeURL(url)}&{UnityWebRequest.EscapeURL(paramString)}";

        // Construct signing key
        string signingKey = $"{UnityWebRequest.EscapeURL(_consumerSecret)}&{UnityWebRequest.EscapeURL(tokenSecret ?? "")}";

        // Calculate signature
        HMACSHA1 hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(signingKey));
        byte[] hashBytes = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(signatureBaseString));
        string oauthSignature = Convert.ToBase64String(hashBytes);

        // Add signature to OAuth parameters
        oauthParams.Add("oauth_signature", UnityWebRequest.EscapeURL(oauthSignature));

        // Construct authorization header
        List<string> headerParams = new List<string>();
        foreach (var kvp in oauthParams)
        {
            headerParams.Add($"{kvp.Key}=\"{kvp.Value}\"");
        }

        return "OAuth " + string.Join(", ", headerParams.ToArray());
    }

    Dictionary<string, string> ParseResponseParameters(string response)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        string[] keyValuePairs = response.Split('&');
        foreach (string pair in keyValuePairs)
        {
            string[] parts = pair.Split('=');
            parameters.Add(parts[0], parts[1]);
        }

        return parameters;
    }

    Dictionary<string, string> ParseQueryString(string url)
    {
        Dictionary<string, string> queryParams = new Dictionary<string, string>();

        string queryString = Regex.Match(url, @"\?(.*)$").Groups[1].Value;
        string[] keyValuePairs = queryString.Split('&');
        foreach (string pair in keyValuePairs)
        {
            string[] parts = pair.Split('=');
            queryParams.Add(parts[0], parts[1]);
        }

        return queryParams;
    }
}