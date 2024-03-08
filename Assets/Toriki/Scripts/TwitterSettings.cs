using UnityEngine;

namespace Toriki.Settings
{
    public class TwitterSettings
    {
        public static readonly string ConsumerKey;
        public static readonly string ConsumerSecret;

        static TwitterSettings()
        {
            ConsumerKey = "9wOMHhKvovMCjDujbj0kCsYPw";
            ConsumerSecret = "mvVt0NzfPugYTnkwTUCbikv7hMTSpyJP5eq2oSfS9IGSOKb6S6";

            if (string.IsNullOrEmpty(ConsumerKey))
            {
                Debug.Log("ConsumerKey is null. please write some code for setting ConsumerKey.");
            }

            if (string.IsNullOrEmpty(ConsumerSecret))
            {
                Debug.Log("ConsumerSecret is null. please write some code for setting ConsumerSecret.");
            }
        }
    }

}