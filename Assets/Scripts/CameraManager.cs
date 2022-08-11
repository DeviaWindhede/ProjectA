using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public const int PLAYER_CAMERA_BASE_LAYER = 9;

    [SerializeField]
    private GameObject _playerCameraPrefab;

    private Rect _topHalf = new Rect(0, 0.5f, 1, 1);
    private Rect _bottomHalf = new Rect(0, -0.5f, 1, 1);
    private Rect _topLeft = new Rect(-0.5f, 0.5f, 1, 1);
    private Rect _topRight = new Rect(0.5f, 0.5f, 1, 1);
    private Rect _bottomLeft = new Rect(-0.5f, -0.5f, 1, 1);
    private Rect _bottomRight = new Rect(0.5f, -0.5f, 1, 1);
    private List<Rect[]> _sizes;
    private List<GameObject> _cameras;

    void Start()
    {
        var networkManager = CustomNetworkManager.singleton as CustomNetworkManager;
        if (!networkManager.IsLocalPlay) {
            gameObject.SetActive(false);
            return;
        }
        _sizes = new List<Rect[]>()
        {
            { new Rect[] { new Rect(0, 0, 1, 1) } },
            { new Rect[] { _topHalf, _bottomHalf } },
            { new Rect[] { _topLeft, _topRight, _bottomHalf } },
            { new Rect[] { _topLeft, _topRight, _bottomLeft, _bottomRight } },
        };

        _cameras = new List<GameObject>();
        Player[] arr = GameObject.FindObjectsOfType<Player>();
        for (int i = arr.Length - 1; i >= 0; i--)
        {
            Player player = arr[i];
            GameObject camera = Instantiate(this._playerCameraPrefab, transform);
            Camera c = camera.GetComponent<Camera>();
            c.cullingMask = c.cullingMask | 1 << (player.PlayerIndex + PLAYER_CAMERA_BASE_LAYER);

            int cullingMask = PLAYER_CAMERA_BASE_LAYER + player.PlayerIndex;
            camera.layer = cullingMask;
            player.GetFollowVirtualCamera.layer = cullingMask;

            camera.GetComponent<PlayerUIHandler>().SetPlayer(player);

            _cameras.Add(camera);
        }
        for (int i = 0; i < _cameras.Count; i++)
        {
            var cam = _cameras[i].GetComponent<Camera>();
            cam.rect = _sizes[_cameras.Count - 1][i];
        }
    }
    public GameObject GetCamera(int playerIndex)
    {
        return _cameras.Find(x => x.GetComponent<PlayerUIHandler>().PlayerIndex == playerIndex);
    }
}
