using BoardGamesTournament.Classes;

namespace BoardGamesTournament.UI
{
	internal class Command
	{
		readonly Dictionary<byte, Action<string[]>> _actions = new();

		public readonly string Description;
		public Command(string description)
		{
			this.Description = description;
		}
		public void AddAction(byte argsCount, Action<string[]> action) { _actions.Add(argsCount, action); }
		public bool ContainsAction(byte argsCount) { return _actions.ContainsKey(argsCount); }
		public void InvokeAction(string[] args)
		{
			if (!_actions.TryGetValue((byte)args.Length, out Action<string[]>? action))
				throw new ArgumentException($"Невозможно вызвать действие, т.к. используется некорректное количество параметров ({args.Length})");
			action.Invoke(args);
		}
		public int ActionsCount { get { return _actions.Count; } }
	}
	internal class Commands
	{
		static bool TryParseTournamentType(string str, out Tournament.Type tournamentType)
		{
			bool result = Enum.TryParse(str, out tournamentType);
			if (!result)
				Console.WriteLine("\"{0}\" не является значением типа {1}", str, typeof(Tournament.Type).FullName);
			return result;
		}
		static Tournament? NewTournament(string tournamentName, Tournament.Type tournamentType)
		{
			List<string> names = new List<string>();
			Console.WriteLine("Построчно введите имена участников турнира. Для завершения ввода используйте команду end");
			while (true)
			{
				string playerName = Commands.ReadLine((string line) =>
				{
					bool result = !names.Contains(line);
					if (!result)
						Console.WriteLine($"\"{line}\" уже содержится в списке участников турнира");
					return result;
				});

				if (playerName.ToLower() == "end")
					break;
				else
					names.Add(playerName);
			}

			if (names.Count < 2)
				throw new Exception($"Невозможно создать турнир {tournamentName}, т.к. заявленное количество участников ({names.Count}) меньше 2");

			Console.WriteLine($"Кол-во участников: {names.Count}. Введите номера настольных игр, участвующий в турнире \"{tournamentName}\", через пробел:");
			List<int> indexes = new();
			Commands.ReadLine((string line) =>
			{
				string[] strArray = line.Split(" ");
				foreach (string str in strArray)
					if (!Int32.TryParse(str, out int number) || number < 1 || number > AllBoardgames.Instance.Count)
					{
						Console.WriteLine($"Значение \"{str}\" не является корректным номером настолькой игры из списка");
						indexes.Clear();
						return false;
					}
					else if (!indexes.Contains(--number)) // возможно, стоит убрать эту проверку, т.к., теоритически, в одном турнире может быть одновременно несколько одинаковых игр
						indexes.Add(number);

				return true;
			});

			Type? type = Type.GetType($"{typeof(Tournament).Namespace}.{tournamentType}Tournament", true);
			Tournament? tournament = (Tournament?)type?.GetConstructor(Array.Empty<Type>())?.Invoke(null);
			if (type is null || tournament is null)
				return null;

			foreach (string name in names)
				tournament.AddPlayer(new Player(name));
			foreach (int index in indexes)
				tournament.AddBoardgame(AllBoardgames.Instance[index]);
			return tournament;
		}
		static Tournament? NewTournament(Predicate<string> validateTournamentName, out string tournamentName)
		{
			Console.WriteLine("Введите уникальное имя турнира:");
			tournamentName = Commands.ReadLine(validateTournamentName);

			Tournament.Type tournamentType = default;
			Console.WriteLine($"Введите тип турнира \"{tournamentName}\":");
			Commands.ReadLine((string line) => Commands.TryParseTournamentType(line, out tournamentType));

			return NewTournament(tournamentName, tournamentType);
		}

		static readonly Lazy<Commands> _instance = new Lazy<Commands>(() => new Commands());
		readonly Dictionary<string, Tournament> _tournaments = new();
		readonly Dictionary<string, Command> _commands = new();
		Commands()
		{
			_commands.Add("games", new Command("список настольных игр"));
			_commands["games"].AddAction(0, (string[] args) =>
			{
				Console.WriteLine($"Настольные игры{(AllBoardgames.Instance.Count == 0 ? "не загружены" : ':')}");
				for (var i = 0; i < AllBoardgames.Instance.Count; i++)
					Console.WriteLine("  {0}. {1}", i + 1, AllBoardgames.Instance[i]);

				Console.WriteLine();
			});

			bool ValidateTournamentName(string line)
			{
				bool result = !_tournaments.ContainsKey(line);
				if (!result)
					Commands.WriteError($"Имя турнира \"{line}\" уже используется ({_tournaments[line]})\n");
				return result;
			};
			void TryAddTournament(Tournament? tournament, string tournamentName)
			{
				if (tournament is not null)
				{
					_tournaments.Add(tournamentName, tournament);
					Console.WriteLine($"Добавлен турнир \"{tournamentName}\" ({tournament})\n");
				}
			}
			bool TryGetTournament(string tournamentName, out Tournament? tournament)
			{
				bool result = _tournaments.TryGetValue(tournamentName, out tournament);
				if (!result)
					Commands.WriteError($"Не удалось найти турнир с именем \"{tournamentName}\"\n");
				return result;
			}
			static bool TryParseByte(string str, out byte value, string ErrorFormat = "")
			{
				bool result = byte.TryParse(str, out value);
				if (!result && !string.IsNullOrEmpty(ErrorFormat))
					Commands.WriteError(string.Format(ErrorFormat, str));
				return result;
			}
			_commands.Add("list", new Command("список всех турниров"));
			_commands["list"].AddAction(0, (string[] args) =>
			{
				if (_tournaments.Count == 0)
				{
					Console.WriteLine("Ещё не было создано ни одного турнира");
					return;
				}
				foreach (var item in _tournaments.Keys)
					Console.WriteLine($"  \"{item}\" ({_tournaments[item]})");

				Console.WriteLine();
			});

			_commands.Add("add", new Command("добавить новый турнир"));
			_commands["add"].AddAction(0, (string[] args) => TryAddTournament(Commands.NewTournament(ValidateTournamentName, out string tournamentName), tournamentName) );
			_commands["add"].AddAction(2, (string[] args) =>
			{
				if (ValidateTournamentName(args[0]) && TryParseTournamentType(args[1], out Tournament.Type tournamentType))
					TryAddTournament(NewTournament(args[0], tournamentType), args[0]);
			});

			_commands.Add("remove", new Command("удалить турнир"));
			_commands["remove"].AddAction(1, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament))
				{
					_tournaments.Remove(args[0]);
					_commands["list"].InvokeAction(Array.Empty<string>());
				}
			});

			const string MaxPlayersCountErrorFormat = "Значение \"{0}\" не является корректным максимальным количеством игроков\n";
			_commands.Add("start", new Command("запустить созданный турнир"));
			_commands["start"].AddAction(1, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament))
				{
					tournament?.Start();
					Console.WriteLine(tournament?.GetMultiLineInfo());
				}
			});
			_commands["start"].AddAction(2, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament) && TryParseByte(args[1], out byte maxPlayersCount, MaxPlayersCountErrorFormat))
				{
					tournament?.Start(maxPlayersCount);
					Console.WriteLine(tournament?.GetMultiLineInfo());
				}
			});

			_commands.Add("restart", new Command("перезапустить созданный турнир"));
			_commands["restart"].AddAction(1, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament))
				{
					tournament?.Restart();
					Console.WriteLine(tournament?.GetMultiLineInfo());
				}

			});
			_commands["restart"].AddAction(2, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament) && TryParseByte(args[1], out byte maxPlayersCount, MaxPlayersCountErrorFormat))
				{
					tournament?.Restart(maxPlayersCount);
					Console.WriteLine(tournament?.GetMultiLineInfo());
				}
			});

			const string ErrorFormatRoundNumber = "Значение \"{0}\" не является корректным номером раунда";
			_commands.Add("info", new Command("получить информацию о турнире или его раунде"));
			_commands["info"].AddAction(1, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament))
				{
					Console.WriteLine(tournament);
					Console.WriteLine(tournament?.GetMultiLineInfo());
				}
			});
			_commands["info"].AddAction(2, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament) && TryParseByte(args[1], out byte roundNumber, ErrorFormatRoundNumber))

					Console.WriteLine(tournament?.GetMultiLineInfo(roundNumber));
			});

			_commands.Add("end", new Command("завершить один раунд турнира"));
			static void CompleteRound(Tournament tournament, byte roundNumber)
			{
				tournament?.CompleteRound(roundNumber, (IDictionary<Player, byte> points) =>
				{
					Console.WriteLine($"Введите количество ПО за раунд #{roundNumber} для каждого игрока:");
					foreach (var item in points)
					{
						string write = $"  {item.Key.Name} = ";
						Console.Write(write);
						byte value = byte.MinValue;
						Commands.ReadLine((string line) => TryParseByte(line, out value, $"Значение \"{line}\" не является корректным количеством ПО"), write);
						points[item.Key] = value;
					}
				});
			}
			_commands["end"].AddAction(2, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament) && TryParseByte(args[1], out byte roundNumber, ErrorFormatRoundNumber) && tournament is not null)
				{
					CompleteRound(tournament, roundNumber);
					Console.WriteLine();
				}
			});
			_commands["end"].AddAction(1, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament) && tournament is not null)
				{
					byte roundNumber = default;
					bool isOnlyOneNotCompleted = false;
					for (int i = 0; i < tournament.Rounds.Count; i++)
						if (!tournament.Rounds[i].Completed)
						{
							isOnlyOneNotCompleted = !isOnlyOneNotCompleted;
							if (isOnlyOneNotCompleted)
								roundNumber = (byte)(i + 1);
							else
								break;

						}

					if (!isOnlyOneNotCompleted)
					{
						Console.WriteLine("Укажите номер раунда турнира, который вы хотите завершить:");
						Commands.ReadLine((string value) => TryParseByte(value, out roundNumber));
					}
					CompleteRound(tournament, roundNumber);
					Console.WriteLine();
				}
			});

			_commands.Add("next", new Command("создать следующие раунды"));
			_commands["next"].AddAction(1, (string[] args) => {
				if (TryGetTournament(args[0], out Tournament? tournament)) {
					tournament?.StartNext();
					Console.WriteLine(tournament?.GetMultiLineInfo());
				}
			});
			_commands["next"].AddAction(2, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament) && TryParseByte(args[1], out byte maxPlayersCount, MaxPlayersCountErrorFormat))
				{
					tournament?.StartNext(maxPlayersCount);
					Console.WriteLine(tournament?.GetMultiLineInfo());
				}
			});

			_commands.Add("score", new Command("подвести итоги турнира"));
			_commands["score"].AddAction(1, (string[] args) =>
			{
				if (TryGetTournament(args[0], out Tournament? tournament) && tournament is not null)
				{
					IEnumerable<KeyValuePair<Player, byte>> points = tournament.GetScore();
					byte prevValue = byte.MinValue;
					int number = 0;
					foreach (var item in points)
					{
						number = item.Value == prevValue ? number : ++number;
						Console.WriteLine($"{number}) {item.Key.Name} - {item.Value}");
						prevValue = item.Value;
					}
					Console.WriteLine();
				}
			});
		}

		public static Commands Instance { get { return _instance.Value; } }
		public void PrintHelp()
		{
			Console.WriteLine("Команды:");
			Console.WriteLine("  help - отобразить справку");
			Console.WriteLine("  cancel - прервать предыдущую команду");
			Console.WriteLine("  exit - закрыть программу");
			foreach (var item in _commands)
				if (item.Value.ActionsCount > 0)
					Console.WriteLine(string.Format($"  {item.Key} - {item.Value.Description}"));
			Console.WriteLine();

			Console.WriteLine("Типы турниров:");
			foreach (Tournament.Type tournamentType in Enum.GetValues(typeof(Tournament.Type)))
			{
				switch (tournamentType)
				{
					case Tournament.Type.Duel:
						Console.WriteLine("  {0:D} ({0}) - дуэльный турнир", tournamentType);
						break;

					case Tournament.Type.Together:
						Console.WriteLine("  {0:D} ({0}) - одновременная игра в нескольких группах", tournamentType);
						break;

					case Tournament.Type.Elimination:
						Console.WriteLine("  {0:D} ({0}) - турнир с выбыванием", tournamentType);
						break;

					default:
						Console.WriteLine("  {0:D} ({0}) - Неизвестное значение типа {1}", tournamentType, typeof(Tournament.Type).Name);
						break;
				}
			}

			Console.WriteLine();
		}
		public static void WriteError(string message) { Console.Error.WriteLine($"(!) {message}"); }
		public static string ReadLine(Predicate<string>? predicate = null, string write = "")
		{
			while (true)
			{
				string? line = Console.ReadLine()?.Trim();
				if (String.IsNullOrEmpty(line))
					continue;
				switch (line.ToLower())
				{
					case "help":
						Commands.Instance.PrintHelp();
						break;
					case "cancel":
						throw new CancelException();
					case "exit":
						throw new ExitException();
					default:
						if (predicate is null || predicate(line))
							return line;
						else if (predicate is not null && !string.IsNullOrEmpty(write))
							Console.Write(write);
						break;
				}
			}

		}
		public bool TryCallCommand(string line)
		{
			string[] strArray = line.Split(' ');
			bool result = strArray.Length > 0 && _commands.TryGetValue(strArray[0].ToLower(), out Command? command) && command.ContainsAction((byte)(strArray.Length - 1));
			if (result)
			{
				string commandName = strArray[0].ToLower();
				Array.Copy(strArray, 1, strArray, 0, strArray.Length - 1);
				Array.Resize<string>(ref strArray, strArray.Length - 1);
				_commands[commandName].InvokeAction(strArray);
			}
			return result;			
		}
	}
}
