using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NSUci;
using RapLog;

namespace NSProgram
{
	class Program
	{
		/// <summary>
		/// Book can write log file.
		/// </summary>
		public static bool isLog = false;
		public static int added = 0;
		public static int updated = 0;
		public static int deleted = 0;
		/// <summary>
		/// Moves added to book per game.
		/// </summary>
		public static int bookAdd = 7;

		public static CBook book = new CBook();

		static void Main(string[] args)
		{
			bool bookChanged = false;
			bool bookUpdate = false;
			bool bookWrite = false;
			bool quit = false;
			/// <summary>
			/// Can start teacher.
			/// </summary>
			bool analyze = false;
			/// <summary>
			/// Book can update moves.
			/// </summary>
			bool isW = false;
			/// <summary>
			/// Limit ply to wrtie.
			/// </summary>
			int bookLimitW = 32;
			/// <summary>
			/// Limit ply to read.
			/// </summary>
			int bookLimitR = 0xf;
			/// <summary>
			/// Number of moves not found in a row.
			/// </summary>
			int emptyRow = 0;
			/// <summary>
			/// Random moves factor.
			/// </summary>
			int bookRandom = 60;
			int lastLength = 0;
			string analyzeMoves = String.Empty;
			string lastFen = String.Empty;
			string lastMoves = String.Empty;
			CUci Uci = new CUci();
			CRapLog logUci = new CRapLog("teacher.uci", 0, false);
			object locker = new object();
			string ax = "-bn";
			List<string> listBn = new List<string>();
			List<string> listEf = new List<string>();
			List<string> listEa = new List<string>();
			List<string> listTf = new List<string>();
			for (int n = 0; n < args.Length; n++)
			{
				string ac = args[n];
				switch (ac)
				{
					case "-add"://moves add to book
					case "-bn"://book name
					case "-ef"://engine file
					case "-ea"://engine arguments
					case "-rnd"://random moves
					case "-lr"://limit read in half moves
					case "-lw"://limit write in half moves
					case "-tf"://teacher file
						ax = ac;
						break;
					case "-log"://add log
						ax = ac;
						isLog = true;
						break;
					case "-w"://writable
						ax = ac;
						isW = true;
						break;
					default:
						switch (ax)
						{
							case "-bn":
								listBn.Add(ac);
								break;
							case "-ef":
								listEf.Add(ac);
								break;
							case "-tf":
								listTf.Add(ac);
								break;
							case "-ea":
								listEa.Add(ac);
								break;
							case "-w":
								ac = ac.Replace("K", "000").Replace("M", "000000");
								book.maxRecords = int.TryParse(ac, out int m) ? m : 0;
								break;
							case "-add":
								bookAdd = int.TryParse(ac, out int a) ? a : bookAdd;
								break;
							case "-rnd":
								bookRandom = int.TryParse(ac, out int r) ? r : 0;
								break;
							case "-lr":
								bookLimitR = int.TryParse(ac, out int lr) ? lr : 0;
								break;
							case "-lw":
								bookLimitW = int.TryParse(ac, out int lw) ? lw : 0;
								break;
						}
						break;
				}
			}
			string bookName = String.Join(" ", listBn);
			string engineFile = String.Join(" ", listEf);
			string teacherFile = String.Join(" ", listTf);
			string arguments = String.Join(" ", listEa);
			string ext = Path.GetExtension(bookName);
			Console.WriteLine($"info string book {CBook.name} ver {CBook.version}");
			if (String.IsNullOrEmpty(ext))
				bookName = $"{bookName}{CBook.defExt}";
			bool bookLoaded = book.LoadFromFile(bookName);
			if (bookLoaded && (book.recList.Count > 0))
			{
				Console.WriteLine($"info string book on");
				if (isW)
					Console.WriteLine($"info string write on");
			}


			Process engineProcess = null;
			if (File.Exists(engineFile))
			{
				engineProcess = new Process();
				engineProcess.StartInfo.FileName = engineFile;
				engineProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engineFile);
				engineProcess.StartInfo.UseShellExecute = false;
				engineProcess.StartInfo.RedirectStandardInput = true;
				engineProcess.StartInfo.Arguments = arguments;
				engineProcess.Start();
				Console.WriteLine($"info string engine on");
			}
			else
			{
				if (engineFile != String.Empty)
					Console.WriteLine($"info string missing engine  [{engineFile}]");
				engineFile = String.Empty;
			}

			Process teacherProcess = null;
			if (File.Exists(teacherFile))
			{
				teacherProcess = new Process();
				teacherProcess.StartInfo.FileName = teacherFile;
				teacherProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(teacherFile);
				teacherProcess.StartInfo.CreateNoWindow = true;
				teacherProcess.StartInfo.RedirectStandardInput = true;
				teacherProcess.StartInfo.RedirectStandardOutput = true;
				teacherProcess.StartInfo.RedirectStandardError = true;
				teacherProcess.StartInfo.UseShellExecute = false;
				teacherProcess.OutputDataReceived += OnDataReceived;
				teacherProcess.Start();
				teacherProcess.BeginOutputReadLine();
				teacherProcess.PriorityClass = ProcessPriorityClass.Idle;
				Console.WriteLine($"info string teacher on");
				TeacherWriteLine("uci");
				TeacherWriteLine("isready");
				TeacherWriteLine("ucinewgame");
			}

			void OnDataReceived(object sender, DataReceivedEventArgs e)
			{
				try
				{
					if (!String.IsNullOrEmpty(e.Data))
					{
						string[] tokens = e.Data.Trim().Split(' ');
						if (tokens[0] == "bestmove")
							lock (locker)
							{
								if (!quit)
								{
									analyzeMoves = $"{analyzeMoves} {tokens[1]}";
									added += book.AddUciMate(analyzeMoves, lastLength);
									if (bookLoaded)
										book.SaveToFile();
									Console.WriteLine($"info string analyze finish {analyzeMoves}");
									if (isLog)
										logUci.Add(analyzeMoves);
								}
							}
					}
				}
				catch { }
			}

			void TeacherWriteLine(string c)
			{
				if (teacherProcess != null)
					if (!teacherProcess.HasExited)
					{
						teacherProcess.StandardInput.WriteLine(c);
						teacherProcess.StandardInput.Flush();
					}
			}

			void TeacherTerminate()
			{
				if (teacherProcess != null)
				{
					teacherProcess.OutputDataReceived -= OnDataReceived;
					teacherProcess.Kill();
					teacherProcess = null;
				}
			}


			if (bookLoaded && (isW || (teacherProcess != null)))
			{
				bookRandom = 0;
				bookLimitR = 0;
				bookLimitW = 0;
			}
			do
			{
				lock (locker)
				{
					string msg = Console.ReadLine().Trim();
					if (String.IsNullOrEmpty(msg) || (msg == "help") || (msg == "book"))
					{
						Console.WriteLine("book load [filename].[mem|pgn|uci|fen] - clear and add moves from file");
						Console.WriteLine("book save [filename].[mem] - save book to the file");
						Console.WriteLine("book delete [number x] - delete x moves from the book");
						Console.WriteLine("book addfile [filename].[mem|png|uci|fen] - add moves to the book from file");
						Console.WriteLine("book adduci [uci] - add moves in uci format to the book");
						Console.WriteLine("book addfen [fen] - add position in fen format");
						Console.WriteLine("book clear - clear all moves from the book");
						Console.WriteLine("book moves [uci] - make sequence of moves in uci format and shows possible continuations");
						Console.WriteLine("book structure - show structure of current book");
						continue;
					}
					Uci.SetMsg(msg);
					int count = book.recList.Count;
					if (Uci.command == "book")
					{
						switch (Uci.tokens[1])
						{
							case "addfen":
								if (book.AddFen(Uci.GetValue(2, 0)))
									Console.WriteLine("Fen have been added");
								else
									Console.WriteLine("Wrong fen");
								break;
							case "addfile":
								string fn = Uci.GetValue(2, 0);
								if (File.Exists(fn))
								{
									book.AddFile(fn);
									book.ShowMoves(true);
								}
								else Console.WriteLine("File not found");
								break;
							case "adduci":
								book.AddUci(Uci.GetValue(2, 0));
								Console.WriteLine($"{(book.recList.Count - count):N0} moves have been added");
								break;
							case "clear":
								book.Clear();
								Console.WriteLine("Book is empty");
								break;
							case "delete":
								int c = book.Delete(Uci.GetInt(2));
								Console.WriteLine($"{c:N0} moves was deleted");
								break;
							case "load":
								book.LoadFromFile(Uci.GetValue(2, 0));
								book.ShowMoves(true);
								break;
							case "moves":
								book.InfoMoves(Uci.GetValue(2, 0));
								break;
							case "structure":
								book.InfoStructure();
								break;
							case "update":
								book.Update();
								break;
							case "save":
								if (book.SaveToFile(Uci.GetValue(2, 0)))
									Console.WriteLine("The book has been saved");
								else
									Console.WriteLine("Writing to the file has failed");
								break;
							default:
								Console.WriteLine($"Unknown command [{Uci.tokens[1]}]");
								break;
						}
						continue;
					}
					if ((Uci.command != "go") && (engineProcess != null))
						engineProcess.StandardInput.WriteLine(msg);
					switch (Uci.command)
					{
						case "position":
							lastFen = Uci.GetValue("fen", "moves");
							lastMoves = Uci.GetValue("moves", "fen");
							book.chess.SetFen(lastFen);
							book.chess.MakeMoves(lastMoves);
							if (String.IsNullOrEmpty(lastFen))
							{
								if (book.chess.g_moveNumber < 2)
								{
									bookChanged = false;
									bookUpdate = isW;
									bookWrite = isW;
									emptyRow = 0;
									added = 0;
									updated = 0;
									deleted = 0;
									analyze = teacherProcess != null;
									quit = false;
									TeacherWriteLine("stop");
								}
								if (bookLoaded && bookWrite && book.chess.Is2ToEnd(out string myMove, out string enMove))
								{
									bookChanged = true;
									bookUpdate = false;
									string[] am = lastMoves.Split(' ');
									List<string> movesUci = new List<string>();
									foreach (string m in am)
										movesUci.Add(m);
									movesUci.Add(myMove);
									movesUci.Add(enMove);
									lastLength = movesUci.Count;
									added += book.AddUciMate(movesUci, lastLength);
								}
							}
							break;
						case "go":
							string move = String.Empty;
							if ((bookLimitR == 0) || (bookLimitR > book.chess.g_moveNumber))
								move = book.GetMove(lastFen, lastMoves, bookRandom,ref bookWrite);
							if (move != String.Empty)
							{
								Console.WriteLine($"bestmove {move}");
								if (bookLoaded && isW && String.IsNullOrEmpty(lastFen) && (emptyRow > 0) && (emptyRow < bookAdd))
								{
									bookChanged = true;
									added += book.AddUci(lastMoves);
								}
								emptyRow = 0;
							}
							else
							{
								emptyRow++;
								if (analyze && (book.chess.GenerateValidMoves(out _).Count > 1))
								{
									analyze = false;
									analyzeMoves = lastMoves;
									TeacherWriteLine("stop");
									TeacherWriteLine($"position startpos moves {analyzeMoves}");
									TeacherWriteLine("go infinite");
									Console.WriteLine($"info string analyze start {analyzeMoves}");
								}
								if (bookUpdate)
								{
									int up = 0;
									if (emptyRow == 1)
										up = book.UpdateBack(lastMoves,0);
									else
									{
										CRec rec = book.recList.GetRec();
										up = book.UpdateRec(rec);
									}
									if (up > 0)
									{
										bookChanged = true;
										updated += up;
									}
								}
								if (engineProcess == null)
									Console.WriteLine("enginemove");
								else
									engineProcess.StandardInput.WriteLine(msg);
							}
							if (bookChanged)
							{
								bookChanged = false;
								book.SaveToFile();
							}
							break;
						case "quit":
							quit = true;
							TeacherTerminate();
							break;
					}
				}
			} while (Uci.command != "quit");
		}
	}
}
