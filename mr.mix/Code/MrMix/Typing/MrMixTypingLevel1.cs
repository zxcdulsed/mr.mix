using System;
using Sandbox;
using Sandbox.UI;

namespace MrMix1;

// Чисто в этом файле опишу как все работает, для себя, на память.
[Title( "Mr.Mix Pitching Level 1" )]
[Category( "MrMix/UI" )]
[Icon( "keyboard", "#000000", "#ffd34d" )]
public sealed class MrMixTypingChallengeHud : PanelComponent
{
	private enum ChallengeState
	{
		Running,
		Won,
		Lost
	}

	// Конфинги
	[Property] public string BackgroundTexture { get; set; } = "/ui/images/mr_mix.png"; // Сам мистер Микс

	[Property] public float TimeLimitSeconds { get; set; } = 60f; //Это время на буквы
	[Property] public int WordsToWin { get; set; } = 5; //Это скольско букв нужно напичатать
	[Property] public string LevelLabel { get; set; } = "Level 1"; //Левелы или же уровни, и так понятно
	[Property] public float WinAutoNextDelaySeconds { get; set; } = 9f; // Время на следующий уровень
	[Property] public string NextLevelScenePath { get; set; } = "scenes/level2.scene"; //Следующий уровень
	[Property] public string RetryScenePath { get; set; } = "scenes/level1.scene"; //Уровень для проигрыша
	[Property] public string MainMenuScenePath { get; set; } = "scenes/mainmenu.scene"; //Меню после проигрыша или победы

	[Property] public string TypeSoundEvent { get; set; } = "sounds/music/snd_type.sound"; //звук пичатания
	[Property] public string MusicEvent { get; set; } = "sounds/music/levels.sound"; // Песня фоновая
	[Property] public string HappySoundEvent { get; set; } = "sounds/music/happy.sound";   // Песня фоновая для победы нашей или вашей
	[Property] public string LossSoundEvent { get; set; } = "sounds/music/loss.sound";     // Песня фоновая для поражения

	//Сам набор слов
	[Property] public string[] WordPool { get; set; } =
	{
		"pepper", "banana", "egg", "broccoli", "strawberry", "onion", "ketchup", "lettuce", "cucumber", "tomato"
	};

	[Property] public Action OnSuccess { get; set; }
	[Property] public Action OnFail { get; set; }

	public float TimeLeft { get; private set; }
	public int WordsCompleted { get; private set; }
	public string CurrentWord { get; private set; } = "";
	public int TypedCount { get; private set; }
	public bool IsWon => _state == ChallengeState.Won;
	public bool IsLost => _state == ChallengeState.Lost;
	public float WinAutoNextLeft => _winAutoNextLeft;

	private ChallengeState _state = ChallengeState.Running;
	private float _winAutoNextLeft;
	private bool _sceneSwitchTriggered;

	private Image _bg;
	private TypingOverlayPanel _overlay;
	private SoundHandle? _music;

	// лист кнопок
	private static readonly string[] LetterKeys =
	{
		"a","b","c","d","e","f","g","h","i","j","k","l","m",
		"n","o","p","q","r","s","t","u","v","w","x","y","z"
	};

	protected override void OnTreeFirstBuilt()
	{
		base.OnTreeFirstBuilt();

		Panel.AddClass( "mrmix-typing-root" );

		_bg = new Image
		{
			Parent = Panel
		};
		_bg.AddClass( "bg" );
		_bg.SetTexture( BackgroundTexture );

		_overlay = new TypingOverlayPanel( this )
		{
			Parent = Panel
		};
	}

	protected override void OnStart()
	{
		base.OnStart();

		PreloadTypeSound();
		PreloadHappySound();
		PreloadLossSound();
		PlayMusic();
		Restart();
	}

	private void PreloadTypeSound()
	{
		if ( string.IsNullOrWhiteSpace( TypeSoundEvent ) )
			return;

		try
		{
			Sound.Preload( TypeSoundEvent );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[MrMixTypingChallenge] Failed to preload type sound '{TypeSoundEvent}': {e}" );
		}
	}

	private void PreloadHappySound()
	{
		if ( string.IsNullOrWhiteSpace( HappySoundEvent ) )
			return;

		try
		{
			Sound.Preload( HappySoundEvent );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[MrMixTypingChallenge] Failed to preload happy sound '{HappySoundEvent}': {e}" );
		}
	}

	private void PreloadLossSound()
	{
		if ( string.IsNullOrWhiteSpace( LossSoundEvent ) )
			return;

		try
		{
			Sound.Preload( LossSoundEvent );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[MrMixTypingChallenge] Failed to preload loss sound '{LossSoundEvent}': {e}" );
		}
	}

	private void PlayTypeSound()
	{
		if ( string.IsNullOrWhiteSpace( TypeSoundEvent ) )
			return;

		try
		{
			Sound.Play( TypeSoundEvent, 0f );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[MrMixTypingChallenge] Failed to play type sound '{TypeSoundEvent}': {e}" );
		}
	}

	private void PlayHappySound()
	{
		if ( string.IsNullOrWhiteSpace( HappySoundEvent ) )
			return;

		try
		{
			Sound.Play( HappySoundEvent, 0f );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[MrMixTypingChallenge] Failed to play happy sound '{HappySoundEvent}': {e}" );
		}
	}

	private void PlayLossSound()
	{
		if ( string.IsNullOrWhiteSpace( LossSoundEvent ) )
			return;

		try
		{
			Sound.Play( LossSoundEvent, 0f );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[MrMixTypingChallenge] Failed to play loss sound '{LossSoundEvent}': {e}" );
		}
	}

	private void PlayMusic()
	{
		if ( string.IsNullOrWhiteSpace( MusicEvent ) )
			return;

		try
		{
			Sound.Preload( MusicEvent );

			_music?.Stop();
			_music = Sound.Play( MusicEvent, 0f );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[MrMixTypingChallenge] Failed to play music '{MusicEvent}': {e}" );
		}
	}

	[Button( "Restart Challenge" )]
	public void Restart()
	{
		TimeLeft = TimeLimitSeconds;
		WordsCompleted = 0;
		TypedCount = 0;
		_state = ChallengeState.Running;
		_winAutoNextLeft = 0f;
		_sceneSwitchTriggered = false;

		_music?.Stop();
		PickNewWord();
		_overlay?.Refresh();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( _state == ChallengeState.Running )
		{
			if ( _music == null || !_music.IsValid || !_music.IsPlaying )
			{
				PlayMusic();
			}
		}

		if ( _state == ChallengeState.Won )
		{
			UpdateWinFlow();
			_overlay?.Refresh();
			return;
		}

		if ( _state == ChallengeState.Lost )
		{
			UpdateLoseFlow();
			_overlay?.Refresh();
			return;
		}

		TimeLeft = MathF.Max( 0f, TimeLeft - Time.Delta );

		HandleTypingInput();

		if ( TimeLeft <= 0f )
		{
			Fail();
			return;
		}

		_overlay?.Refresh();
	}

	private void HandleTypingInput()
	{
		//бекспейс
		if ( Input.Keyboard.Pressed( "backspace" ) && TypedCount > 0 )
		{
			TypedCount--;
			return;
		}

		for ( int i = 0; i < LetterKeys.Length; i++ )
		{
			var key = LetterKeys[i];
			if ( !Input.Keyboard.Pressed( key ) )
				continue;

			OnTypedChar( key[0] );
			return;
		}
	}

	private void OnTypedChar( char c )
	{
		if ( string.IsNullOrEmpty( CurrentWord ) ) return;
		if ( TypedCount >= CurrentWord.Length ) return;

		char expected = char.ToLowerInvariant( CurrentWord[TypedCount] );
		char got = char.ToLowerInvariant( c );

		if ( got != expected )
			return;

		PlayTypeSound();
		TypedCount++;

		if ( TypedCount >= CurrentWord.Length )
		{
			WordsCompleted++;

			if ( WordsCompleted >= WordsToWin )
			{
				Success();
				return;
			}

			PickNewWord();
		}
	}

	private void PickNewWord()
	{
		if ( WordPool == null || WordPool.Length == 0 )
		{
			CurrentWord = "wordpool_empty";
			TypedCount = 0;
			return;
		}

		CurrentWord = WordPool[Game.Random.Int( 0, WordPool.Length - 1 )];
		TypedCount = 0;
	}

	private void Success()
	{
		_state = ChallengeState.Won;
		_music?.Stop();
		PlayHappySound();
		_winAutoNextLeft = WinAutoNextDelaySeconds;
		OnSuccess?.Invoke();
		Log.Info( $"[MrMixTypingChallenge] Success! {WordsCompleted}/{WordsToWin}" );
	}

	private void Fail()
	{
		_state = ChallengeState.Lost;
		_music?.Stop();
		PlayLossSound();
		OnFail?.Invoke();
		Log.Info( $"[MrMixTypingChallenge] Fail! {WordsCompleted}/{WordsToWin}" );
	}

	private void UpdateWinFlow()
	{
		if ( _sceneSwitchTriggered )
			return;

		_winAutoNextLeft = MathF.Max( 0f, _winAutoNextLeft - Time.Delta );
		if ( _winAutoNextLeft > 0f )
			return;

		_sceneSwitchTriggered = true;
		LoadSceneIfSet( NextLevelScenePath );
	}

	private void UpdateLoseFlow()
	{
		if ( Input.Keyboard.Pressed( "enter" ) )
		{
			LoadSceneIfSet( RetryScenePath );
			return;
		}

		if ( Input.Keyboard.Pressed( "escape" ) )
		{
			LoadSceneIfSet( MainMenuScenePath );
		}
	}

	private void LoadSceneIfSet( string scenePath )
	{
		if ( string.IsNullOrWhiteSpace( scenePath ) )
		{
			Log.Warning( "[MrMixTypingChallenge] Scene path is empty." );
			return;
		}

		Game.ActiveScene.LoadFromFile( scenePath );
	}

	private sealed class TypingOverlayPanel : Panel
	{
		private readonly MrMixTypingChallengeHud _src;

		private readonly Label _typed;
		private readonly Label _remaining;
		private readonly Label _timer;
		private readonly Label _progress;
		private readonly Label _level;
		private readonly Label _result;
		private readonly Label _hint;

		public TypingOverlayPanel( MrMixTypingChallengeHud src )
		{
			_src = src;

			AddClass( "overlay" );

			var title = new Label
			{
				Parent = this,
				Text = "Type this!"
			};
			title.AddClass( "title" );

			var wordRow = new Panel
			{
				Parent = this
			};
			wordRow.AddClass( "word-row" );

			_typed = new Label { Parent = wordRow, Text = "" };
			_typed.AddClass( "typed" );

			_remaining = new Label { Parent = wordRow, Text = "" };
			_remaining.AddClass( "remaining" );

			_timer = new Label { Parent = this, Text = "" };
			_timer.AddClass( "timer" );

			_progress = new Label { Parent = this, Text = "" };
			_progress.AddClass( "progress" );

			_level = new Label { Parent = this, Text = "" };
			_level.AddClass( "level" );

			_result = new Label { Parent = this, Text = "" };
			_result.AddClass( "result" );

			_hint = new Label { Parent = this, Text = "" };
			_hint.AddClass( "hint" );
		}

		public void Refresh()
		{
			_level.Text = _src.LevelLabel;

			if ( _src.IsWon )
			{
				int nextIn = (int)MathF.Ceiling( _src.WinAutoNextLeft );

				_typed.Text = "";
				_remaining.Text = "";
				_timer.Text = "";
				_progress.Text = "";

				_result.Text = "Great Job!!!";
				return;
			}

			if ( _src.IsLost )
			{
				_typed.Text = "";
				_remaining.Text = "";
				_timer.Text = "";
				_progress.Text = "";

				_result.Text = "You Failed";
				_hint.Text = "Enter - Repeat   |   Esc - Main Menu";
				return;
			}

			_result.Text = "";
			_hint.Text = "";

			string w = _src.CurrentWord ?? "";

			int typed = Math.Clamp( _src.TypedCount, 0, w.Length );
			_typed.Text = w.Substring( 0, typed );
			_remaining.Text = w.Substring( typed );

			int seconds = (int)MathF.Floor( _src.TimeLeft );
			_timer.Text = $"{Math.Max( 0, seconds )}";

			_progress.Text = $"{_src.WordsCompleted}/{_src.WordsToWin}";
		}
	}
}