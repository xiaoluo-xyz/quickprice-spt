using System.Threading;
using EFT.InventoryLogic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EFT.UI;

public class SimpleTooltip : Tooltip
{
	private readonly Vector2 vector2_1 = new Vector2(10f, 10f);

	[FormerlySerializedAs("_text")]
	[SerializeField]
	private TextMeshProUGUI _label;

	private LayoutElement layoutElement_0;

	private float float_0;

	public override void Awake()
	{
		base.Awake();
		layoutElement_0 = base.gameObject.GetComponent<LayoutElement>();
		if (layoutElement_0 != null)
		{
			float_0 = layoutElement_0.preferredWidth;
		}
	}

	public CancellationToken Show(string text, Vector2? offset = null, float delay = 0f, float? maxWidth = null)
	{
		SetText(text);
		CancellationToken result = Show(offset ?? vector2_1, delay);
		_label.color = new Color(_label.color.r, _label.color.g, _label.color.b, 1f);
		if (layoutElement_0 != null)
		{
			layoutElement_0.preferredWidth = maxWidth ?? float_0;
		}
		return result;
	}

	public void ShowInventoryError(InventoryError error)
	{
		Show("<color=red>" + error.GetLocalizedDescription() + "</color>");
	}

	public void ShowWarning(InventoryError warning)
	{
		Show("<color=orange>" + warning.GetLocalizedDescription() + "</color>");
	}

	public void SetText(string text)
	{
		_label.text = text;
	}
}
