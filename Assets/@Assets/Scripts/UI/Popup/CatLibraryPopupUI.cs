using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CatLibraryPopupUI : BasePopupUI
{
    enum Buttons
    {
        CloseButton,
    }

    enum Objects
    {
        CharacterList
    }

    private void Awake()
    {
        BindButton(typeof(Buttons));
        BindObject(typeof(Objects));

        AddButtonListener(Buttons.CloseButton, Close);
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        var listRoot = GetObject(Objects.CharacterList);
        Util.DestroyChilds(listRoot);

        var characterChart = GameDataManager.Instance.Store.CharacterChart;
        if (characterChart == null) return;

        var ownedIds = new HashSet<string>(
            GameDataManager.Instance.Store.CharacterTable?.Characters?.Select(c => c.CharacterId) ?? Enumerable.Empty<string>());
        var sortedCharts = characterChart.OrderBy(chart => !ownedIds.Contains(chart.CharacterId)).ToList();

        Transform parent = listRoot.transform;
        foreach (var chart in sortedCharts)
        {
            var characterData = GameDataManager.Instance.Store.CharacterTable?.Characters?.Find(x => x.CharacterId == chart.CharacterId) ?? null;
            var holder = UIManager.Instance.MakeItemHolder<CharacterHolder>(parent);
            holder.Initialize(chart, characterData);
        }
    }
}
