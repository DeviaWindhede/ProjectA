using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using Mirror.FizzySteam;

public class SteamDataManager : Singleton<SteamDataManager>
{
    public FizzySteamworks FizzySteamworks { get; private set; }
    public SteamManager SteamManager { get; private set; }

    void Awake()
    {
        FizzySteamworks = GetComponent<FizzySteamworks>();
        SteamManager = GetComponent<SteamManager>();
    }
}
