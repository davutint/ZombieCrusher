using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CrazyGames
{
    public class UserModuleDemo : MonoBehaviour
    {
        public Text userAccountAvailableText;
        public Text userLoggedOutText;
        public Image userAvatarImage;
        public Text usernameText;
        public GameObject userInfoContainer;
        public InputField friendsPage;
        public InputField friendsPerPage;

        private readonly List<Action<PortalUser>> _authListeners = new List<Action<PortalUser>>();

        public void Start()
        {
            userInfoContainer.SetActive(false);
            userLoggedOutText.gameObject.SetActive(false);
            friendsPage.text = "1";
            friendsPerPage.text = "10";
            CrazySDK.Init(() =>
            {
                CrazySDK.User.GetUser(
                    (user) =>
                    {
                        if (user != null)
                        {
                            Debug.Log("User is signed in: " + user);
                            userInfoContainer.SetActive(true);
                            usernameText.text = user.username;
                            StartCoroutine(FetchUserImage(user.profilePictureUrl));
                        }
                        else
                        {
                            Debug.Log("User is not signed in");
                            userLoggedOutText.gameObject.SetActive(true);
                        }
                    }
                );
            }); // ensure if starting this scene from editor it is initialized

            if (userAccountAvailableText != null)
            {
                var isAvailable = CrazySDK.User.IsUserAccountAvailable;
                userAccountAvailableText.text = $"User account available: {isAvailable}";
            }
        }

        IEnumerator FetchUserImage(string url)
        {
#if UNITY_2020_1_OR_NEWER
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                userAvatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            }
#else
            Debug.Log("Image fetch example not supported in Unity versions before 2020.1 due to UnityWebRequest changes.");
            yield return null;
#endif
        }

        public async void GetUser()
        {
            if (MainDemoScene.UseAsyncMethods)
            {
                var user = await CrazySDK.User.GetUserAsync();
                if (user != null)
                {
                    Debug.Log("Get user result (async): " + user);
                }
                else
                {
                    Debug.Log("User is not signed in (async)");
                }
            }
            else
            {
                CrazySDK.User.GetUser(
                    (user) =>
                    {
                        if (user != null)
                        {
                            Debug.Log("Get user result: " + user);
                        }
                        else
                        {
                            Debug.Log("User is not signed in");
                        }
                    }
                );
            }
        }

        public async void GetToken()
        {
            if (MainDemoScene.UseAsyncMethods)
            {
                try
                {
                    var token = await CrazySDK.User.GetUserTokenAsync();
                    Debug.Log("Get token result (async): " + token);
                }
                catch (SdkError e)
                {
                    Debug.LogError("Get token error (async): " + e);
                }
            }
            else
            {
                CrazySDK.User.GetUserToken(
                    (error, token) =>
                    {
                        if (error != null)
                        {
                            Debug.LogError("Get user token error: " + error);
                            return;
                        }

                        Debug.Log("User token: " + token);
                    }
                );
            }
        }

        public async void GetXsollaUserToken()
        {
            if (MainDemoScene.UseAsyncMethods)
            {
                try
                {
                    var token = await CrazySDK.User.GetXsollaUserTokenAsync();
                    Debug.Log("Get Xsolla user token result (async): " + token);
                }
                catch (SdkError e)
                {
                    Debug.LogError("Get Xsolla user token error (async): " + e);
                }
            }
            else
            {
                CrazySDK.User.GetXsollaUserToken(
                    (error, token) =>
                    {
                        if (error != null)
                        {
                            Debug.LogError("Get Xsolla user token error: " + error);
                            return;
                        }

                        Debug.Log("Xsolla user token: " + token);
                    }
                );
            }
        }

        public void SyncUnityGameData()
        {
            CrazySDK.User.SyncUnityGameData();
        }

        public async void ShowAuthPrompt()
        {
            if (MainDemoScene.UseAsyncMethods)
            {
                try
                {
                    var user = await CrazySDK.User.ShowAuthPromptAsync();
                    Debug.Log("Auth prompt user (async): " + user);
                }
                catch (SdkError e)
                {
                    Debug.LogError("Show auth prompt error (async): " + e);
                }
            }
            else
            {
                CrazySDK.User.ShowAuthPrompt(
                    (error, user) =>
                    {
                        if (error != null)
                        {
                            Debug.LogError("Show auth prompt error: " + error);
                            return;
                        }

                        Debug.Log("Auth prompt user: " + user);
                    }
                );
            }
        }

        public async void ShowAccountLinkPrompt()
        {
            if (MainDemoScene.UseAsyncMethods)
            {
                try
                {
                    var answer = await CrazySDK.User.ShowAccountLinkPromptAsync();
                    Debug.Log("Account link answer (async): " + answer);
                }
                catch (SdkError e)
                {
                    Debug.LogError("Show account link prompt error (async): " + e);
                }
            }
            else
            {
                CrazySDK.User.ShowAccountLinkPrompt(
                    (error, answer) =>
                    {
                        if (error != null)
                        {
                            Debug.LogError("Show account link prompt error: " + error);
                            return;
                        }

                        Debug.Log("Account link answer: " + answer);
                    }
                );
            }
        }

        public void AddAuthListener()
        {
            _authListeners.Add(
                (user) =>
                {
                    Debug.Log("Auth listener, user: " + user);
                }
            );
            CrazySDK.User.AddAuthListener(_authListeners.Last());
        }

        public void RemoveLastAuthListener()
        {
            if (_authListeners.Count == 0)
            {
                return;
            }

            var lst = _authListeners.Last();
            CrazySDK.User.RemoveAuthListener(lst);
            _authListeners.Remove(lst);
        }

        public void GetSystemInfo()
        {
            var systemInfo = CrazySDK.User.SystemInfo;
            Debug.Log($"All system info: {systemInfo}");
            Debug.Log($"Browser: {systemInfo.browser}");
            Debug.Log($"OS: {systemInfo.os}");
            Debug.Log($"Country code: {systemInfo.countryCode}");
            Debug.Log($"Locale: {systemInfo.locale}");
            Debug.Log($"Device type: {systemInfo.device.type}");
            Debug.Log($"Application type: {systemInfo.applicationType}");
        }

        public void AddScore()
        {
            CrazySDK.User.AddScore(152.1f);
        }

        public void SubmitScore()
        {
            float score = 152.1f;
            string encryptionKey = "mKwl+7lX7n87tJ41eNbRMcvIYVcYjZG71sas3EIMxoU=";
            string encryptedScore = ScoreEncryption.EncryptScore(score, encryptionKey);
            CrazySDK.User.SubmitScore(encryptedScore, score);
        }

        public async void ListFriends()
        {
            var page = int.Parse(friendsPage.text);
            var perPage = int.Parse(friendsPerPage.text);
            Action<FriendsPage> onSuccess = (friendsPage) =>
            {
                Debug.Log(
                    $"Displaying friends page {friendsPage.page} (per page: {friendsPage.size}, total friends: {friendsPage.total}, has more: {friendsPage.hasMore}):"
                );
                foreach (var friend in friendsPage.friends)
                {
                    Debug.Log("Friend: " + friend);
                }
            };
            if (MainDemoScene.UseAsyncMethods)
            {
                try
                {
                    var friends = await CrazySDK.User.ListFriendsAsync(page, perPage);
                    onSuccess(friends);
                }
                catch (SdkError e)
                {
                    Debug.LogError("List friends error (async): " + e);
                }
            }
            else
            {
                CrazySDK.User.ListFriends(
                    page,
                    perPage,
                    (error, friends) =>
                    {
                        if (error != null)
                        {
                            Debug.LogError("List friends error: " + error);
                            return;
                        }
                        onSuccess(friends);
                    }
                );
            }
        }
    }
}
