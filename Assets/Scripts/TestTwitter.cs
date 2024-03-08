using UnityEngine;

public class TestTwitter : MonoBehaviour
{
    public string consumerKey;
    public string consumerSecret;

    public TwitterLogin TwitterAPI;
    
    private void Awake()
    {
        TwitterAPI.Init(consumerKey, consumerSecret);
    }

    public void LoginToTwitter()
    {

        TwitterAPI.Login();
    }

    public void TweetScreenShot()
    {
        TweetManager.Instance.TweetWithScreenshot();
    }
}