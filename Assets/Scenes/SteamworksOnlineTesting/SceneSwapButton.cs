using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class SceneSwapButton : MonoBehaviour {
    private CustomNetworkManager networkManager;
    private CustomNetworkManager NetworkManager {
        get {
            if (networkManager != null) return networkManager;
            return networkManager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }
    public void ChangeScene(string sceneName) {
        MenuManager.Instance.SwapScenes(sceneName);
        DisableButton();
    }

    public void ChangeToLocalPlay(bool isLocalPlay) {
        NetworkManager.IsLocalPlay = isLocalPlay;
    }

    public void CloseLobby() {
        StartCoroutine(CloseLobbyCoroutine());
    }

    public void HostLobby() {
        StartCoroutine(HostLobbyCoroutine());
    }

    private void DisableButton() {
        GetComponent<Button>().interactable = false;
    }

    private IEnumerator HostLobbyCoroutine() {
        if (SteamLobby.Instance == null)
            yield return null;

        DisableButton();
        ChangeToLocalPlay(false);
        MenuManager.Instance.StartTransitionAnimation();
        yield return new WaitForSeconds(MenuManager.Instance.GetTransitionLength);

        bool hasLoadedScene = false;
        SceneManager.sceneLoaded += (scene, y) => hasLoadedScene = scene.isLoaded;

        SteamLobby.Instance.HostLobby();
        yield return new WaitWhile(() => hasLoadedScene == true);
        yield return new WaitForSeconds(0.25f);

        MenuManager.Instance.StopTransitionAnimation();
        SceneManager.sceneLoaded -= (scene, y) => hasLoadedScene = scene.isLoaded;
    }
    private IEnumerator CloseLobbyCoroutine() {
        if (SteamLobby.Instance == null)
            yield return null;

        DisableButton();
        MenuManager.Instance.StartTransitionAnimation();
        yield return new WaitForSeconds(MenuManager.Instance.GetTransitionLength);

        SteamLobby.Instance.CloseLobby();
        ChangeToLocalPlay(true);
        MenuManager.Instance.StopTransitionAnimation();
    }
}
