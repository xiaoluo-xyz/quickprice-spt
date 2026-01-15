using UnityEngine;

namespace EFT.UI;

public class ResizeTooltip : UIElement
{
	[SerializeField]
	private RectTransform _oldTiles;

	[SerializeField]
	private ColorBlinker _newTiles;

	[SerializeField]
	private RectTransform _upArrow;

	[SerializeField]
	private RectTransform _rightArrow;

	private const int int_0 = 10;

	private static readonly Color color_0 = new Color32(222, 0, 0, byte.MaxValue);

	private Vector2 vector2_0;

	private RectTransform rectTransform_1;

	public void Show(XYCellSizeStruct oldSize, XYCellSizeStruct newSize, Vector2 tooltipOffset, RectTransform parentTooltip)
	{
		ShowGameObject();
		vector2_0 = tooltipOffset;
		rectTransform_1 = parentTooltip;
		XYCellSizeStruct cellSize = newSize - oldSize;
		XYCellSizeStruct xYCellSizeStruct = smethod_0(cellSize);
		_rightArrow.gameObject.SetActive(cellSize.X > 0);
		_upArrow.gameObject.SetActive(cellSize.Y > 0);
		_rightArrow.anchoredPosition = new Vector2(10 - xYCellSizeStruct.X + 1, 0f);
		_upArrow.anchoredPosition = new Vector2(0f, xYCellSizeStruct.Y - 10 - 1);
		XYCellSizeStruct xYCellSizeStruct2 = smethod_0(oldSize);
		XYCellSizeStruct xYCellSizeStruct3 = smethod_0(newSize);
		_oldTiles.sizeDelta = xYCellSizeStruct2;
		_oldTiles.anchoredPosition = new Vector2(0f, (xYCellSizeStruct3 - xYCellSizeStruct2).Y);
		((RectTransform)base.transform).sizeDelta = xYCellSizeStruct3;
		_newTiles.EndColor = color_0;
	}

	public static XYCellSizeStruct smethod_0(XYCellSizeStruct cellSize)
	{
		return cellSize * 10 + new XYCellSizeStruct(1, 1);
	}

	public void Update()
	{
		base.transform.position = rectTransform_1.position - new Vector3(((RectTransform)base.transform).sizeDelta.x, 0f) + (Vector3)vector2_0;
	}
}
