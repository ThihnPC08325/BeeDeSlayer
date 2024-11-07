using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granade : MonoBehaviour
{
    [SerializeField] private float delay;
    [SerializeField] private float radius;
    [SerializeField] private float force;
    [SerializeField] private float damage;
    [SerializeField] private GameObject ExplodeEffect;
    private float countdown;
    private bool hasExploded = false;

    // Start is called before the first frame update
    void Start()
    {
        countdown = delay;
    }

    // Update is called once per frame
    void Update()
    {
        countdown -= Time.deltaTime;
        if (countdown <= 0f && !hasExploded)
        {
            Explode();
            hasExploded = true;
        }
    }

    private void Explode()
    {
        Instantiate(ExplodeEffect, transform.position, transform.rotation);

        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider nearObject in colliders)
        {
            Rigidbody rigidbody = nearObject.GetComponent<Rigidbody>();

            if (nearObject.gameObject.CompareTag("Enemy"))
            {
                rigidbody.AddExplosionForce(force, transform.position, radius);
                EnemyHealth TakeDamage = nearObject.GetComponent<EnemyHealth>();
                TakeDamage.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
