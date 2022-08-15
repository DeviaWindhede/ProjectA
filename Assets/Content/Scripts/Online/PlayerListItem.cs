using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Steamworks;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour {
    public string playerName;
    public int connectionId;
    public ulong playerSteamId;
    private bool avatarRecieved;

    public TextMeshProUGUI playerNameText;
    public RawImage playerIcon;

    protected Callback<AvatarImageLoaded_t> imageLoaded;

    private void Start() {
        imageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    public void SetPlayerValues() {
        playerNameText.text = playerName;
        if (!avatarRecieved) GetPlayerIcon();
    }

    private void GetPlayerIcon() {
        int imageId = SteamFriends.GetLargeFriendAvatar((CSteamID)playerSteamId);
        if (imageId == -1) return;
        playerIcon.texture = GetSteamImageAsTexture(imageId);
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback) {
        if (callback.m_steamID.m_SteamID == playerSteamId) {
            playerIcon.texture = GetSteamImageAsTexture(callback.m_iImage);
        }
        else { // another player
            return; // TODO
        }
    }

    private Texture2D GetSteamImageAsTexture(int iImage) {
        Texture2D texture = null;

        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);
        if (isValid) {
            byte[] image = new byte[width * height * 4];

            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));

            if (isValid) {
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
            }
        }
        avatarRecieved = true;
        return texture;
    }
}
