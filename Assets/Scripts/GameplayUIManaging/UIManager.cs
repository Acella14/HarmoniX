using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("References")]
    public PlayerHealthWidget healthWidget;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void Init(PlayerHealth playerHealth)
    {
        playerHealth.OnHealthChanged += UpdateHealth;
    }

    public void UpdateHealth(int newHealth, int maxHealth)
    {
        healthWidget?.SetHealth(newHealth, maxHealth);
    }
}
