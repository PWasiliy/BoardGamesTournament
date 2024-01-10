using System.Text;

namespace BoardGamesTournament.Classes
{
	internal interface IRound
	{
		public bool Completed { get; }
	}
	internal class Round : IRound
	{
		readonly Dictionary<Player, byte> _players = new();
		bool _completed;

		public readonly Boardgame Boardgame;
		public bool Completed { get { return _completed; } private set {
				if (_completed && !value)
					throw new ArgumentException($"Невозможно завершить раунд в игру \"{Boardgame}\", т.к. он уже был завершён");
				_completed = value;
		} }
		public Round(Boardgame boardgame, IEnumerable<Player> players)
		{
			Boardgame = boardgame;
			foreach (Player player in players)
				_players.Add(player, 0);
		}
		public byte GetPointsByPlayer(Player player) { return this.HasPlayer(player) ? _players[player] : byte.MinValue; }
		public string GetMultiLineInfo() { 
			StringBuilder stringBuilder = new();
			stringBuilder.Append(this.Completed ? $"{Boardgame}:\n" : $"{Boardgame}:");
			foreach (var item in _players)
				if (this.Completed)
					stringBuilder.AppendLine($"  {item.Key.Name} - {item.Value}");
				else
					stringBuilder.Append(string.Format(item.Key == _players.Last().Key ? " {0}\n" : " {0},", item.Key.Name));

			return stringBuilder.ToString();
		}
		public bool HasPlayer(Player player) { return _players.ContainsKey(player); }
		public void Complete(IDictionary<Player, byte> points)
		{
			foreach (var player in _players.Keys)
				if (!points.ContainsKey(player))
					throw new ArgumentException($"Невозможно завершить раунд по игре \"{Boardgame}\", т.к. отсутствует количество ПО для игрока \"{player.Name}\"");

			this.Completed = true;
			foreach (var player in _players.Keys)
				_players[player] = points[player];
		}
	}
}
