using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 50f;

    public GameObject damageNumberPrefab;

    public Transform headTransform;

    public void TakeDamage(float amount)
    {
        health -= amount;

        DisplayDamageNumber(amount);

        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        var gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.OnEnemyKilled();
        }

        Destroy(gameObject);
    }

    private void DisplayDamageNumber(float damageAmount)
    {
        GameObject damageNumberObject = Instantiate(damageNumberPrefab, headTransform.position, Quaternion.identity);

        DamageNumber damageNumber = damageNumberObject.GetComponent<DamageNumber>();
        damageNumber.SetDamageAmount(damageAmount);
    }
}
