using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : Singleton<MenuManager>
{
    [SerializeField] private Animator _animator;
    private static GameObject gameObjectInstance;

    public float GetTransitionLength {
        get { return _animator.GetCurrentAnimatorStateInfo(0).length; }
    }

    void Awake() {
        if (gameObjectInstance != null)
            Destroy(gameObject);

        gameObjectInstance = gameObject;
        DontDestroyOnLoad(this);
    }

    public void SwapScenes(string sceneName) {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public void StartTransitionAnimation() {
        _animator.SetTrigger("StartLoading");
        _animator.SetBool("Loading", true);
    }

    public void StopTransitionAnimation() {
        _animator.SetBool("Loading", false);
    }

    IEnumerator LoadSceneAsync(string sceneName) {
        StartTransitionAnimation();
        yield return new WaitForSeconds(GetTransitionLength);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while(!asyncLoad.isDone) {
            yield return null;
        }

        StopTransitionAnimation();
    }
}
