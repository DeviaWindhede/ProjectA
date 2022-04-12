using UnityEngine;
using UnityEngine.UI;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField]
    private Color _chargeColor;

    [SerializeField]
    private Color _expirationColor;

    [SerializeField]
    private Color _burnoutColor;

    [SerializeField]
    private Image _chargeImage;

    [SerializeField]
    private Image _expirationImage;
    private bool _burnout;
    private float _chargeRatio;
    private float _expirationRatio;
    private Player _player;
    public int PlayerIndex
    {
        get { return _player.PlayerIndex; }
    }

    public void SetFillRatio(float ratio)
    {
        _chargeRatio = Mathf.Clamp01(ratio);
        _chargeImage.fillAmount = _chargeRatio;
    }

    public void SetExpirationRatio(float ratio)
    {
        _expirationRatio = Mathf.Clamp01(ratio);
        _expirationImage.color = new Color(
            _expirationColor.r,
            _expirationColor.g,
            _expirationColor.b,
            _expirationRatio
        );
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
