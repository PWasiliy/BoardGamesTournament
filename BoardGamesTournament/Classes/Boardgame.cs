using Tesera.Models;

namespace BoardGamesTournament.Classes
{
	internal class Boardgame
	{
		static void SetPlayersCount(ref byte playersCount, byte value)
		{
			if (value <= 1)
				throw new ArgumentException($"Значение свойства \"Ков-во игроков\" не может быть меньше или равно 1");
			playersCount = value;
		}
		byte _minPlayersCount = 2;
		byte _maxPlayersCount = 2;
		readonly byte? _playersCount;
		
		public readonly string Name;
		public byte MinPlayersCount { get { return _minPlayersCount; } set { Boardgame.SetPlayersCount(ref _minPlayersCount, value); } }
		public byte MaxPlayersCount { get { return _maxPlayersCount; } set { Boardgame.SetPlayersCount(ref _maxPlayersCount, value); } }
		public Boardgame(string name, byte? playersCount = null) { this.Name = name; _playersCount = playersCount; }
		public override string ToString() { return string.Format($"\"{this.Name}\" ({this.MinPlayersCount}-{this.MaxPlayersCount})"); }
		public byte PlayersCount {  
			get {
				if (_playersCount is not null)
					return (byte)_playersCount;
				double value = (_maxPlayersCount - _minPlayersCount) / 2;
				double result = Math.Ceiling(value) + _minPlayersCount;
				return (byte)result;
			}
		}
	}
	internal class AllBoardgames
	{
		readonly List<Boardgame> _boardgames = [];
		static readonly Lazy<AllBoardgames> _lazyInstance = new(() => new AllBoardgames());
		
		public static AllBoardgames Instance { get { return _lazyInstance.Value; } }
		public int Load(int collectionId)
		{
			using HttpClient httpClient = new();
			Tesera.TeseraClient teseraClient = new(httpClient);
			
			var collectionGames = teseraClient.Get<IEnumerable<CustomCollectionGameInfo>>(new Tesera.API.Collections.Custom.GamesClear(collectionId, Tesera.Types.Enums.GamesType.SelfGame))
				?? throw new NullReferenceException($"Не удалось получить список игр из коллекции с ID {collectionId}");

			int prevCount = _boardgames.Count;
			foreach (var item in collectionGames)
			{
				string gameName = string.IsNullOrEmpty(item.Game.Title) ? $"c ID {item.Game.Id}" : item.Game.Title;
				if (string.IsNullOrEmpty(item.Game.Alias))
					throw new NullReferenceException($"У игры {gameName} отсутствует алиас");
				var gameInfoResponse = teseraClient.Get<GameInfoResponse>(new Tesera.API.Games(item.Game.Alias)) ?? throw new NullReferenceException($"Не удалось получить информацию об игре {gameName}");
				byte? playersCount = string.IsNullOrEmpty(item.Comment) || !byte.TryParse(item.Comment, out byte parsedComment) ? null : parsedComment;

				GameInfo game = gameInfoResponse.Game;
				_boardgames.Add(new Boardgame(game.Title ?? throw new NullReferenceException($"У игры {gameName} отсутствует название"), playersCount)
				{
					MinPlayersCount = (byte)(game.PlayersMinRecommend <= 1 ? game.PlayersMin == 1 ? 2 : game.PlayersMin : game.PlayersMinRecommend),
					MaxPlayersCount = (byte)(game.PlayersMaxRecommend <= 1 ? game.PlayersMax : game.PlayersMaxRecommend)
				});
			}
			return _boardgames.Count - prevCount;

		}
		public int Count { get { return _boardgames.Count; } }
		public Boardgame this[int index] { get { return _boardgames[index]; } }
	}
}
