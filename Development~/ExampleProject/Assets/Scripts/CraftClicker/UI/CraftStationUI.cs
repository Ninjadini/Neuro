using Ninjadini.Neuro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingStationUI : MonoBehaviour
{
    [SerializeField] Image iconImg;
    [SerializeField] Text nameTxt;
    [SerializeField] Button button;

    public Button Button => button;
    public CraftingStation Station { get; private set; }

    public void Populate(CraftingStation station)
    {
        Station = station;
        if (iconImg)
        {
            var settings = NeuroDataProvider.GetSharedSingleton<CraftClickerSettings>();
            var icon = station.Icon.IsEmpty() ? settings.DefaultItemIcon : station.Icon;
            icon.LoadAssetAsync<Sprite>(sprite =>
            {
                iconImg.sprite = sprite;
            });
        }
        if (nameTxt)
        {
            nameTxt.text = station.Name;
        }
    }
}
