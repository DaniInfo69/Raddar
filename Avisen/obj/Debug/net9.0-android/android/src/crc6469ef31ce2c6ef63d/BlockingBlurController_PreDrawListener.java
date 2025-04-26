package crc6469ef31ce2c6ef63d;


public class BlockingBlurController_PreDrawListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.view.ViewTreeObserver.OnPreDrawListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onPreDraw:()Z:GetOnPreDrawHandler:Android.Views.ViewTreeObserver/IOnPreDrawListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("Sharpnado.MaterialFrame.Droid.BlurView.BlockingBlurController+PreDrawListener, Maui.MaterialFrame", BlockingBlurController_PreDrawListener.class, __md_methods);
	}

	public BlockingBlurController_PreDrawListener ()
	{
		super ();
		if (getClass () == BlockingBlurController_PreDrawListener.class) {
			mono.android.TypeManager.Activate ("Sharpnado.MaterialFrame.Droid.BlurView.BlockingBlurController+PreDrawListener, Maui.MaterialFrame", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean onPreDraw ()
	{
		return n_onPreDraw ();
	}

	private native boolean n_onPreDraw ();

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
