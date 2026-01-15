using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace EFT.UI;

public class Tooltip : UIElement
{
	[SerializeField]
	private RectTransform _mainTransform;

	[SerializeField]
	private RectTransform _boundsTransform;

	private Coroutine coroutine_0;

	private Vector2 vector2_0;

	[CompilerGenerated]
	private bool bool_0;

	public bool Displayed
	{
		[CompilerGenerated]
		get
		{
			return bool_0;
		}
		[CompilerGenerated]
		set
		{
			bool_0 = value;
		}
	}

	public virtual void Awake()
	{
		if (_boundsTransform == null)
		{
			_boundsTransform = _mainTransform;
		}
	}

	public virtual void Update()
	{
		method_0(Input.mousePosition);
	}

	public void OnDisable()
	{
		Close();
	}

	public CancellationToken Show(Vector2 offset = default(Vector2), float delay = 0f)
	{
		UI.Dispose();
		vector2_0 = offset;
		StaticManager.KillCoroutine(coroutine_0);
		if (delay > 0f)
		{
			coroutine_0 = GClass855.WaitSeconds(StaticManager.Instance, delay, Display);
		}
		else
		{
			Display();
		}
		Displayed = true;
		return UI.CancellationToken;
	}

	public override void Display()
	{
		base.Display();
		method_0(Input.mousePosition);
	}

	public override void Close()
	{
		base.Close();
		if (coroutine_0 != null)
		{
			StaticManager.KillCoroutine(coroutine_0);
		}
		coroutine_0 = null;
		Displayed = false;
	}

	public void OnApplicationFocus(bool hasFocus)
	{
		if (!hasFocus && Displayed)
		{
			Close();
		}
	}

	public void method_0(Vector2 position)
	{
		_mainTransform.position = position + GClass855.Multiply(vector2_0, (Vector2)_mainTransform.lossyScale);
		GClass949.CorrectPositionResolution(_mainTransform, _boundsTransform);
	}
}
