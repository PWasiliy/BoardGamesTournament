namespace BoardGamesTournament.Classes
{
	internal abstract class Tournament
	{
		static void Add<T>(T item, IList<T> list)
		{
			if (list.Contains(item))
				throw new ArgumentException($"Невозможно повторно добавить элемент \"{item}\" к списку");
			list.Add(item);
		}
		static readonly Lazy<Random> _random = new Lazy<Random>(() => new Random());
		static IEnumerable<Round> NewRounds(byte maxPlayersCount, IList<Boardgame> boardgames, IList<Player> players)
		{
			static T? GetRandom<T>(IList<T> from, Predicate<T>? predicate = null) where T : class
			{
				List<T>? excluded = predicate is null ? null : new();
				while (excluded is null || excluded.Count < from.Count)
				{
					T item = from[_random.Value.Next(from.Count)];
					if (excluded is not null && excluded.Contains(item))
						continue;
					else if (predicate is not null && !predicate(item))
						excluded?.Add(item);
					else
						return item;
				}
				return null;

			}

			Dictionary<Boardgame, HashSet<Player>> rounds = new();
			HashSet<Player> freePlayers = new(players.Count);
			void UpdateFreePlayers(HashSet<Player> freePlayers)
			{
				freePlayers.Clear();
				if (rounds.Count == 0)
					freePlayers.UnionWith(players);
				else
				{
					foreach (var player in players)
						if (!rounds.Values.Any((HashSet<Player> players) => players.Contains(player)))
							freePlayers.Add(player);
				}
			}

			UpdateFreePlayers(freePlayers);
			Label:
			int maxDelta = int.MinValue;
			foreach (Boardgame item in boardgames)
				maxDelta = rounds.ContainsKey(item) ? maxDelta : Math.Max(maxDelta, Math.Max(item.PlayersCount - item.MinPlayersCount, item.MaxPlayersCount - item.PlayersCount));

			int playersCount = 0;
			Boardgame? boardgame = null;
			for (int delta = 0; boardgame is null && delta <= maxDelta; delta++)
			{
				boardgame = GetRandom<Boardgame>(boardgames, (Boardgame boardgame) => {
					playersCount = Math.Max(boardgame.MinPlayersCount, boardgame.PlayersCount - delta);
					int[] countArray = new int[2] { playersCount, Math.Min(boardgame.MaxPlayersCount, boardgame.PlayersCount + delta) };
					return !rounds.ContainsKey(boardgame) && freePlayers.Count >= countArray.Min() && (maxPlayersCount == byte.MinValue || (countArray.Contains<int>(maxPlayersCount)));
				});
			}

			if (boardgame is null)
			{
				if (rounds.Count > 0)
				{
					foreach (var item in rounds)
						if (item.Value.Count < item.Key.MaxPlayersCount && (maxPlayersCount == byte.MinValue || item.Value.Count < maxPlayersCount) && (boardgame is null
							|| Math.Max(item.Key.PlayersCount, item.Value.Count) - Math.Min(item.Key.PlayersCount, item.Value.Count)
								< Math.Max(boardgame.PlayersCount, rounds[boardgame].Count) - Math.Min(boardgame.PlayersCount, rounds[boardgame].Count)))
						{
							boardgame = item.Key;
						}
				}
				if (boardgame is null)
					throw new InvalidOperationException("Невозможно начать турнир, т.к. не удалось подобрать настольную игру");
			}

			playersCount = rounds.ContainsKey(boardgame) ? 1 : playersCount;
			if (!rounds.ContainsKey(boardgame))
				rounds.Add(boardgame, new HashSet<Player>());

			for (int i = 0; i < playersCount; i++)
			{
				Player? player = GetRandom<Player>(freePlayers.ToList(), (Player player) => !rounds[boardgame].Contains(player))
					?? throw new InvalidOperationException($"Невозможно начать турнир, т.к. не удалось подобрать игроков в {boardgame}");
				rounds[boardgame].Add(player);
			}

			UpdateFreePlayers(freePlayers);
			if (freePlayers.Count > 0)
				goto Label;

			Round[] result = new Round[rounds.Count];
			int index = 0;
			foreach (var item in rounds)
				result[index++] = new Round(item.Key, item.Value);
			return result;
		}
		IEnumerable<Round> NewRounds(byte maxPlayersCount, bool started, string description)
		{
			List<Boardgame> boardgames = new();
			if (_rounds.Count > 0 && !started)
				throw new InvalidOperationException($"Невозможно {description}, т.к. он уже был запущен");
			else if (_rounds.Count == 0 && started)
				throw new InvalidOperationException($"Невозможно {description}, т.к. он ещё не был запущен");
			else if (maxPlayersCount != byte.MinValue && maxPlayersCount == 1)
				throw new ArgumentException($"Невозможно {description}, т.к. указанное максимальное количество игроков ({maxPlayersCount}) для одной любой игры некорректно");

			bool ignorePlayed = true;
			Label:
			boardgames.AddRange(_boardgames.Where((Boardgame boardgame) => (maxPlayersCount == byte.MinValue || (boardgame.MinPlayersCount <= maxPlayersCount && maxPlayersCount <= boardgame.MaxPlayersCount))
				&& (_rounds.Count == 0 || !_rounds.Any((Round round) => round.Boardgame == boardgame && (!round.Completed || ignorePlayed)))));

			if (boardgames.Count == 0)
				if (ignorePlayed)
				{
					ignorePlayed = false;
					goto Label;
				}
				else if (_rounds.Count == 0)
					throw new InvalidOperationException($"Невозможно {description}, т.к. ни одна из настольных игр не соответствует параметрам турнира");
				else
					throw new InvalidOperationException($"Невозможно {description}, т.к. нет свободных настольных игр или ни одна из них не соответствует параметрам турнира");

			if (!started)
			{
				byte playersCount = 0;
				boardgames.ForEach((Boardgame boardgame) => playersCount += boardgame.MaxPlayersCount);
				if (playersCount < _players.Count)
					throw new InvalidOperationException($"Невозможно {description}, т.к. количество участников ({_players.Count}) превышает максимальное количество одновременных игроков ({playersCount})");
			}

			IList<Player> players = this.GetNextPlayers();
			if (players.Count == 0)
				throw new InvalidOperationException(started ? $"Невозможно {description}, т.к. отсутствуют свободные участники" : $"Невозможно {description}, т.к. в турнир не добавлены участники");

			return Tournament.NewRounds(maxPlayersCount, boardgames, players);
		}

		private protected readonly List<Player> _players = new();
		private protected readonly List<Boardgame> _boardgames = new();
		private protected readonly List<Round> _rounds = new();
		private protected virtual IList<Player> GetNextPlayers()
		{
			List<Player> result = new();
			result.AddRange(_rounds.Count == 0 ? _players : _players.Where((Player player) => !_rounds.Any((Round round) => !round.Completed && round.HasPlayer(player))));
			return result;
		}

		public enum Type { Elimination = 1, Duel, Together }
		public void AddPlayer(Player player) { Tournament.Add<Player>(player, _players); }
		public void AddBoardgame(Boardgame boardgame) { Tournament.Add<Boardgame>(boardgame, _boardgames); }
		public override string ToString() { return string.Format($"Кол-во участников: {_players.Count}; Кол-во игр: {_boardgames.Count}"); }
		public virtual void Start(byte maxPlayersCount = byte.MinValue)
		{
			_rounds.AddRange(this.NewRounds(maxPlayersCount, false, "начать турнир"));
		}
		public void Restart(byte maxPlayersCount = byte.MinValue)
		{
			if (_rounds.Count == 0)
				throw new InvalidOperationException("Невозможно перезапустить нестартовавший турнир");
			_rounds.Clear();
			this.Start(maxPlayersCount);
		}
		public string GetMultiLineInfo(byte roundNumber = byte.MinValue)
		{
			if (_rounds.Count == 0)
				return "Невозможно получить информацию о раундах турнира, т.к. он ещё не был запущен";
			else if (roundNumber != byte.MinValue)
			{
				if (0 <= roundNumber - 1 && roundNumber - 1 < _rounds.Count)
					return _rounds[roundNumber - 1].GetMultiLineInfo();
				else
					throw new ArgumentException($"Значение \"{roundNumber}\" не является корректным номером раунда");
			}

			System.Text.StringBuilder stringBuilder = new();
			for (var i = 0; i < _rounds.Count; i++)
				stringBuilder.Append(string.Format($"  #{i + 1} {_rounds[i].GetMultiLineInfo()}"));
			return stringBuilder.ToString();
		}
		public void CompleteRound(int roundNumber, Action<IDictionary<Player, byte>> action)
		{
			if (_rounds.Count == 0)
				throw new InvalidOperationException($"Невозможно завершить раунд #{roundNumber}, т.к. турнир ещё не стартовал");
			if (roundNumber <= 0 || roundNumber > _rounds.Count)
				throw new ArgumentException($"Невозможно завершить раунд #{roundNumber}, т.к. он ещё не был создан");

			Dictionary<Player, byte> points = new();
			_players.ForEach((Player player) => { if (_rounds[roundNumber - 1].HasPlayer(player)) points.Add(player, byte.MinValue); });
			action(points);
			_rounds[roundNumber - 1].Complete(points);
		}
		public virtual byte[] StartNext(byte maxPlayersCount = byte.MinValue)
		{
			byte[] result = new byte[1] { (byte)(_rounds.Count + 1) };
			_rounds.AddRange(this.NewRounds(maxPlayersCount, true, "продолжить турнир"));
			Array.Resize<byte>(ref result, _rounds.Count + 1 - result[0]);
			for(int i = 1; i < result.Length; i++)
				result[i] = result[i - 1];
			return result;
		}
		public IEnumerable<KeyValuePair<Player, byte>> GetScore()
		{
			if (_rounds.Count == 0)
				throw new InvalidOperationException("Невозможно подвести итоги турнира, т.к. он ещё не был запущен");
			Dictionary<Player, byte> points = new();
			_players.ForEach((Player player) => points.Add(player, byte.MinValue));
			_rounds.ForEach((Round round) => _players.ForEach((Player player) => points[player] = (byte)(points[player] + round.GetPointsByPlayer(player))));
			var result = points.ToList();
			result.Sort((x, y) => -x.Value.CompareTo(y.Value));
			return result;
		}
		public IReadOnlyList<IRound> Rounds { get { return _rounds; }  }
	}
	internal class EliminationTournament : Tournament
	{
		private protected override IList<Player> GetNextPlayers()
		{
			IList<Player> result = base.GetNextPlayers();
			if (_rounds.Count == 0)
				return result;
			List<KeyValuePair<Player, byte>> points = new(this.GetScore());
			for (int i = 0; i < _rounds.Count; i++)
				result.Remove(points[points.Count - 1 - i].Key);
			return result;
			
		}
		public override void Start(byte maxPlayersCount = byte.MinValue)
		{
			if (maxPlayersCount != byte.MinValue && maxPlayersCount != _players.Count)
				throw new ArgumentException($"Невозможно начать турнир, т.к. максимальное количество игроков ({maxPlayersCount}) для одной любой игры не соответствует количеству участников ({_players.Count})");
			base.Start((byte)_players.Count);
		}
		public override byte[] StartNext(byte maxPlayersCount = byte.MinValue)
		{
			return base.StartNext((byte)(_players.Count - _rounds.Count));
		}
	}
	internal class TogetherTournament : Tournament
	{

	}
	internal class DuelTournament : Tournament
	{
		public override void Start(byte maxPlayersCount = byte.MinValue)
		{
			if (maxPlayersCount != byte.MinValue && maxPlayersCount != 2)
				throw new ArgumentException($"Невозможно начать дуэльный турнир, т.к. указано неверное максимальное число участников для одной игры ({maxPlayersCount})");
			base.Start(2);
		}
	}
}
