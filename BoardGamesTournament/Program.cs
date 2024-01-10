using BoardGamesTournament.Classes;
using BoardGamesTournament.UI;

namespace BoardGamesTournament
{
	internal class Program
	{
		static void Main(string[] args)
		{
			bool start = true;
			while (true)
			{
				try
				{
					if (start)
					{
						start = false;
						const int COLLECTION_ID = 3849;
						Commands.Instance.TryCallCommand($"load {COLLECTION_ID}");
						Commands.Instance.TryCallCommand("games");
						Commands.Instance.PrintHelp();
					}

					Commands.Instance.TryCallCommand(Commands.ReadLine());
				}
				catch (CancelException) { Commands.WriteError("Прервано выполнение предыдущей команды\n"); }
				catch (ExitException) { return; }
				catch (Exception e) { Commands.WriteError($"{e.Message}\n"); }
			}
		}
	}
}