using Sandbox;
using Sandbox.UI;
using System;

namespace MrMix;

public sealed class MrMixMainMenuHud : PanelComponent
{
	[Property] public string MusicEvent { get; set; } = "sounds/music/menu.sound"; // Моя музыка

	private SoundHandle _music;
	private Panel _root;
	private StartButton _startButton;

	protected override void OnTreeFirstBuilt()
	{
		base.OnTreeFirstBuilt();

		_root = new Panel
		{
			Parent = Panel
		};
		_root.Style.Width = Length.Percent( 100 );
		_root.Style.Height = Length.Percent( 100 );
		_root.Style.Position = PositionMode.Absolute;
		_root.Style.Left = 0;
		_root.Style.Top = 0;

		_root.Style.BackgroundColor = new Color( 0, 0, 0, 0.85f );

		_startButton = new StartButton( "START" )
		{
			Parent = _root
		};

		_startButton.Style.Width = 320;
		_startButton.Style.Height = 84;

		_startButton.Style.Position = PositionMode.Absolute;
		_startButton.Style.Left = Length.Percent( 50 );
		_startButton.Style.Top = Length.Percent( 60 );
		_startButton.Style.MarginLeft = -160;
		_startButton.Style.MarginTop = -42;

		_startButton.Clicked += OnStartClicked;

		TryPlayMenuMusic();
	}

	private void OnStartClicked()
	{
		Game.ActiveScene.LoadFromFile( "scenes/level1.scene" ); // Вот тут мы начинаем играть
	}

	private void TryPlayMenuMusic()
	{
		if ( string.IsNullOrWhiteSpace( MusicEvent ) )
		{
			Log.Warning( "MusicEvent empty - menu music won't play." );
			return;
		}

		try
		{
			Sound.Preload( MusicEvent );

			_music = Sound.Play( MusicEvent, 0.25f ); // перезагрузка музыки не работает
			Log.Info( $"Menu music started: {MusicEvent}" );
		}
		catch ( Exception e )
		{
			Log.Error( $"Failed to play music event '{MusicEvent}': {e}" );
		}
	}

	protected override void OnDestroy()
	{
		if ( _music.IsValid )
		{
			_music.Stop( 0.25f );
		}

		base.OnDestroy();
	}

	private sealed class StartButton : Panel
	{
		public event Action Clicked;

		private readonly Label _label;

		public StartButton( string text )
		{
			Style.PointerEvents = PointerEvents.All;
			Style.Cursor = "pointer";

			//Временная кнопочка, сделаем мы ее по красивее потом.
			Style.BackgroundColor = new Color( 0.15f, 0.15f, 0.15f, 0.95f );
			Style.BorderTopLeftRadius = 10;
			Style.BorderTopRightRadius = 10;
			Style.BorderBottomLeftRadius = 10;
			Style.BorderBottomRightRadius = 10;

			Style.JustifyContent = Justify.Center;
			Style.AlignItems = Align.Center;

			_label = new Label( text )
			{
				Parent = this
			};
			_label.Style.FontSize = 42;
			_label.Style.FontWeight = 700;
			_label.Style.FontColor = Color.White;
		}

		protected override void OnClick( MousePanelEvent e )
		{
			base.OnClick( e );
			Clicked?.Invoke();
		}
	}
}