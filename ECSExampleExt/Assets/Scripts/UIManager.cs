using System;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    public Canvas UIRoot;
    public Text FPSLabel;
    public Text UIModeLabel;
    public Text MobCountLabel;
    public Button Dec100Btn;
    public Button Inc100Btn;
    public Button Dec1000Btn;
    public Button Inc1000Btn;

    void Start()
    {
        if (Dec100Btn)
            Dec100Btn.onClick.AddListener(delegate { ChangeMobCnt(-100); });

        if (Inc100Btn)
            Inc100Btn.onClick.AddListener(delegate { ChangeMobCnt(100); });

        if (Dec1000Btn)
            Dec1000Btn.onClick.AddListener(delegate { ChangeMobCnt(-1000); });

        if (Inc1000Btn)
            Inc1000Btn.onClick.AddListener(delegate { ChangeMobCnt(1000); });

        RefreshPoolCount();
    }

    void Update()
    {
    }

    public void UpdateFPS(float fps)
    {
        if (!FPSLabel)
            return;

        FPSLabel.text = fps.ToString("0.00") + " fps";
    }

    public void UpdateInputMode(InputManager.InputMode mode)
    {
        if (!UIModeLabel)
            return;

        UIModeLabel.text = mode.ToString();
    }

    public void ShowHideUI()
    {
        if (!UIRoot)
            return;

        UIRoot.enabled = !UIRoot.enabled;
    }

    public void RefreshPoolCount()
    {
        if (!MobCountLabel)
            return;

        MobCountLabel.text = "Mob Count: " + ECSManager.Instance.MobCnt.ToString();
    }

    public void ChangeMobCnt(int changeAmount)
    {
        ECSManager.instance.MobCnt += changeAmount;
        RefreshPoolCount();
    }
}
