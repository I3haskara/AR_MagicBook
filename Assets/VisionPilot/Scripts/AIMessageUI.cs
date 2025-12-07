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

    public void ShowMessage(string userText, string aiText, string intent = null)
    {
        if (messageText == null) return;

        // Backend marker for STT failure / empty audio
        if (!string.IsNullOrEmpty(userText) &&
            userText.Contains("STT error or empty audio"))
        {
            messageText.text = "I couldn’t hear you. Tap and try again.";
            timer = autoClearSeconds;
            return;
        }

        string textToShow = aiText;
        if (!string.IsNullOrEmpty(intent) && !string.IsNullOrEmpty(aiText))
        {
            textToShow = $"[{intent}] {aiText}";
        }

        messageText.text = textToShow;

        Debug.Log("[AIMessageUI] ShowMessage: " + textToShow);

        timer = autoClearSeconds;
    }
}
