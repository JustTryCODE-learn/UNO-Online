using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuleManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject rulesPanel;
    public GameObject[] pages;
    public TMP_Text pageCounterText;
    public Button nextBtn;
    public Button prevBtn;

    private int currentPage = 0;

    void Start()
    {
        if (rulesPanel != null) rulesPanel.SetActive(false);
        UpdateUI();
    }

    public void OpenRules()
    {
        rulesPanel.SetActive(true);
        currentPage = 0;
        UpdateUI();
    }

    public void CloseRules() => rulesPanel.SetActive(false);

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            UpdateUI();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currentPage);
        }
        pageCounterText.text = (currentPage + 1) + " / " + pages.Length;
        prevBtn.interactable = (currentPage > 0);
        nextBtn.interactable = (currentPage < pages.Length - 1);
    }
}