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

    private static Dictionary<StatType, Texture2D> _pickupInfo;

    public void Awake()
    {
        _pickupInfo = new Dictionary<StatType, Texture2D>()
        {
            { StatType.All, Resources.Load<Texture2D>("Stats/all") },
            { StatType.Boost, Resources.Load<Texture2D>("Stats/boost") },
            { StatType.Charge, Resources.Load<Texture2D>("Stats/charge") },
            { StatType.Defence, Resources.Load<Texture2D>("Stats/defence") },
            { StatType.Glide, Resources.Load<Texture2D>("Stats/glide") },
            { StatType.Health, Resources.Load<Texture2D>("Stats/health") },
            { StatType.Offence, Resources.Load<Texture2D>("Stats/offence") },
            { StatType.TopSpeed, Resources.Load<Texture2D>("Stats/speed") },
            { StatType.Turn, Resources.Load<Texture2D>("Stats/turn") },
            { StatType.Weight, Resources.Load<Texture2D>("Stats/weight") },
        };
    }

    public static Texture2D GetStatTexture(StatType type)
    {
        Texture2D tex = null;
        _pickupInfo.TryGetValue(type, out tex);
        return tex;
    }
}
