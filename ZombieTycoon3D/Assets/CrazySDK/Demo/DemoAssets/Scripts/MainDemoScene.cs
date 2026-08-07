using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyGames
{
    public class MainDemoScene : MonoBehaviour
    {
        public Text adblockText;
        public Text infoText;
        public GameObject initializedPanel,
            notInitializedPanel;
        public Toggle useAsyncToggle;

        public static bool UseAsyncMethods = false;

        [RuntimeInitializeOnLoadMethod]
        static void OnRuntimeMethodLoad()
        {
            UseAsyncMethods = false;
        }

        private void Start()
        {
            initializedPanel.SetActive(false);
            notInitializedPanel.SetActive(true);
            useAsyncToggle.isOn = UseAsyncMethods;
            CrazySDK.Init(() =>
            {
                infoText.text = $"SDK Version: {CrazySDK.Version} \n";
                infoText.text += $"SDK Initialized: {CrazySDK.IsInitialized} \n";
                infoText.text += $"Is user account available: {CrazySDK.User.IsUserAccountAvailable} \n";
                infoText.text += $"Environment: {CrazySDK.Environment} \n";
                infoText.text += $"Is SDK Available: {CrazySDK.IsAvailable} \n";

                CrazySDK.User.GetUser(
                    (
                        user =>
                        {
                            Debug.Log(("Got user" + user));
                        }
                    )
                );
                CrazySDK.Ad.HasAdblock(
                    (adblockPresent) =>
                    {
                        infoText.text += "Has adblock: " + adblockPresent + "\n";
                    }
                );

                if (CrazySDK.Game.InviteParams == null)
                {
                    infoText.text += "No invite params \n";
                }
                else
                {
                    infoText.text += "Invite params: \n";
                    foreach (var kvp in CrazySDK.Game.InviteParams)
                    {
                        infoText.text += $"{kvp.Key}: {kvp.Value}\n";
                    }
                }

                initializedPanel.SetActive(true);
                notInitializedPanel.SetActive(false);
            });
        }

        public void ToggleUseAsyncMethods()
        {
            UseAsyncMethods = useAsyncToggle.isOn;
            ReInitSDK();
        }

        private async void ReInitSDK()
        {
            if (UseAsyncMethods)
            {
                await CrazySDK.InitAsync();
                Debug.Log("Init SDK with async");
            }
            else
            {
                CrazySDK.Init(() =>
                {
                    Debug.Log("Init SDK with callback");
                });
            }
        }
    }
}
