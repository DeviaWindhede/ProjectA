using UnityEngine;
using UnityEngine.UI;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField]
    private Color _chargeColor;

    [SerializeField]
    private Color _burnoutColor;

    [SerializeField]
    private Image _chargeImage;
    private bool _burnout;
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

    private Player _player;
    public int PlayerIndex
    {
        get { return _player.PlayerIndex; }
    }

    public void SetFillRatio(float ratio)
    {
        ChargeRatio = ratio;
        _chargeImage.fillAmount = ChargeRatio;
    }

    public void SetBurnout(bool value)
    {
        _burnout = value;
        _chargeImage.color = value ? _burnoutColor : _chargeColor;
    }

    public void SetPlayer(Player player)
    {
        _player = player;
    }

    public void Start()
    {
        _chargeImage.color = _chargeColor;
    }
}
