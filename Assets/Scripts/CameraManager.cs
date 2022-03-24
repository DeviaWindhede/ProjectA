using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private Rect topHalf = new Rect(0, 0.5f, 1, 1);
    private Rect bottomHalf = new Rect(0, -0.5f, 1, 1);
    private Rect topLeft = new Rect(-0.5f, 0.5f, 1, 1);
    private Rect topRight = new Rect(0.5f, 0.5f, 1, 1);
    private Rect bottomLeft = new Rect(-0.5f, -0.5f, 1, 1);
    private Rect bottomRight = new Rect(0.5f, -0.5f, 1, 1);
    private List<Rect[]> sizes;

    public const int PLAYER_CAMERA_BASE_LAYER = 9;
    [SerializeField] private GameObject playerCameraPrefab;
    private List<Camera> cameras;
    void Start()
    {
        sizes = new List<Rect[]>() {
            { new Rect[] { new Rect(0, 0, 1, 1) }},
            { new Rect[] { topHalf, bottomHalf }},
            { new Rect[] { topLeft, topRight, bottomHalf }},
            { new Rect[] { topLeft, topRight, bottomLeft, bottomRight }},
        };

        cameras = new List<Camera>();
        foreach (Player player in GameObject.FindObjectsOfType<Player>()) {
            GameObject camera = Instantiate(this.playerCameraPrefab, this.transform);
            Camera c = camera.GetComponent<Camera>();
            c.cullingMask = c.cullingMask | 1 << (player.PlayerIndex + PLAYER_CAMERA_BASE_LAYER);

            int cullingMask = PLAYER_CAMERA_BASE_LAYER + player.PlayerIndex;
            camera.layer = cullingMask;
            player.GetFollowVirtualCamera.layer = cullingMask;

            cameras.Add(camera.GetComponent<Camera>());
        }
        for (int i = 0; i < cameras.Count; i++) {
            cameras[i].rect = sizes[cameras.Count - 1][i];
        }
    }
}
