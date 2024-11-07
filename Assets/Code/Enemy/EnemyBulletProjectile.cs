using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletProjectile : MonoBehaviour
{
    public Vector3 Position { get; private set; }
    public Vector3 Direction { get; private set; }
    public float Speed { get; private set; }

    public EnemyBulletProjectile(Vector3 startPosition, Vector3 direction, float speed)
    {
        Position = startPosition;
        Direction = direction.normalized;
        Speed = speed;
    }
}
