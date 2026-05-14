using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("数字键快捷面板")]
    [SerializeField]
    private GameObject wallNutPanel;
    [SerializeField]
    private GameObject hrPanel;
    [SerializeField]
    private GameObject shuStorePanel;
    [SerializeField]
    private GameObject buildPanel;
    [SerializeField]
    private string wallNutPanelKey;
    [SerializeField]
    private string hrPanelKey;
    [SerializeField]
    private string shuStorePanelKey;
    [SerializeField]
    private string buildPanelKey;

    private TabPageManager tabPageManager;

    #region 面板注册
    [System.Serializable]
    public class PanelEntry
    {
        public string panelKey;
        public GameObject panelObject;
        public bool closeOnEsc = true;
    }

    [SerializeField]
    private List<PanelEntry> panels = new List<PanelEntry>();

    private readonly Dictionary<string, PanelEntry> panelLookup = new Dictionary<string, PanelEntry>();
    private readonly Dictionary<GameObject, System.Action> closeCallbacks = new Dictionary<GameObject, System.Action>();
    private PanelEntry currentPanel;
    private GameObject currentPanelObject;
    private bool currentCloseOnEsc = true;
    #endregion

    #region Unity生命周期
    // 初始化单例并注册面板。
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildPanelLookup();
        CloseAllPanels();
    }

    // 监听ESC退出当前面板。
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCurrentPanelByEsc();
        }

        HandleHotkeys();
    }
    #endregion

    #region 数字键快捷键

    // 处理数字键快捷切换。
    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TogglePanelByHotkey(wallNutPanel, wallNutPanelKey);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TogglePanelByHotkey(hrPanel, hrPanelKey);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TogglePanelByHotkey(shuStorePanel, shuStorePanelKey);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            CloseCurrentPanelImmediate();
            EnsureTabPageManager();
            if (tabPageManager != null)
            {
                tabPageManager.ToggleDestroyModeByHotkey();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            EnsureTabPageManager();
            if (tabPageManager != null)
            {
                tabPageManager.ToggleBuildPanelByHotkey();
            }
            else
            {
                TogglePanelByHotkey(buildPanel, buildPanelKey);
            }
        }
    }

    // 快捷键切换面板显示。
    private void TogglePanelByHotkey(GameObject panelObject, string panelKey)
    {
        if (panelObject == null && string.IsNullOrEmpty(panelKey))
        {
            return;
        }

        if (panelObject != null)
        {
            if (panelObject.activeSelf)
            {
                ClosePanel(panelObject);
                return;
            }

            OpenPanel(panelObject);
            return;
        }

        TogglePanelByKey(panelKey);
    }

    // 快捷键切换面板显示（按Key）。
    private void TogglePanelByKey(string panelKey)
    {
        if (string.IsNullOrEmpty(panelKey))
        {
            return;
        }

        if (IsPanelOpen(panelKey))
        {
            ClosePanel(panelKey);
            return;
        }

        OpenPanel(panelKey);
    }

    // 获取TabPageManager实例。
    private void EnsureTabPageManager()
    {
        if (tabPageManager == null)
        {
            tabPageManager = FindObjectOfType<TabPageManager>();
        }
    }

    #endregion

    #region 快捷关闭

    // 快捷关闭当前打开面板。
    private void CloseCurrentPanelImmediate()
    {
        if (currentPanel != null && currentPanel.panelObject != null)
        {
            ClosePanel(currentPanel.panelKey);
            return;
        }

        if (currentPanelObject != null)
        {
            ClosePanel(currentPanelObject);
        }
    }

    #endregion

    #region 面板控制
    // 打开指定面板，并关闭其它面板。
    public void OpenPanel(string panelKey)
    {
        if (string.IsNullOrEmpty(panelKey))
        {
            return;
        }

        PanelEntry entry;
        if (!panelLookup.TryGetValue(panelKey, out entry) || entry.panelObject == null)
        {
            return;
        }

        HandleDestroyModeOnPanelOpen(entry.panelObject);
        CloseAllPanels();
        entry.panelObject.SetActive(true);
        currentPanel = entry;
        currentPanelObject = entry.panelObject;
        currentCloseOnEsc = entry.closeOnEsc;
    }

    // 打开指定面板对象，并关闭其它面板。
    public void OpenPanel(GameObject panelObject)
    {
        PanelEntry entry = FindEntry(panelObject);
        if (entry != null)
        {
            OpenPanel(entry.panelKey);
            return;
        }

        if (panelObject == null)
        {
            return;
        }

        HandleDestroyModeOnPanelOpen(panelObject);
        CloseAllPanels();
        panelObject.SetActive(true);
        currentPanel = null;
        currentPanelObject = panelObject;
        currentCloseOnEsc = true;
    }

    // 关闭指定面板。
    public void ClosePanel(string panelKey)
    {
        PanelEntry entry;
        if (!panelLookup.TryGetValue(panelKey, out entry) || entry.panelObject == null)
        {
            return;
        }

        entry.panelObject.SetActive(false);
        InvokeCloseCallback(entry.panelObject);
        if (currentPanel == entry)
        {
            currentPanel = null;
            currentPanelObject = null;
        }
    }

    // 关闭指定面板对象。
    public void ClosePanel(GameObject panelObject)
    {
        PanelEntry entry = FindEntry(panelObject);
        if (entry != null)
        {
            ClosePanel(entry.panelKey);
            return;
        }

        if (panelObject != null)
        {
            panelObject.SetActive(false);
            InvokeCloseCallback(panelObject);
        }

        if (currentPanelObject == panelObject)
        {
            currentPanelObject = null;
        }
    }

    // 关闭所有面板。
    public void CloseAllPanels()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            PanelEntry entry = panels[i];
            if (entry != null && entry.panelObject != null)
            {
                entry.panelObject.SetActive(false);
                InvokeCloseCallback(entry.panelObject);
            }
        }

        currentPanel = null;
        currentPanelObject = null;
        currentCloseOnEsc = true;
    }
    #endregion

    #region 面板查询
    // 判断指定面板是否已打开。
    public bool IsPanelOpen(string panelKey)
    {
        PanelEntry entry;
        if (!panelLookup.TryGetValue(panelKey, out entry) || entry.panelObject == null)
        {
            return false;
        }

        return entry.panelObject.activeSelf;
    }

    // 获取当前打开的面板Key。
    public string GetCurrentPanelKey()
    {
        return currentPanel != null ? currentPanel.panelKey : string.Empty;
    }
    #endregion

    #region 面板回调
    // 注册面板关闭回调（用于补充关闭时的逻辑）。
    public void RegisterPanelCloseCallback(GameObject panelObject, System.Action onClosed)
    {
        if (panelObject == null)
        {
            return;
        }

        if (onClosed == null)
        {
            closeCallbacks.Remove(panelObject);
            return;
        }

        closeCallbacks[panelObject] = onClosed;
    }
    #endregion

    #region 内部工具
    // 根据配置表构建面板查询字典。
    private void BuildPanelLookup()
    {
        panelLookup.Clear();
        for (int i = 0; i < panels.Count; i++)
        {
            PanelEntry entry = panels[i];
            if (entry == null || string.IsNullOrEmpty(entry.panelKey) || entry.panelObject == null)
            {
                continue;
            }

            if (!panelLookup.ContainsKey(entry.panelKey))
            {
                panelLookup.Add(entry.panelKey, entry);
            }
        }
    }

    // 查找面板对象对应的配置。
    private PanelEntry FindEntry(GameObject panelObject)
    {
        if (panelObject == null)
        {
            return null;
        }

        for (int i = 0; i < panels.Count; i++)
        {
            PanelEntry entry = panels[i];
            if (entry != null && entry.panelObject == panelObject)
            {
                return entry;
            }
        }

        return null;
    }

    // 按ESC关闭当前面板（仅对允许ESC关闭的面板生效）。
    private void CloseCurrentPanelByEsc()
    {
        if (currentPanel != null)
        {
            if (currentPanel.panelObject != null && !currentPanel.panelObject.activeSelf)
            {
                currentPanel = null;
            }
        }

        if (currentPanel != null)
        {
            if (currentPanel.closeOnEsc)
            {
                ClosePanel(currentPanel.panelKey);
            }
            return;
        }

        if (currentPanelObject != null && currentCloseOnEsc)
        {
            ClosePanel(currentPanelObject);
        }
    }

    // 打开面板时处理拆除状态。
    private void HandleDestroyModeOnPanelOpen(GameObject panelObject)
    {
        if (panelObject == null)
        {
            return;
        }

        TabPageManager tabPageManager = FindObjectOfType<TabPageManager>();
        if (tabPageManager != null)
        {
            tabPageManager.CloseDestroyModeIfOpening(panelObject);
        }
    }

    // 调用面板关闭回调。
    private void InvokeCloseCallback(GameObject panelObject)
    {
        System.Action callback;
        if (panelObject != null && closeCallbacks.TryGetValue(panelObject, out callback) && callback != null)
        {
            callback();
        }
    }
    #endregion
}
