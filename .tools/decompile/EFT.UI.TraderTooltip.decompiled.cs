using UnityEngine;

namespace EFT.UI;

public class TraderTooltip : MonoBehaviour
{
	[SerializeField]
	private CustomTextMeshProUGUI _nickname;

	[SerializeField]
	private CustomTextMeshProUGUI _location;

	[SerializeField]
	private CustomTextMeshProUGUI _description;

	[SerializeField]
	private CustomTextMeshProUGUI _loyaltyLevel;

	[SerializeField]
	private CustomTextMeshProUGUI _standing;

	[SerializeField]
	private CustomTextMeshProUGUI _moneySpent;

	[SerializeField]
	private GameObject _nextLevelContainer;

	[SerializeField]
	private CustomTextMeshProUGUI _standingRequired;

	[SerializeField]
	private CustomTextMeshProUGUI _moneySpentRequired;

	[SerializeField]
	private CustomTextMeshProUGUI _playerLevelRequired;

	[SerializeField]
	private GameObject _standingMet;

	[SerializeField]
	private GameObject _moneySpentMet;

	[SerializeField]
	private GameObject _playerLevelMet;

	[SerializeField]
	private GameObject _detailsPanel;

	[SerializeField]
	private GameObject _lockedPanel;

	[SerializeField]
	private CustomTextMeshProUGUI _lockedText;

	[SerializeField]
	private GameObject _separator;

	private const string string_0 = "traders/trader_is_locked";

	private const string string_1 = "#747b7e";

	private const string string_2 = "#54c1ff";

	private const string string_3 = "#c40000";

	public void Show(Profile.TraderInfo traderInfo)
	{
		base.gameObject.SetActive(value: true);
		BackendConfigSettingsClass.TraderSettings settings = traderInfo.Settings;
		bool available = traderInfo.Available;
		bool disabled = traderInfo.Disabled;
		_detailsPanel.SetActive(available);
		_separator.SetActive(available);
		_lockedPanel.SetActive(!available);
		_lockedText.text = ((!disabled || available) ? GClass2348.Localized("traders/trader_is_locked") : GClass2348.Localized("TraderError/TraderDisabled"));
		_nickname.text = GClass2348.Localized(settings.Nickname);
		_location.text = "<b>" + GClass2348.Localized("Location") + "</b>: " + GClass2348.Localized(settings.Location);
		_description.text = GClass2348.Localized(settings.Description);
		string text = GClass2348.Localized(GClass3932.GetStandingRating(traderInfo.Standing));
		string moneyString = GClass3932.GetMoneyString(traderInfo.SalesSum);
		string currencyString = GClass3130.GetCurrencyString(settings.Currency);
		_loyaltyLevel.text = string.Format("{0}: <color={1}>{2}</color>", GClass2348.Localized("Loyalty level (LL)"), "#747b7e", traderInfo.LoyaltyLevel);
		_standing.text = string.Format("{0}: <color={1}>{2} ({3})</color>", GClass2348.Localized("Standing"), "#747b7e", traderInfo.Standing, text);
		_moneySpent.text = GClass2348.Localized("Spent") + ": <color=#747b7e>" + moneyString + " " + currencyString + "</color>";
		BackendConfigSettingsClass.TraderLoyaltyLevel nextLoyalty;
		bool flag = traderInfo.TryGetNextLoyalty(out nextLoyalty);
		_nextLevelContainer.SetActive(flag);
		if (flag)
		{
			bool active = traderInfo.Standing >= nextLoyalty.MinStanding;
			bool flag2 = traderInfo.SalesSum >= nextLoyalty.MinSalesSum;
			bool flag3 = traderInfo.ProfileLevel >= nextLoyalty.MinProfileLevel;
			text = GClass2348.Localized(GClass3932.GetStandingRating(nextLoyalty.MinStanding));
			moneyString = GClass3932.GetMoneyString(nextLoyalty.MinSalesSum);
			string text2 = (flag2 ? "#54c1ff" : "#c40000");
			string arg = (flag3 ? "#54c1ff" : "#c40000");
			_standingRequired.text = string.Format("{0}: <color={1}>{2} ({3})</color>", GClass2348.Localized("Standing"), "#747b7e", nextLoyalty.MinStanding, text);
			_moneySpentRequired.text = GClass2348.Localized("To spend") + ": <color=" + text2 + ">" + moneyString + " " + currencyString + "</color>";
			_playerLevelRequired.text = string.Format("{0}: <color={1}>{2}</color>", GClass2348.Localized("Player level"), arg, nextLoyalty.MinProfileLevel);
			_standingMet.SetActive(active);
			_moneySpentMet.SetActive(flag2);
			_playerLevelMet.SetActive(flag3);
		}
		method_0(Input.mousePosition);
	}

	public void Update()
	{
		method_0(Input.mousePosition);
	}

	public void method_0(Vector2 position)
	{
		position.x = Mathf.Clamp(position.x, 0f, Screen.width);
		position.y = Mathf.Clamp(position.y, 0f, Screen.height);
		base.transform.position = position + new Vector2(15f, 15f);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}
}
