using UnityEngine;
using UnityEngine.UI;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField]
    private Image _chargeImage;

    private Player _player;
    private float _chargeRatio;
    private float ChargeRatio
    {
        get { return _chargeRatio; }
        set
        {
            if (value > 1)
                _chargeRatio = 1;
            else if (value < 0)
                _chargeRatio = 0;
            else
                _chargeRatio = value;
        }
    }

    public int PlayerIndex { get { return _player.PlayerIndex; } }

    public void SetChargeRatio(float ratio)
    {
        ChargeRatio = ratio;
        _chargeImage.fillAmount = ChargeRatio;
    }

    public void SetPlayer(Player player)
    {
        _player = player;
    }
}
