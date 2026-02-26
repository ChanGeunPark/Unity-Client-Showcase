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


    private const int GridColumnCount = 5;
    private const float DiagonalDelayStep = 0.1f;

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

        for (int index = 0; index < sortedCharts.Count; index++)
        {
            var chart = sortedCharts[index];
            int row = index / GridColumnCount;
            int col = index % GridColumnCount;
            float diagonalDelay = (row + col) * DiagonalDelayStep;

            var characterData = GameDataManager.Instance.Store.CharacterTable?.Characters?.Find(x => x.CharacterId == chart.CharacterId) ?? null;
            var holder = UIManager.Instance.MakeItemHolder<CharacterHolder>(parent);
            holder.Initialize(chart, characterData);
            holder.ShowAnimation(diagonalDelay, index);
        }
    }
}
