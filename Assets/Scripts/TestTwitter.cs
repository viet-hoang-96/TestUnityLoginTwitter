using UnityEngine;
using Toriki;

public class TestTwitter : MonoBehaviour
{
    public void LoginToTwitter()
    {
        TwitterAPI.InitWithLogin(
        (nickname, token, secret) =>
        {
            Debug.Log($"login success: nick name {nickname}, token {token}, secret {secret}");
            // nickname, accesstoken, accesstokensecret is available. 
            // also TwitterAPI is ready now.
        },
        (errorCode, message) =>
        {
            // failed to log in to Twitter.
            Debug.Log($"login error: code: {errorCode} message: {message}");
        }
        );
    }

    public void TweetScreenShot()
    {
        TweetManager.Instance.TweetWithScreenshot();
    }
}