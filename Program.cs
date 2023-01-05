using NSUci;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
		public static int bookLimitAdd = 5;
		/// <summary>
		/// Limit ply to wrtie.
		/// </summary>
		public static int bookLimitW = 32;
		/// <summary>
		/// Limit ply to read.
		/// </summary>
		public static int bookLimitR = 0xf;
		public static bool isIv = false;
		public static CBook book = new CBook();

		static void Main(string[] args)
		{
			bool bookChanged = false;
			bool bookUpdate = false;
			bool bookWrite = false;
			bool isInfo = false;
			/// <summary>
			/// Book can update moves.
			/// </summary>
			bool isW = false;
			/// <summary>
			/// Number of moves not found in a row.
			/// </summary>
			int emptyRow = 0;
			/// <summary>
			/// Random moves factor.
			/// </summary>
			int bookRandom = 60;
			string lastFen = String.Empty;
			string lastMoves = String.Empty;
			CUci uci = new CUci();
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
					case "-info":
						ax = ac;
						isInfo = true;
						break;
					case "-iv":
						ax = ac;
						isIv = true;
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
								bookLimitAdd = int.TryParse(ac, out int a) ? a : bookLimitAdd;
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
			string engineArguments = String.Join(" ", listEa);
			string ext = Path.GetExtension(bookName);
			Console.WriteLine($"info string book {CBook.name} ver {CBook.version}");
			if (String.IsNullOrEmpty(ext))
				bookName = $"{bookName}{CBook.defExt}";
			bool bookLoaded = book.LoadFromFile(bookName);
			if (bookLoaded && (book.recList.Count > 0))
			{
				FileInfo fi = new FileInfo(book.path);
				long bpm = (fi.Length << 3) / book.recList.Count;
				Console.WriteLine($"info string book on {book.recList.Count:N0} moves {bpm} bpm");
				if (isW)
					Console.WriteLine($"info string write on ");
			}
			Process engineProcess = null;
			if (File.Exists(engineFile))
			{
				engineProcess = new Process();
				engineProcess.StartInfo.FileName = engineFile;
				engineProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engineFile);
				engineProcess.StartInfo.UseShellExecute = false;
				engineProcess.StartInfo.RedirectStandardInput = true;
				engineProcess.StartInfo.Arguments = engineArguments;
				engineProcess.Start();
				Console.WriteLine($"info string engine on");
			}
			else if (engineFile != String.Empty)
					Console.WriteLine($"info string missing engine  [{engineFile}]");
			if (bookLoaded && isW)
			{
				bookRandom = 0;
				bookLimitR = 0;
				bookLimitW = 0;
			}
			if (isInfo)
				book.InfoMoves();
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
					uci.SetMsg(msg);
					int count = book.recList.Count;
					if (uci.command == "book")
					{
						switch (uci.tokens[1])
						{
							case "addfen":
								if (book.AddFen(uci.GetValue("addfen")))
									Console.WriteLine("Fen have been added");
								else
									Console.WriteLine("Wrong fen");
								break;
							case "addfile":
								string fn = uci.GetValue("addfile");
								if (File.Exists(fn))
								{
									book.AddFile(fn);
									book.ShowMoves(true);
								}
								else Console.WriteLine("File not found");
								break;
							case "adduci":
								book.AddUci(uci.GetValue("adduci"),out _);
								Console.WriteLine($"{book.recList.Count - count:N0} moves have been added");
								break;
							case "clear":
								book.Clear();
								Console.WriteLine("Book is empty");
								break;
							case "delete":
								int c = book.Delete(uci.GetInt("delete"));
								Console.WriteLine($"{c:N0} moves was deleted");
								break;
							case "load":
								book.LoadFromFile(uci.GetValue("load"));
								book.ShowMoves(true);
								break;
							case "moves":
								book.InfoMoves(uci.GetValue("moves"));
								break;
							case "structure":
								book.InfoStructure();
								break;
							case "update":
								book.Update();
								break;
							case "save":
								if (book.SaveToFile(uci.GetValue("save")))
									Console.WriteLine("The book has been saved");
								else
									Console.WriteLine("Writing to the file has failed");
								break;
							case "getoption":
								Console.WriteLine($"option name Book file type string default book{CBook.defExt}");
								Console.WriteLine($"option name Engine file type string default");
								Console.WriteLine($"option name Engine arguments type string default");
								Console.WriteLine($"option name Teacher file type string default");
								Console.WriteLine($"option name Write type check default false");
								Console.WriteLine($"option name Log type check default false");
								Console.WriteLine($"option name Limit add moves type spin default {bookLimitAdd} min 0 max 100");
								Console.WriteLine($"option name Limit read moves type spin default {bookLimitR} min 0 max 100");
								Console.WriteLine($"option name Limit write moves type spin default {bookLimitW} min 0 max 100");
								Console.WriteLine($"option name Random moves type spin default {bookRandom} min 0 max 201");
								Console.WriteLine("optionok");
								break;
							case "setoption":
								switch (uci.GetValue("name", "value").ToLower())
								{
									case "book file":
										bookLoaded=book.LoadFromFile(uci.GetValue("value"));
										break;
									case "engine file":
										engineFile = uci.GetValue("value");
										break;
									case "engine arguments":
										engineArguments = uci.GetValue("value");
										break;
									case "write":
										isW = uci.GetValue("value") == "true";
										break;
									case "log":
										isLog = uci.GetValue("value") == "true";
										break;
									case "limit add":
										bookLimitAdd = uci.GetInt("value");
										break;
									case "limit read":
										bookLimitR = uci.GetInt("value");
										break;
									case "limit write":
										bookLimitW = uci.GetInt("value");
										break;
									case "Random":
										bookRandom = uci.GetInt("value");
										break;
								}
								break;
							default:
								Console.WriteLine($"Unknown command [{uci.tokens[1]}]");
								break;
						}
						continue;
					}
					if ((uci.command != "go") && (engineProcess != null))
						engineProcess.StandardInput.WriteLine(msg);
					switch (uci.command)
					{
						case "position":
							lastFen = uci.GetValue("fen", "moves");
							lastMoves = uci.GetValue("moves", "fen");
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
									added += book.AddUciMate(movesUci, movesUci.Count);
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
								if (bookLoaded && isW && String.IsNullOrEmpty(lastFen) && (emptyRow > 0) && (emptyRow < bookLimitAdd))
								{
									bookChanged = true;
									added += book.AddUci(lastMoves,out _);
								}
								emptyRow = 0;
							}
							else
							{
								emptyRow++;
								if (bookUpdate)
								{
									int up = 0;
									if (emptyRow == 1)
										up = book.UpdateBack(lastMoves);
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
					}
				}
			} while (uci.command != "quit");
		}
	}
}
