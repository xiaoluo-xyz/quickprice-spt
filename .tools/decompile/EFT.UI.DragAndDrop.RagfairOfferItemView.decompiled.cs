using System;
using System.Runtime.CompilerServices;
using Diz.Binding;
using EFT.InventoryLogic;
using EFT.UI.Ragfair;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EFT.UI.DragAndDrop;

public class RagfairOfferItemView : StaticGridItemView
{
	[Serializable]
	[CompilerGenerated]
	public class Class3357
	{
		public static readonly Class3357 class3357_0 = new Class3357();

		public static Predicate<ItemAttributeClass> predicate_0;

		public bool method_0(ItemAttributeClass x)
		{
			return x.Id.Equals(EItemAttributeId.Durability);
		}
	}

	private const float float_4 = 10f;

	private const int int_0 = 330;

	private const int int_1 = 125;

	[SerializeField]
	private LayoutElement _layoutElement;

	[SerializeField]
	private DurabilitySlider _durabilitySlider;

	private ItemAttributeClass itemAttributeClass;

	private bool bool_8;

	private Offer offer_0;

	public override ItemRotation ItemRotation
	{
		get
		{
			return _itemRotation;
		}
		set
		{
			_itemRotation = value;
			if (bool_8)
			{
				_itemRotation = value;
				MainImage.transform.rotation = ((value == ItemRotation.Horizontal) ? ItemViewFactory.HorizontalRotation : ItemViewFactory.VerticalRotation);
				if (!(base.RectTransform.anchorMin != base.RectTransform.anchorMax))
				{
					XYCellSizeStruct cellPixelSize = ItemViewFactory.GetCellPixelSize(base.Item.CalculateRotatedSize(_itemRotation));
					Vector2 sizeDelta = new Vector2(Mathf.Clamp(cellPixelSize.X, 0, 330), Mathf.Clamp(cellPixelSize.Y, 0, 125));
					_layoutElement.minWidth = sizeDelta.x;
					_layoutElement.minHeight = sizeDelta.y;
					base.RectTransform.sizeDelta = sizeDelta;
				}
			}
		}
	}

	public override IBindable<float> Transparency => new global::BindableStateClass<float>(1f);

	public override bool PropagateClicks => false;

	public bool Boolean_0
	{
		get
		{
			if (bool_8)
			{
				return base.Item.GetItemComponent<RepairableComponent>() != null;
			}
			return false;
		}
	}

	public static GridItemView Create(Item item, ItemRotation rotation, TraderControllerClass itemController, IItemOwner itemOwner, ItemUiContext itemUiContext, InsuranceCompanyClass insurance)
	{
		RagfairOfferItemView ragfairOfferItemView = ItemViewFactory.CreateFromPool<RagfairOfferItemView>("ragfair_offer_layout");
		ragfairOfferItemView.Show(null, item, rotation, expanded: false, itemController, itemOwner, itemUiContext, insurance);
		return ragfairOfferItemView;
	}

	public void Show(Offer offer, Item item, ItemRotation rotation, bool expanded, TraderControllerClass itemController, IItemOwner itemOwner, ItemUiContext itemUiContext, InsuranceCompanyClass insurance)
	{
		offer_0 = offer;
		bool_8 = expanded;
		NewGridItemView(item, new GClass3453(item, EItemViewType.Ragfair), rotation, itemController, itemOwner, null, null, itemUiContext, insurance, itemUiContext.WishlistManager).Init();
		foreach (GEventArgs6 item2 in ItemOwner.SelectEvents<GEventArgs6>(base.Item))
		{
			SetBeingExaminedState(item2);
		}
		if (!(_durabilitySlider == null) && item.TryGetItemComponent<RepairableComponent>(out var component))
		{
			_durabilitySlider.method_0(component, 10f);
			itemAttributeClass = item.Attributes.Find((ItemAttributeClass x) => x.Id.Equals(EItemAttributeId.Durability));
			itemAttributeClass.OnUpdate -= method_37;
			itemAttributeClass.OnUpdate += method_37;
		}
	}

	public override void OnBeingExaminedChanged(bool isBeingExamined)
	{
		base.OnBeingExaminedChanged(isBeingExamined);
		if (!(_durabilitySlider == null))
		{
			bool active = Boolean_0 && !isBeingExamined;
			_durabilitySlider.gameObject.SetActive(active);
		}
	}

	public override string GetErrorText()
	{
		return null;
	}

	public void method_37()
	{
		_durabilitySlider.method_1(itemAttributeClass.Base());
	}

	public override void UpdateScale()
	{
		Vector2 sizeDelta = MainImage.rectTransform.sizeDelta;
		float x = sizeDelta.x;
		float y = sizeDelta.y;
		int num;
		int num2;
		if (bool_8)
		{
			base.UpdateScale();
			num = 330;
			num2 = 125;
			if (x < 330f && y < (float)num2)
			{
				return;
			}
		}
		else
		{
			num = 64;
			num2 = 64;
		}
		float num3 = Mathf.Min((float)num / x, (float)num2 / y);
		MainImage.rectTransform.sizeDelta = new Vector2(x * num3, y * num3);
	}

	public override void SetItemValue(EItemValueFormat format, bool display, string color, object arg1, object arg2 = null, string color2 = null)
	{
		if (offer_0 != null)
		{
			string text = offer_0.TotalItemCount.ToString();
			if ((offer_0.BuyRestrictionMax <= 0 && offer_0.UnlimitedCount) || (float)offer_0.TotalItemCount > Mathf.Pow(10f, 5f) - 1f)
			{
				text = GClass2348.Localized("A LOT");
			}
			UpdateItemValue("<color=#b6c1c7>" + text + "</color>");
		}
	}

	public override void UpdateInfo()
	{
		base.UpdateInfo();
		Caption.gameObject.SetActive(value: false);
		SetInscriptionVisibility(visible: false);
		base.SecureIcon.gameObject.SetActive(value: false);
		base.LockedIcon.gameObject.SetActive(value: false);
		base.TogglableIcon.gameObject.SetActive(value: false);
	}

	public override void OnClick(PointerEventData.InputButton button, Vector2 position, bool doubleClick)
	{
		switch (button)
		{
		case PointerEventData.InputButton.Left:
			if (doubleClick)
			{
				base.NewContextInteractions.ExecuteInteraction(EItemInfoButton.Inspect);
			}
			break;
		case PointerEventData.InputButton.Right:
			ShowContextMenu(position);
			break;
		case PointerEventData.InputButton.Middle:
			ExecuteMiddleClick();
			break;
		}
	}
}
