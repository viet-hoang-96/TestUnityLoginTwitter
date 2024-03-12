using Cysharp.Threading.Tasks;
using UnityEngine;

public class TestTwitter : MonoBehaviour
{
    public string consumerKey;
    public string consumerSecret;
    public GameObject[] cullingObjects;
    public TwitterLogin TwitterAPI;
    public RectTransform CropRect;
    
    private void Awake()
    {
        TwitterAPI.Init(consumerKey, consumerSecret);
    }

    public void LoginToTwitter()
    {

        TwitterAPI.Login();
    }

    public void SetActiveCullingObjects(bool value)
    {
        foreach (GameObject obj in cullingObjects)
        {
            obj.SetActive(value);
        }
    }

    public void TweetScreenShot()
    {
        SetActiveCullingObjects(false);
        TweetManager.Instance.TweetWithScreenshot(CropRect, () => 
        {
            SetActiveCullingObjects(true);
        }).Forget();
    }
}