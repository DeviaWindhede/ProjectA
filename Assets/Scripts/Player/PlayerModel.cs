using UnityEngine;

[System.Serializable]
public struct PlayerStats {
    public const int MAX_STAT_VALUE = 18;
    public const int MIN_STAT_VALUE = -14;

    [SerializeField, Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    private int _boost;

    [SerializeField, Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    private int _charge;

    [SerializeField, Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    private int _defence; // Missing implementation

    [SerializeField, Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    private int _glide;

    [SerializeField, Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    private int _health; // Missing implementation

    [SerializeField, Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    private int _offence; // Missing implementation

    [SerializeField, Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    private int _topSpeed;

    [SerializeField, Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    private int _turn;

    [SerializeField, Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    private int _weight; // Missing implementation related to combat

    public int Boost {
        get { return _boost; }
        set { _boost = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Charge {
        get { return _charge; }
        set { _charge = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Defence {
        get { return _defence; }
        set { _defence = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Glide {
        get { return _glide; }
        set { _glide = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Health {
        get { return _health; }
        set { _health = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Offence {
        get { return _offence; }
        set { _offence = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int TopSpeed {
        get { return _topSpeed; }
        set { _topSpeed = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Turn {
        get { return _turn; }
        set { _turn = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Weight {
        get { return _weight; }
        set { _weight = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }

    public PlayerStats(
        int boost,
        int charge,
        int defence,
        int glide,
        int health,
        int offence,
        int topSpeed,
        int turn,
        int weight
    ) {
        _boost = boost;
        _charge = charge;
        _defence = defence;
        _glide = glide;
        _health = health;
        _offence = offence;
        _topSpeed = topSpeed;
        _turn = turn;
        _weight = weight;
    }

    public static PlayerStats operator +(PlayerStats current, PlayerStats apply) {
        current.Boost += apply.Boost;
        current.Charge += apply.Charge;
        current.Defence += apply.Defence;
        current.Glide += apply.Glide;
        current.Health += apply.Health;
        current.Offence += apply.Offence;
        current.TopSpeed += apply.TopSpeed;
        current.Turn += apply.Turn;
        current.Weight += apply.Weight;
        return current;
    }

    public static bool operator ==(PlayerStats current, PlayerStats apply) {
        return current.Boost == apply.Boost &&
        current.Charge == apply.Charge &&
        current.Defence == apply.Defence &&
        current.Glide == apply.Glide &&
        current.Health == apply.Health &&
        current.Offence == apply.Offence &&
        current.TopSpeed == apply.TopSpeed &&
        current.Turn == apply.Turn &&
        current.Weight == apply.Weight;
    }
    public static bool operator !=(PlayerStats current, PlayerStats apply) {
        return current.Boost != apply.Boost ||
        current.Charge != apply.Charge ||
        current.Defence != apply.Defence ||
        current.Glide != apply.Glide ||
        current.Health != apply.Health ||
        current.Offence != apply.Offence ||
        current.TopSpeed != apply.TopSpeed ||
        current.Turn != apply.Turn ||
        current.Weight != apply.Weight;
    }

    public override bool Equals(object obj) {
        if (obj.GetType() == typeof(PlayerStats))
            return this == (PlayerStats)obj;
        return base.Equals(obj);
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }
}
