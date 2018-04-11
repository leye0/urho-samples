using Android.App;
using Android.Content.PM;
using Android.Widget;
using Android.OS;
using Android.Views;
using Urho.Droid;
using DeviceMotion.Plugin;
using DeviceMotion.Plugin.Abstractions;
using System;

namespace SamplyGame.Droid
{
	[Activity(Label = "SamplyGame", MainLauncher = true, 
		Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
		ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Landscape)]
	public class MainActivity : Activity
	{
		protected override async void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			var mLayout = new AbsoluteLayout(this);
			var surface = UrhoSurface.CreateSurface(this);// (this, , true);
			mLayout.AddView(surface);
			SetContentView(mLayout);

            CrossDeviceMotion.Current.Start(MotionSensorType.Accelerometer, MotionSensorDelay.Game);                                         
            CrossDeviceMotion.Current.SensorValueChanged += (s, a) => {
                switch (a.SensorType)
                {
                    case MotionSensorType.Accelerometer:
                        Console.WriteLine("A: {0},{1},{2}", ((MotionVector)a.Value).X, ((MotionVector)a.Value).Y, ((MotionVector)a.Value).Z);
                        break;
                    case MotionSensorType.Compass:
                        Console.WriteLine("H: {0}", a.Value);
                        break;
                }
            };

			var app = await surface.Show<SamplyGame>(new Urho.ApplicationOptions("Data"));
		}

		protected override void OnResume()
		{
			UrhoSurface.OnResume();
			base.OnResume();
		}

		protected override void OnPause()
		{
			UrhoSurface.OnPause();
			base.OnPause();
		}

		public override void OnLowMemory()
		{
			UrhoSurface.OnLowMemory();
			base.OnLowMemory();
		}

		protected override void OnDestroy()
		{
			UrhoSurface.OnDestroy();
			base.OnDestroy();
		}

		public override bool DispatchKeyEvent(KeyEvent e)
		{
			if (e.KeyCode == Android.Views.Keycode.Back)
			{
				this.Finish();
				return false;
			}

			if (!UrhoSurface.DispatchKeyEvent(e))
				return false;
			return base.DispatchKeyEvent(e);
		}

		public override void OnWindowFocusChanged(bool hasFocus)
		{
			UrhoSurface.OnWindowFocusChanged(hasFocus);
			base.OnWindowFocusChanged(hasFocus);
		}
	}
}