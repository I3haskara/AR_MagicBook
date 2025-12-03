using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AIMessageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Display Settings")]
    [SerializeField] private float autoClearSeconds = 5f;

    private float timer = 0f;

    private void Awake()
    {
        if (messageText != null)
        {
            messageText.text = "";
        }
    }

    private void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f && messageText != null)
            {
                messageText.text = "";
            }
        }
    }

    public void ShowMessage(string message, string intent)
    {
        if (messageText == null) return;

        Debug.Log("[AIMessageUI] ShowMessage: " + message);

        if (!string.IsNullOrEmpty(intent))
        {
            messageText.text = $"[{intent}] {message}";
        }
        else
        {
            messageText.text = message;
        }

        timer = autoClearSeconds;
    }
}
