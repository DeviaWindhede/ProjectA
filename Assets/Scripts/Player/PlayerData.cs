using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour {
    public delegate void PlayerDataUpdate();
    public event PlayerDataUpdate OnStatUpdate;

    public PlayerInputValues input;

    [SerializeField] private PlayerStats _stats;
    public PlayerStats Stats {
        get { return _stats; }
        set { 
            _stats = value;
            OnStatUpdate();
        }
    }
}
