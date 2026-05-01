using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DataPanelWiget : MonoBehaviour
{
    public Text natureEnergyText;
    public Text fruitEnergyText;
    public Text rootEnergyText;
        // 记录上一次显示的数值，只有发生变化时才更新 UI，减少不必要的赋值
        private int prevNature = int.MinValue;
        private int prevFruit = int.MinValue;
        private int prevRoot = int.MinValue;

        // 在开始时强制刷新一次显示
        private void Start()
        {
            RefreshAll();
        }

        // 每帧检查 BaseData 中的值，若有变化则更新对应的 Text
        private void Update()
        {
            var bd = BaseData.instance;
            if (bd == null) return;

            int natureEnergy = bd.GetBlackboardValue(BaseData.BlackboardKeys.NatureEnergy, bd.natureEnergy);
            int fruitEnergy = bd.GetBlackboardValue(BaseData.BlackboardKeys.FruitEnergy, bd.fruitEnergy);
            int rootEnergy = bd.GetBlackboardValue(BaseData.BlackboardKeys.RootEnergy, bd.rootEnergy);

            if (natureEnergy != prevNature)
            {
                prevNature = natureEnergy;
                if (natureEnergyText != null) natureEnergyText.text = prevNature.ToString();
            }

            if (fruitEnergy != prevFruit)
            {
                prevFruit = fruitEnergy;
                if (fruitEnergyText != null) fruitEnergyText.text = prevFruit.ToString();
            }

            if (rootEnergy != prevRoot)
            {
                prevRoot = rootEnergy;
                if (rootEnergyText != null) rootEnergyText.text = prevRoot.ToString();
            }
        }

        // 强制全部刷新一次（用于初始化）
        private void RefreshAll()
        {
            var bd = BaseData.instance;
            if (bd == null) return;
            prevNature = bd.GetBlackboardValue(BaseData.BlackboardKeys.NatureEnergy, bd.natureEnergy);
            prevFruit = bd.GetBlackboardValue(BaseData.BlackboardKeys.FruitEnergy, bd.fruitEnergy);
            prevRoot = bd.GetBlackboardValue(BaseData.BlackboardKeys.RootEnergy, bd.rootEnergy);

            if (natureEnergyText != null) natureEnergyText.text = prevNature.ToString();
            if (fruitEnergyText != null) fruitEnergyText.text = prevFruit.ToString();
            if (rootEnergyText != null) rootEnergyText.text = prevRoot.ToString();
        }

}
