using UnityEngine;
using UnityEngine.UI;

public class EnemyStamina : MonoBehaviour
{
	[Header("Stamina Settings")]
	public float maxStamina = 100f;
	public float currentStamina;

	[Header("UI References")]
	public Image staminaBarFill; // Drag your UI Image here


	void Start()
	{
		// Initialize stamina to maximum at the start
		currentStamina = maxStamina;
		UpdateStaminaUI();
	}

	void Update()
	{
		UpdateStaminaUI();
	}
	private void UpdateStaminaUI()
	{
		if (staminaBarFill != null)
		{
			// The fillAmount expects a value between 0 and 1
			if (staminaBarFill.fillAmount != currentStamina / maxStamina)
				staminaBarFill.fillAmount = currentStamina / maxStamina;
		}
	}
}