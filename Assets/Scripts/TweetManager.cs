using System;
using System.Collections;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Networking;
#if !UNITY_EDITOR && UNITY_WEBGL
using System.Runtime.InteropServices;
#endif
	public class TweetManager : MonoBehaviour
	{
		public static TweetManager Instance { get; private set; }

		[SerializeField] private string _imgurClientId;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(this.gameObject);
			}
			else
			{
				Destroy(this.gameObject);
				return;
			}
		}


#if !UNITY_EDITOR && UNITY_WEBGL
		[DllImport("__Internal")]
		private static extern string TweetFromUnity(string rawMessage);
#endif
		public void Tweet(string msg)
		{
#if !UNITY_EDITOR && UNITY_WEBGL
		TweetFromUnity(msg);
#endif
		}
		public void TweetWithScreenshot()
		{
			StartCoroutine(TweetWithScreenshotCo());
		}

		private IEnumerator TweetWithScreenshotCo()
		{
			yield return new WaitForEndOfFrame();

			Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

			var wwwForm = new WWWForm();
			wwwForm.AddField("image", Convert.ToBase64String(tex.EncodeToJPG()));
			wwwForm.AddField("type", "base64");

			// Upload to Imgur
			UnityWebRequest www = UnityWebRequest.Post("https://api.imgur.com/3/image.xml", wwwForm);
			www.SetRequestHeader("AUTHORIZATION", "Client-ID " + _imgurClientId);

			yield return www.SendWebRequest();

			var uri = "";

			if (www.result != UnityWebRequest.Result.ConnectionError)
			{
				Debug.Log("Upload complete!");
				XDocument xDoc = XDocument.Parse(www.downloadHandler.text);
				uri = xDoc.Element("data")?.Element("link")?.Value;

				// Remove Ext
				uri = uri?.Remove(uri.Length - 4, 4);
			}
			else
			{
				Debug.Log("Upload error: " + www.error);
			}

#if !UNITY_EDITOR && UNITY_WEBGL
		TweetFromUnity($"Tweet Message%0a{uri}");
#endif
		}
	}
