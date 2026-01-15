using TMPro;
using UnityEngine;

namespace EFT.UI;

public class CompoundTooltipHeader : UIElement
{
	[SerializeField]
	private TextMeshProUGUI _header;

	public void Show(string text)
	{
		ShowGameObject();
		_header.text = text;
	}
}
