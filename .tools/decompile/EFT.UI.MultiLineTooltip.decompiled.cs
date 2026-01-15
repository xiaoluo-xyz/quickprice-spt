using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

namespace EFT.UI;

public class MultiLineTooltip : Tooltip
{
	[Serializable]
	[CompilerGenerated]
	public class Class3174
	{
		public static readonly Class3174 class3174_0 = new Class3174();

		public static Action<MultiLineRow> action_0;

		public void method_0(MultiLineRow s)
		{
			s.HideGameObject();
		}
	}

	[SerializeField]
	private MultiLineRow _multiLineRow;

	[SerializeField]
	private TextMeshProUGUI _header;

	private readonly List<MultiLineRow> list_0 = new List<MultiLineRow>();

	public void Show(GClass3843 multiLineInfo)
	{
		if (multiLineInfo.Header != null || multiLineInfo.Lines != null)
		{
			GClass3842[] lines = multiLineInfo.Lines;
			method_1(GClass2348.Localized(multiLineInfo.Header));
			method_2(lines.Length, list_0);
			method_3(lines, list_0);
			Show();
		}
	}

	public void method_1(string header)
	{
		_header.text = header;
	}

	public void method_2(int rowsNeededCount, List<MultiLineRow> rowList)
	{
		int num = rowsNeededCount - rowList.Count;
		for (int i = 0; i < num; i++)
		{
			MultiLineRow item = UnityEngine.Object.Instantiate(_multiLineRow, _multiLineRow.transform.parent, worldPositionStays: false);
			rowList.Add(item);
		}
		rowList.ForEach(delegate(MultiLineRow s)
		{
			s.HideGameObject();
		});
	}

	public void method_3(GClass3842[] singleLineInfo, List<MultiLineRow> rowList)
	{
		for (int i = 0; i < singleLineInfo.Length; i++)
		{
			rowList[i].Show(singleLineInfo[i]);
		}
	}
}
