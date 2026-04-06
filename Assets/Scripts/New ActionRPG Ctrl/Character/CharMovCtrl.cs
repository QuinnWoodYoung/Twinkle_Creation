using UnityEngine;

public class CharMovCtrl : MonoBehaviour
{
    private CharacterController _CC;
    private Vector3 _charVelocity;
    private bool _isCharGrounded;

    [SerializeField] private float charMoveSpeed = 2.0f;
    [SerializeField] private float gravityValue = -9.81f;

    public void ApplyMovement(Vector2 movement)
    {
        _isCharGrounded = _CC.isGrounded;
        if (_isCharGrounded && _charVelocity.y < 0)
        {
            _charVelocity.y = 0f;
        }

        Vector3 move = new Vector3(movement.x, 0, movement.y);
        _CC.Move(move * Time.deltaTime * charMoveSpeed);

        if (move != Vector3.zero)
        {
            gameObject.transform.forward = move;
        }

        _charVelocity.y += gravityValue * Time.deltaTime;
        _CC.Move(_charVelocity * Time.deltaTime);
    }
}
