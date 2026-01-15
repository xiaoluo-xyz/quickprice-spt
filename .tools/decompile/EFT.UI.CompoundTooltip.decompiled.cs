using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EFT.UI;

public class CompoundTooltip : Tooltip
{
	[Serializable]
	[CompilerGenerated]
	public class Class3172
	{
		public static readonly Class3172 class3172_0 = new Class3172();

		public static Action<CompoundTooltipHeader> action_0;

		public static Action<CompoundTooltipText> action_1;

		public void method_0(CompoundTooltipHeader s)
		{
			s.HideGameObject();
		}

		public void method_1(CompoundTooltipText s)
		{
			s.HideGameObject();
		}
	}

	[SerializeField]
	private CompoundTooltipHeader _header;

	[SerializeField]
	private CompoundTooltipText _bodyText;

	private readonly List<CompoundTooltipHeader> list_0 = new List<CompoundTooltipHeader>();

	private readonly List<CompoundTooltipText> list_1 = new List<CompoundTooltipText>();

	public void Show(List<GClass3841> textInfos, float delay = 0f)
	{
		method_1(textInfos.Count);
		method_2(textInfos);
		Show(default(Vector2), delay);
	}

	public void method_1(int blocksNeeded)
	{
		int num = blocksNeeded - list_0.Count;
		int num2 = blocksNeeded - list_1.Count;
		for (int i = 0; i < num; i++)
		{
			CompoundTooltipHeader item = UnityEngine.Object.Instantiate(_header, _header.transform.parent, worldPositionStays: false);
			list_0.Add(item);
		}
		for (int j = 0; j < num2; j++)
		{
			CompoundTooltipText item2 = UnityEngine.Object.Instantiate(_bodyText, _bodyText.transform.parent, worldPositionStays: false);
			list_1.Add(item2);
		}
		list_0.ForEach(delegate(CompoundTooltipHeader s)
		{
			s.HideGameObject();
		});
		list_1.ForEach(delegate(CompoundTooltipText s)
		{
			s.HideGameObject();
		});
	}

	public void method_2(List<GClass3841> textInfos)
	{
		for (int i = 0; i < textInfos.Count; i++)
		{
			list_0[i].Show(textInfos[i].Header);
			list_1[i].Show(textInfos[i].Text);
		}
	}
}
