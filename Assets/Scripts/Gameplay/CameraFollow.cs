using UnityEngine;

/// <summary>
/// Плавно следует за target (игроком), сохраняя свою высоту по Z
/// (важно для 2D-камеры — у вас она стоит на z = -10).
/// Повесьте на Main Camera и назначьте target = Player в инспекторе.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("Чем меньше — тем быстрее камера догоняет игрока")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector2 offset = Vector2.zero;

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z); // сохраняем текущий z камеры

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref _velocity, smoothTime);
    }
}
