using System;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
#if !UNITY_EDITOR && UNITY_WEBGL
	using System.Runtime.InteropServices;
#endif

	public class TweetManager : MonoBehaviour
	{
		public static TweetManager Instance { get; private set; }

		[SerializeField] private string _imgurClientId;
		[SerializeField] private string _defaultMessage;

		private const string IMGUR_URL = "https://api.imgur.com/3/image.xml";

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

		private Texture2D CaptureScreenshotAsTexture(RectTransform rect)
		{
			if (rect == null) 
			{
				return ScreenCapture.CaptureScreenshotAsTexture();
			}
			//Get the corners of RectTransform rect and store it in a array vector
			Vector3[] corners = new Vector3[4];
			rect.GetWorldCorners(corners);
			//Remove 100 and you will get error
			int width = ((int)corners[3].x - (int)corners[0].x) - 100;
			int height = (int)corners[1].y - (int)corners[0].y;
			var startX = corners[0].x;
			var startY = corners[0].y;
			//Make a temporary texture and read pixels from it
			Texture2D tex = new(width, height, TextureFormat.RGB24, false);
			tex.ReadPixels(new Rect(startX, startY, width, height), 0, 0);
			tex.Apply();
			return tex;
		}

		public async UniTask TweetWithScreenshot(RectTransform rect = null, Action afterCapture = null)
		{
			await UniTask.WaitForEndOfFrame(this);
			//Save the screenshot to disk
			Texture2D tex = CaptureScreenshotAsTexture(rect);
			afterCapture?.Invoke();

			var wwwForm = new WWWForm();
			wwwForm.AddField("image", Convert.ToBase64String(tex.EncodeToJPG()));
			wwwForm.AddField("type", "base64");
			// Destroy texture to avoid memory leaks
			Destroy(tex);
			// Upload to Imgur
			using (var request = UnityWebRequest.Post(IMGUR_URL, wwwForm))
			{
				request.SetRequestHeader("AUTHORIZATION", $"Client-ID {_imgurClientId}");
				await request.SendWebRequest().ToUniTask();

				if (request.result != UnityWebRequest.Result.ConnectionError)
				{
					Debug.Log("Upload complete!");
					var response = JsonConvert.DeserializeObject<ImgurResponse>(request.downloadHandler.text);
					var uri = response.data.link;
					uri = uri?.Remove(uri.Length - 4, 4);

#if !UNITY_EDITOR && UNITY_WEBGL
					TweetFromUnity($"{_defaultMessage}%0a{uri}");
#endif
				}
				else
				{
					Debug.Log($"Upload error: {request.error}");
				}
			}
		}

		[Serializable]
		public class ImgurResponse
		{
			public Data data;
		}

		[Serializable]
		public class Data
		{
			public string link;
		}
	}
