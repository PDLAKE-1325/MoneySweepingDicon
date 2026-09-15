using System;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelectionUI : MonoBehaviour
{
    [SerializeField] Text _nameText;
    [SerializeField] Text _descriptionText;
    [SerializeField] Image _background;
    [SerializeField] Color _selectedColor = new Color(0.72f, 0.82f, 0.58f);
    [SerializeField] Color _unselectedColor = new Color(0.5f, 0.5f, 0.5f);
    int _index;
    string _name;
    Action<int> _onSelected;
    public void Bind(int index, BattleUnit unit, bool selected, Action<int> onSelected)
    {
        _index = index;
        _name = unit.Info_Name;
        _descriptionText.text = unit.Info_Description;
        _onSelected = onSelected;
        SetSelected(selected);
    }
    public void SetSelected(bool selected)
    {
        _nameText.text = (selected ? "✓  " : "+  ") + _name;
        _background.color = selected ? _selectedColor : _unselectedColor;
    }
    public void Select() => _onSelected?.Invoke(_index);
    void OnDestroy() => _onSelected = null;
}
