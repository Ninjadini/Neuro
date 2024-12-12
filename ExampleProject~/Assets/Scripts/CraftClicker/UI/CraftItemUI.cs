using System;
using System.Text;
using Ninjadini.Neuro;
using Ninjadini.Neuro.Utils;
using UnityEngine;
using UnityEngine.UI;

public class CraftItemUI : MonoBehaviour
{
    [SerializeField] Image iconImg;
    [SerializeField] Text nameTxt;
    [SerializeField] Text requirementsTxt;
    [SerializeField] Text ownedTxt;
    [SerializeField] Slider progressSlider;
    [SerializeField] Button button;

    CraftItem _item;
    CraftClickerLogic _logic;
    DateTime? _drawnSaveTime;

    public Button Button => button;
    public CraftItem Item => _item;
    
    public void Populate(CraftItem item, CraftClickerLogic logic)
    {
        _item = item;
        _logic = logic;
        _drawnSaveTime = null;
        if (iconImg)
        {
            iconImg.enabled = false;
            var settings = NeuroDataProvider.GetSharedSingleton<CraftClickerSettings>();
            var icon = item.Icon.IsEmpty() ? settings.DefaultItemIcon : item.Icon;
            icon.LoadAssetAsync<Sprite>(sprite =>
            {
                if (sprite)
                {
                    iconImg.sprite = sprite;
                    iconImg.enabled = true;
                }
            });
        }
        if (nameTxt)
        {
            nameTxt.text = $"{item.CraftOutputCount}x {item.Name}";
        }
        Update();
    }

    void Update()
    {
        if (_item == null)
        {
            return;
        }

        if (_drawnSaveTime != _logic.LastSaveTime)
        {
            _drawnSaveTime = _logic.LastSaveTime;
            UpdateCounts();
        }
        UpdateProgressBar();
    }

    void UpdateCounts()
    {
        if (ownedTxt)
        {
            ownedTxt.text = $"Owned: {_logic.GetOwnedCount(_item)}";
        }
        if (requirementsTxt)
        {
            requirementsTxt.text = GetRequirementsTxt();
        }
    }

    static readonly StringBuilder _tempStringBuilder = new ();
    string GetRequirementsTxt()
    {
        _tempStringBuilder.Clear();
        if (_item.RequiredItems is { Count: > 0 })
        {
            _tempStringBuilder.AppendLine("\nRequires");
            foreach (var requiredItem in _item.RequiredItems)
            {
                var ownedReqCount = _logic.GetOwnedCount(requiredItem.Item);
                _tempStringBuilder
                    .Append(requiredItem.Item.GetValue().Name)
                    .Append(" (");
                if (ownedReqCount < requiredItem.Amount)
                {
                    _tempStringBuilder.Append("<color=red>");
                }
                _tempStringBuilder
                    .AppendNum(_logic.GetOwnedCount(requiredItem.Item))
                    .Append("/")
                    .AppendNum(requiredItem.Amount);
                if (ownedReqCount < requiredItem.Amount)
                {
                    _tempStringBuilder.Append("</color>");
                }
                _tempStringBuilder.AppendLine(")");
            }
        }
        return _tempStringBuilder.ToString();
    }

    void UpdateProgressBar()
    {
        if (!progressSlider)
        {
            return;
        }
        var progress = _logic.GetCraftingProgress(_item);
        if (progress.HasValue)
        {
            progressSlider.value = progress.Value;
            progressSlider.gameObject.SetActive(true);
        }
        else
        {
            progressSlider.gameObject.SetActive(false);
        }
    }
}
