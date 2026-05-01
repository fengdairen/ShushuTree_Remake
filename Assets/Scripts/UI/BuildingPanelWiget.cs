using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BuildingPanelWiget : MonoBehaviour
{

    public Button[] BuildingList = new Button[5];
    public int pagenum = 0;
    public BuilidingPool buildingPool;
    public Button nextButton;
    public Button prevButton;
    private bool isListEnd = false;
    public string tabPageChoice = "自然能量";
    public string iconFolderPath = "BuildingIcons"; // 假设在Resources文件夹下
    public Text buildingText;
    [HideInInspector]
    public int selectedBuildingId = -1;

    void Start()
    {
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PrevPage);
        UpdatePage();
        for(int i = 0; i < BuildingList.Length; i++)
        {
            int index = i;
            BuildingList[i].onClick.AddListener(() => OnBuildingClick(index));
        }
    }

    public void UpdatePage()
    {
        selectedBuildingId = -1;
        var filteredBuildings = buildingPool.buildings.Where(b => b.type == tabPageChoice).ToList();
        int start = pagenum * 5;
        isListEnd = (start + 5 >= filteredBuildings.Count);
        for (int i = 0; i < 5; i++)
        {
            int index = start + i;
            if (index < filteredBuildings.Count)
            {
                BuildingList[i].GetComponentInChildren<Text>().text = filteredBuildings[index].Name;
                BuildingList[i].gameObject.SetActive(true);
                Sprite icon = Resources.Load<Sprite>(iconFolderPath + "/" + filteredBuildings[index].id.ToString());
                if (icon != null)
                {
                    Image[] images = BuildingList[i].GetComponentsInChildren<Image>();
                    images[1].sprite = icon;
                    Debug.Log($"Loaded icon for building ID {filteredBuildings[index].id} from path {iconFolderPath}/{filteredBuildings[index].id}");
                    
                }
                else
                {
                    Debug.Log($"Icon not found for building ID {filteredBuildings[index].id} at path {iconFolderPath}/{filteredBuildings[index].id}");
                }
            }
            else
            {
                BuildingList[i].gameObject.SetActive(false);
            }
        }
    }

    void NextPage()
    {
        
        if (isListEnd == false)
        {
            pagenum++;
            UpdatePage();

        }
    }

    void PrevPage()
    {
        if (pagenum > 0)
        {
            pagenum--;
            UpdatePage();
            isListEnd = false;
        }
        
    }

    void OnBuildingClick(int index)
    {
        var filteredBuildings = buildingPool.buildings.Where(b => b.type == tabPageChoice).ToList();
        int buildingIndex = pagenum * 5 + index;
        if (buildingIndex < filteredBuildings.Count)
        {
            Building selectedBuilding = filteredBuildings[buildingIndex];
            selectedBuildingId = selectedBuilding.id;
            if (buildingText != null)
            {
                buildingText.text = buildingPool != null ? buildingPool.GetBuildingDisplayText(selectedBuilding) : selectedBuilding.text;
            }
        }
    }
}
