namespace Editor
{
	internal class EditorSplashScreen : Widget
	{
		internal static EditorSplashScreen Singleton;

		Pixmap BackgroundImage;

		public EditorSplashScreen() : base( null, true )
		{
			WindowFlags = WindowFlags.Window | WindowFlags.Customized | WindowFlags.WindowTitle | WindowFlags.MSWindowsFixedSizeDialogHint;
			Singleton = this;
			DeleteOnClose = true;

			SetWindowIcon( Pixmap.FromFile( "window_icon.png" ) );
			WindowTitle = "Opening s&box Editor";
			BackgroundImage = Pixmap.FromFile( "new_splash.png" );

			// load any saved geometry
			string geometryCookie = EditorCookie.GetString( "splash.geometry", null );
			RestoreGeometry( geometryCookie );

			Size = new( BackgroundImage.Width, BackgroundImage.Height );

			if ( geometryCookie is null )
			{
				// fallback to screen centre if there's no saved geo
				Position = ScreenGeometry.Contain( Size ).Position;
			}

			Show();

			//
			// Resample background image if dpi scale is gonna make us draw it bigger
			//
			if ( DpiScale != 1.0f )
			{
				BackgroundImage = BackgroundImage.Resize( BackgroundImage.Size * DpiScale );
			}

			ConstrainToScreen();

			g_pToolFramework2.SetStallMonitorMainThreadWindow( _widget );

			Logging.OnMessage += OnConsoleMessage;
		}

		public override void OnDestroyed()
		{
			Logging.OnMessage -= OnConsoleMessage;

			base.OnDestroyed();
			Singleton = null;
		}

		void OnConsoleMessage( LogEvent e )
		{
			OnMessage( e.Message );

			g_pToolFramework2.Spin();
			NativeEngine.EngineGlobal.ToolsStallMonitor_IndicateActivity();
		}

		public static void StartupFinish()
		{
			if ( Singleton.IsValid() )
			{
				EditorCookie.Set( "splash.geometry", Singleton.SaveGeometry() );
				Singleton.Destroy();
			}

			Singleton = null;
		}

		const int MaxMessageCount = 30;
		LinkedList<string> MessageList = new();

		public void OnMessage( string message )
		{
			MessageList.AddLast( message );

			if ( MessageList.Count > MaxMessageCount )
			{
				MessageList.RemoveFirst();
			}

			Update();
		}

		protected override bool OnClose()
		{
			return false;
		}

		protected override void OnPaint()
		{
			Paint.Draw( LocalRect, BackgroundImage );

			Paint.SetPen( Color.Magenta );

			string visibleMessages = string.Join( "\n", MessageList );
			Paint.SetFont( "Century Gothic", 16 );
			Paint.DrawText( LocalRect.Shrink( 32 ), visibleMessages, TextFlag.LeftBottom );
		}

	}
}
