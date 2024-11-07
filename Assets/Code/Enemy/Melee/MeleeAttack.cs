using System.Collections;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform weapon;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Slash Path")]
    [SerializeField] private Vector3 startControlPoint = new Vector3(0, 0.5f, 0.5f);
    [SerializeField] private Vector3 endControlPoint = new Vector3(0, -0.5f, 1f);
    [SerializeField] private float slashDistance = 1f; // Khoảng cách để vung vũ khí về phía trước
    [SerializeField] private float slashDistanceX = 0.5f; // Khoảng cách để vung vũ khí theo trục x
    [SerializeField] private float slashDuration = 0.5f;
    [SerializeField] private float weaponTiltAngle = 45f;

    private bool isSlashing = false;
    private float lastAttackTime = 0f;
    private Transform target;
    private Vector3 originalWeaponPosition;
    private Quaternion originalWeaponRotation;

    private void Awake()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        originalWeaponPosition = weapon.localPosition;
        originalWeaponRotation = weapon.localRotation;
    }

    private void Update()
    {
        if (CanAttack())
        {
            StartSlashAttack();
        }
    }

    private bool CanAttack()
    {
        return Vector3.Distance(transform.position, target.position) <= attackRange
            && !isSlashing
            && Time.time >= lastAttackTime + attackCooldown;
    }

    private void StartSlashAttack()
    {
        isSlashing = true;
        lastAttackTime = Time.time;
        StartCoroutine(SlashAnimation());
    }

    private IEnumerator SlashAnimation()
    {
        float elapsedTime = 0f;
        Vector3 startPosition = weapon.localPosition;
        Vector3 endPoint = startPosition + transform.forward * slashDistance + transform.right * slashDistanceX;
        Quaternion startRotation = weapon.localRotation;

        while (elapsedTime < slashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / slashDuration;

            // Tính toán vị trí trên đường cong Bézier

            Vector3 bezierPoint = CubicBezier(
                startPosition,
                startPosition + transform.TransformDirection(startControlPoint),
                startPosition + transform.TransformDirection(endControlPoint),
                endPoint,
                t
            );

            // Tính toán vector tiếp tuyến
            Vector3 tangent = CubicBezierTangent(
                startPosition,
                startPosition + transform.TransformDirection(startControlPoint),
                startPosition + transform.TransformDirection(endControlPoint),
                startPosition + transform.forward * slashDistance,
                t
            ).normalized;

            // Áp dụng vị trí mới cho weapon trong không gian world
            Vector3 worldPosition = transform.TransformPoint(bezierPoint);
            weapon.position = worldPosition;

            // Xoay weapon để hướng theo tiếp tuyến và nằm theo hướng xoay của tiếp tuyến
            if (tangent != Vector3.zero)
            {
                Vector3 weaponForward = Vector3.ProjectOnPlane(tangent, transform.up).normalized;
                Vector3 weaponUp = transform.up;
                Vector3 weaponRight = Vector3.Cross(weaponUp, weaponForward).normalized;
                weaponForward = Vector3.Cross(weaponRight, weaponUp).normalized;

                Quaternion tilt = Quaternion.AngleAxis(weaponTiltAngle, weaponForward);
                Quaternion targetRotation = Quaternion.LookRotation(weaponForward, weaponUp) * tilt;
                weapon.rotation = targetRotation;
            }

            yield return null;
        }

        // Phần code quay về
        float returnTime = 0f;
        Vector3 endPosition = weapon.localPosition;
        Quaternion endRotation = weapon.localRotation;

        while (returnTime < slashDuration * 0.5f)
        {
            returnTime += Time.deltaTime;
            float t = returnTime / (slashDuration * 0.5f);

            float curveT = BezierCurve(t);

            weapon.localPosition = Vector3.Lerp(endPosition, originalWeaponPosition, curveT);
            weapon.localRotation = Quaternion.Slerp(endRotation, originalWeaponRotation, curveT);

            yield return null;
        }

        weapon.localPosition = originalWeaponPosition;
        weapon.localRotation = originalWeaponRotation;
        isSlashing = false;
    }

    private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    private Vector3 CubicBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 tangent = -3 * uu * p0;
        tangent += 3 * uu * p1 - 6 * u * t * p1;
        tangent += 6 * u * t * p2 - 3 * tt * p2;
        tangent += 3 * tt * p3;

        return tangent;
    }

    private float BezierCurve(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void OnDrawGizmosSelected()
    {
        if (weapon != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Vẽ đường cong Bézier
            Gizmos.color = Color.blue;
            Vector3 start = weapon.position;
            Vector3 end = start + transform.forward * slashDistance + transform.right * slashDistanceX;
            Vector3 startControl = start + transform.TransformDirection(startControlPoint);
            Vector3 endControl = start + transform.TransformDirection(endControlPoint);

            Gizmos.DrawLine(start, startControl);
            Gizmos.DrawLine(end, endControl);

            Vector3 prev = start;
            for (int i = 1; i <= 20; i++)
            {
                float t = i / 20f;
                Vector3 point = CubicBezier(start, startControl, endControl, end, t);
                Gizmos.DrawLine(prev, point);
                prev = point;
            }
            Gizmos.color = Color.green;
            int steps = 10;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 point = transform.TransformPoint(CubicBezier(start, startControl, endControl, end, t));
                Vector3 tangent = transform.TransformDirection(CubicBezierTangent(start, startControl, endControl, end, t).normalized);
                Vector3 weaponForward = Vector3.ProjectOnPlane(tangent, transform.up).normalized;
                Gizmos.DrawRay(point, weaponForward * 0.5f);
            }
        }
    }
}