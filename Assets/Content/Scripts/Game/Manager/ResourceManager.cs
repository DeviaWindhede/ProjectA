using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public enum StatType
    {
        None,
        All,
        Boost,
        Charge,
        Defence,
        Glide,
        Health,
        Offence,
        TopSpeed,
        Turn,
        Weight,
    }
    private const string BASE_STAT_FILE_PATH = "Graphics/Stats/";
    private static Dictionary<StatType, Texture2D> _pickupStatToTextureMap;
    private static Dictionary<StatType, PlayerStatPickupInfo> _pickupInfo; // TODO: Move this to somewhere more appropriate

    public void Awake()
    {
        _pickupStatToTextureMap = new Dictionary<StatType, Texture2D>()
        {
            { StatType.All, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "all") },
            { StatType.Boost, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "boost") },
            { StatType.Charge, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "charge") },
            { StatType.Defence, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "defence") },
            { StatType.Glide, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "glide") },
            { StatType.Health, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "health") },
            { StatType.Offence, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "offence") },
            { StatType.TopSpeed, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "speed") },
            { StatType.Turn, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "turn") },
            { StatType.Weight, Resources.Load<Texture2D>(BASE_STAT_FILE_PATH + "weight") },
        };


        _pickupInfo = new Dictionary<StatType, PlayerStatPickupInfo>()
        {
            {
                StatType.All,
                new PlayerStatPickupInfo(
                    new PlayerStats(1, 1, 1, 1, 1, 1, 1, 1, 1),
                    GetStatTexture(StatType.All)
                )
            },
            {
                StatType.Boost,
                new PlayerStatPickupInfo(
                    new PlayerStats(1, 0, 0, 0, 0, 0, 0, 0, 0),
                    GetStatTexture(StatType.Boost)
                )
            },
            {
                StatType.Charge,
                new PlayerStatPickupInfo(
                    new PlayerStats(0, 1, 0, 0, 0, 0, 0, 0, 0),
                    GetStatTexture(StatType.Charge)
                )
            },
            {
                StatType.Defence,
                new PlayerStatPickupInfo(
                    new PlayerStats(0, 0, 1, 0, 0, 0, 0, 0, 0),
                    GetStatTexture(StatType.Defence)
                )
            },
            {
                StatType.Glide,
                new PlayerStatPickupInfo(
                    new PlayerStats(0, 0, 0, 1, 0, 0, 0, 0, 0),
                    GetStatTexture(StatType.Glide)
                )
            },
            {
                StatType.Health,
                new PlayerStatPickupInfo(
                    new PlayerStats(0, 0, 0, 0, 1, 0, 0, 0, 0),
                    GetStatTexture(StatType.Health)
                )
            },
            {
                StatType.Offence,
                new PlayerStatPickupInfo(
                    new PlayerStats(0, 0, 0, 0, 0, 1, 0, 0, 0),
                    GetStatTexture(StatType.Offence)
                )
            },
            {
                StatType.TopSpeed,
                new PlayerStatPickupInfo(
                    new PlayerStats(0, 0, 0, 0, 0, 0, 1, 0, 0),
                    GetStatTexture(StatType.TopSpeed)
                )
            },
            {
                StatType.Turn,
                new PlayerStatPickupInfo(
                    new PlayerStats(0, 0, 0, 0, 0, 0, 0, 1, 0),
                    GetStatTexture(StatType.Turn)
                )
            },
            {
                StatType.Weight,
                new PlayerStatPickupInfo(
                    new PlayerStats(0, 0, 0, 0, 0, 0, 0, 0, 1),
                    GetStatTexture(StatType.Weight)
                )
            },
        };
    }

    public static Texture2D GetStatTexture(StatType type)
    {
        Texture2D tex = null;
        _pickupStatToTextureMap.TryGetValue(type, out tex);
        return tex;
    }

    public static PlayerStatPickupInfo GetPickupInfo(StatType type) {
        PlayerStatPickupInfo t;
        _pickupInfo.TryGetValue(type, out t);
        return t;
    }
}
