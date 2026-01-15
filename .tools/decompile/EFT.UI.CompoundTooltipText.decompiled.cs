using TMPro;
using UnityEngine;

namespace EFT.UI;

public class CompoundTooltipText : UIElement
{
	[SerializeField]
	private TextMeshProUGUI _bodyText;

	public void Show(string text)
	{
		ShowGameObject();
		_bodyText.text = text;
	}
}
