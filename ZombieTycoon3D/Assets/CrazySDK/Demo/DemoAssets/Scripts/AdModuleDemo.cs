using UnityEngine;
using UnityEngine.UI;

namespace CrazyGames
{
    public class AdModuleDemo : MonoBehaviour
    {
        public Text timerText;
        public Text keyboardText;
        public Text adblockText;

        private void Start()
        {
            CrazySDK.Init(() => { }); // ensure if starting this scene from editor it is initialized
            CheckAdblock();
        }

        private async void CheckAdblock()
        {
            if (MainDemoScene.UseAsyncMethods)
            {
                bool adblockPresent = await CrazySDK.Ad.HasAdblockAsync();
                adblockText.text = "Has adblock: " + adblockPresent + " (async)";
            }
            else
            {
                CrazySDK.Ad.HasAdblock(
                    (adblockPresent) =>
                    {
                        adblockText.text = "Has adblock: " + adblockPresent;
                    }
                );
            }
        }

        private void Update()
        {
            timerText.text = "Timer: " + Time.time;

            string heldKeys = "";
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKey(key))
                {
                    heldKeys += key.ToString() + " ";
                }
            }
            if (heldKeys.Length == 0)
            {
                heldKeys = "None";
            }
            keyboardText.text = "Keyboard: " + heldKeys.Trim();

            if (Input.GetKeyDown(KeyCode.M))
            {
                ShowMidgameAd();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ShowRewardedAd();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                PrefetchAd();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void ShowMidgameAd()
        {
            CrazySDK.Ad.RequestAd(
                CrazyAdType.Midgame,
                () =>
                {
                    Debug.Log("Midgame ad started");
                },
                (error) =>
                {
                    Debug.Log("Midgame ad error: " + error);
                },
                () =>
                {
                    Debug.Log("Midgame ad finished");
                }
            );
        }

        public void ShowRewardedAd()
        {
            CrazySDK.Ad.RequestAd(
                CrazyAdType.Rewarded,
                () =>
                {
                    Debug.Log("Rewarded ad started");
                },
                (error) =>
                {
                    Debug.Log("Rewarded ad error: " + error);
                },
                () =>
                {
                    Debug.Log("Rewarded ad finished, reward the player here");
                }
            );
        }

        public void PrefetchAd()
        {
            CrazySDK.Ad.PrefetchAd(CrazyAdType.Rewarded);
        }
    }
}
