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
        MenuManager.Instance.StartTransitionAnimation();
        yield return new WaitForSeconds(MenuManager.Instance.GetTransitionLength);

        SceneManager.sceneLoaded += OnSceneLoaded;
        SteamLobby.Instance.HostLobby();
    }
    private IEnumerator CloseLobbyCoroutine() {
        if (SteamLobby.Instance == null)
            yield return null;

        DisableButton();
        MenuManager.Instance.StartTransitionAnimation();
        yield return new WaitForSeconds(MenuManager.Instance.GetTransitionLength);

        SceneManager.sceneLoaded += OnSceneLoaded;

        SteamLobby.Instance.CloseLobby();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode _) {
        if (scene.isLoaded) {
            MenuManager.Instance.StopTransitionAnimation();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
