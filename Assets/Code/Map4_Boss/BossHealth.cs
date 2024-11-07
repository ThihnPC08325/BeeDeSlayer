using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [SerializeField] float maxHealthPhase1 = 200f;
    [SerializeField] float maxHealthPhase2 = 250f;
    private float health;
    private readonly bool isPhase2 = false;

    private void Awake()
    {
        health = maxHealthPhase1;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0f && !isPhase2)
        {
            TransitionToPhase2();
        }
    }

    private void TransitionToPhase2()
    {
        health = maxHealthPhase2;
    }

    private void Die()
    {
        throw new NotImplementedException();
    }
}
