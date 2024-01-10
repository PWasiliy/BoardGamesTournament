using BoardGamesTournament.Classes;
using BoardGamesTournament.UI;

namespace BoardGamesTournament
{
	internal class Program
	{
		static void Main(string[] args)
		{
			const int COLLECTION_ID = 3849;
			Console.WriteLine($"Загружается список игр из коллекции с ID {COLLECTION_ID}...");
			try
			{
				AllBoardgames.Instance.Load(COLLECTION_ID);
			}
			catch (Exception e) { Commands.WriteError(e.Message); }

			Console.WriteLine();
			Commands.Instance.PrintHelp();
			while (true)
			{
				try
				{
					Commands.Instance.TryCallCommand(Commands.ReadLine());
				}
				catch (CancelException) { Commands.WriteError("Прервано выполнение предыдущей команды\n"); }
				catch (ExitException) { return; }
				catch (Exception e) { Commands.WriteError($"{e.Message}\n"); }
			}
		}
	}
}