
using UnityEngine;

namespace ImageAndVideoPicker
{

	public class AndroidPicker
	{
		#if UNITY_ANDROID
		static AndroidJavaClass _plugin;

		static AndroidPicker()
		{
			if(Application.platform == RuntimePlatform.Android)
				_plugin = new AndroidJavaClass("com.astricstore.imageandvideopicker.AndroidPicker");
		}

		public static void BrowseImage()
		{
			if(Application.platform == RuntimePlatform.Android)
				_plugin.CallStatic("BrowseForImage",false,1,1);
			
		}

		public static void BrowseImage(bool cropping, int aspectX = 1, int aspectY = 1)
		{
			if(Application.platform == RuntimePlatform.Android)
				_plugin.CallStatic("BrowseForImage",cropping,aspectX,aspectY);

		}

		public static void BrowseForMultipleImage()
		{
			if(Application.platform == RuntimePlatform.Android)
				_plugin.CallStatic("BrowseForMultipleImage");

		}

		public static void BrowseVideo()
		{
			if(Application.platform == RuntimePlatform.Android)
				_plugin.CallStatic("BrowseForVideo");

		}

		public static void BrowseForMultipleVideo()
		{
			if(Application.platform == RuntimePlatform.Android)
				_plugin.CallStatic("BrowseForMultipleVideo");

		}

		public static void BrowseContact()
		{
			if(Application.platform == RuntimePlatform.Android)
				_plugin.CallStatic("BrowseForContact");
			
		}
#endif
	}
}


