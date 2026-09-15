using System;
using UnityEngine;

public class BattleUIManager : MonoBehaviour
{
    [SerializeField] Transform _uniUICutsceneParent;
    public Transform UnitUICutsceneParent => _uniUICutsceneParent;

    [SerializeField] TurnDisplayObject _turnDisplayPrefab;
    [SerializeField] ActionDisplayObject _actionDisplayPrefab;
    [SerializeField] Transform _turnDisplayParent;
    [SerializeField] Transform _actionDisplayParent;

    public void ClearBattleUI()
    {
        ClearChildren(_turnDisplayParent);
        ClearChildren(_actionDisplayParent);
        ClearChildren(_uniUICutsceneParent);
    }

    static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public void ShowTurn(int[] turnOrder)
    {
        ClearChildren(_turnDisplayParent);
        for (int i = 0; i < turnOrder.Length; i++)
        {
            BattleUnit unit = BattleManager.Instance.GetUnit(turnOrder[i]);
            TurnDisplayObject obj = Instantiate(_turnDisplayPrefab, _turnDisplayParent);
            if (i == 0) obj.image.color = Color.yellow;
            obj.text.text = $"{unit.Team}>{unit.Info_Name}({unit.Id})";
        }

    }

    public void DisplayActions(params Tuple<BattleAction, string>[] data)
    {
        ClearChildren(_actionDisplayParent);
        for (int i = 0; i < data.Length; i++)
        {
            ActionDisplayObject obj = Instantiate(_actionDisplayPrefab, _actionDisplayParent);
            obj.Name.text = $"{data[i].Item2} - {data[i].Item1.Name}";
            obj.Desc.text = $"{data[i].Item1.Description}";
        }
    }
}
