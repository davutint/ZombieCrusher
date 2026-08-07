using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyGames
{
    public class GameModuleDemo : MonoBehaviour
    {
        public Text instantJoinText;
        public Text settingsText;
        public InputField roomNameInput;
        public Toggle joinableToggle;
        public Toggle dontSentId;

        private void Start()
        {
            CrazySDK.Init(() => { }); // ensure if starting this scene from editor it is initialized
            instantJoinText.text = $"IsInstantMultiplayer: {CrazySDK.Game.IsInstantMultiplayer}";
            settingsText.text = "Settings: " + CrazySDK.Game.Settings.ToString();
            CrazySDK.Game.AddSettingsChangeListener(
                (newSettings) =>
                {
                    settingsText.text = "Settings: " + newSettings.ToString();
                }
            );
            CrazySDK.Game.AddJoinRoomListener(JoinRoomListener);
            roomNameInput.text = "123";
        }

        private void JoinRoomListener(Dictionary<string, string> parameters)
        {
            Debug.Log("Join room with invite parameters: " + string.Join(", ", parameters.Select(p => $"{p.Key}: {p.Value}")));
        }

        private void OnDestroy()
        {
            CrazySDK.Game.RemoveJoinRoomListener(JoinRoomListener);
        }

        public void Happytime()
        {
            CrazySDK.Game.HappyTime();
        }

        public void GameplayStart()
        {
            CrazySDK.Game.GameplayStart();
        }

        public void GameplayStop()
        {
            CrazySDK.Game.GameplayStop();
        }

        public void LogSettings()
        {
            Debug.Log(CrazySDK.Game.Settings.ToString());
        }

        public void InviteLink()
        {
            var parameters = new Dictionary<string, string> { { "roomName", "1234" }, { "otherParameter", " uri encoded string" } };
            var inviteLink = CrazySDK.Game.InviteLink(parameters);
            Debug.Log("Invite link (also copied to clipboard): " + inviteLink);
            CrazySDK.Game.CopyToClipboard(inviteLink);
        }

        public void ShowInviteButton()
        {
            var parameters = new Dictionary<string, string> { { "roomName", "1234" } };
            var inviteLink = CrazySDK.Game.ShowInviteButton(parameters);
            Debug.Log("Invite button link: " + inviteLink);
        }

        public void HideInviteButton()
        {
            CrazySDK.Game.HideInviteButton();
        }

        public void ParseInviteLink()
        {
            if (Application.isEditor)
            {
                Debug.Log("Cannot parse url in Unity editor, try running it in a browser");
            }
            else
            {
                var roomId = CrazySDK.Game.GetInviteLinkParameter("roomName");
                Debug.Log($"Invite link param roomId = {roomId}");
            }
        }

        public void OnUpdateRoomClick()
        {
            var input = new UpdateRoomInput();
            var region = "eu";
            if (!dontSentId.isOn)
            {
                // keep a unique room id across regions
                input.RoomId = roomNameInput.text + region;
            }
            if (joinableToggle.isOn)
            {
                input.IsJoinable = true;
                // you can append more invite params as needed
                input.InviteParams = new Dictionary<string, string>() { { "roomName", roomNameInput.text }, { "region", region } };
            }
            else
            {
                input.IsJoinable = false;
            }
            CrazySDK.Game.UpdateRoom(input);
        }

        public void OnLeftRoomClick()
        {
            CrazySDK.Game.LeftRoom();
        }

        public void OnSetGameContextClick()
        {
            var context = new Dictionary<string, string>() { { "level", "1" } };
            CrazySDK.Game.SetGameContext(context);
        }

        public void OnClearGameContextClick()
        {
            CrazySDK.Game.ClearGameContext();
        }
    }
}
