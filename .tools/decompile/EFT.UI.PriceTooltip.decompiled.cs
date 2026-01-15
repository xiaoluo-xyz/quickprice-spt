using EFT.InventoryLogic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EFT.UI;

public class PriceTooltip : SimpleTooltip
{
	private const string string_0 = "Trader can't buy this item";

	private const string string_1 = "The item has been sold";

	[SerializeField]
	private Image _currencyIcon;

	[SerializeField]
	private TextMeshProUGUI _price;

	[SerializeField]
	private Color _priceColor;

	[SerializeField]
	private Color _unbuyableColor;

	public void Show(EOwnerType ownerType, string text, int price, MongoID? currencyTemplate)
	{
		Show(text);
		bool flag2;
		bool flag = !(flag2 = !currencyTemplate.HasValue) && price > 0;
		_currencyIcon.gameObject.SetActive(flag);
		_price.color = (flag ? _priceColor : _unbuyableColor);
		if (flag)
		{
			_price.text = price.ToString();
			_currencyIcon.sprite = EFTHardSettings.Instance.StaticIcons.GetSmallCurrencySign(currencyTemplate);
			_currencyIcon.SetNativeSize();
		}
		else if (flag2)
		{
			_price.text = ((ownerType == EOwnerType.Trader) ? GClass2348.Localized("The item has been sold") : GClass2348.Localized("Trader can't buy this item"));
		}
		else
		{
			_price.text = string.Empty;
		}
	}
}
