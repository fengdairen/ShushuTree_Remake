using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
   public Button tutorialButton;

    private void Update()
    {
        if (tutorialButton != null && tutorialButton.gameObject.activeInHierarchy)
        {
            tutorialButton.onClick.AddListener(OnTutorialButtonClick);
        }

    }

    private void OnTutorialButtonClick()
    {
        tutorialButton.gameObject.SetActive(false);
    }
}
