using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RapLog;

namespace NSProgram
{

	class CBook
	{
		public string path = String.Empty;
		public int errors = 0;
		public int maxRecords = 0;
		public const string name = "BookReaderTnt";
		public const string version = "2022-08-29";
		public const string defExt = ".tnt";
		public CChessExt chess = new CChessExt();
		readonly int[] arrAge = new int[0x100];
		public CRecList recList = new CRecList();
		public CRapLog log = new CRapLog();
		readonly Stopwatch stopWatch = new Stopwatch();

		public void ShowMoves(bool last = false)
		{
			Console.Write($"\r{recList.Count} moves");
			if (last)
			{
				Console.WriteLine();
				if (errors > 0)
					Console.WriteLine($"{errors} errors");
				errors = 0;
			}
		}

		int AgeAvg()
		{
			return (recList.Count >> 8) + 1;
		}

		int AgeDel()
		{
			return (AgeAvg() >> 1) + 1;
		}

		int AgeMax()
		{
			return AgeAvg() + AgeDel();
		}

		int AgeMin()
		{
			return AgeAvg() - AgeDel();
		}

		public void Clear()
		{
			recList.Clear();
		}

		string GetHeader()
		{
			return $"{name} {version}";
		}

		public bool AddFen(string fen)
		{
			if (chess.SetFen(fen))
			{
				CRec rec = new CRec
				{
					tnt = chess.GetTnt()
				};
				recList.AddRec(rec);
				return true;
			}
			return false;
		}

		public int UpdateBack(string moves)
		{
			return UpdateBack(moves.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
		}

		public int UpdateBack(List<string> moves)
		{
			return UpdateBack(moves.ToArray());
		}

		public int UpdateBack(string[] moves)
		{

			int result = 0;
			List<CRec> lr = new List<CRec>();
			chess.SetFen();
			foreach (string uci in moves)
				if (chess.MakeMove(uci, out _))
				{
					string tnt = chess.GetTnt();
					CRec rec = recList.GetRec(tnt);
					if (rec != null)
						lr.Add(rec);
					else break;
				}
				else break;
			for (int n = lr.Count - 2; n >= 0; n--)
				result += UpdateRec(lr[n]);
			return result;
		}

		public int UpdateRec(CRec rec)
		{
			if (rec == null)
				return 0;
			chess.SetTnt(rec.tnt);
			CEmoList emoList = GetEmoList();
			if (emoList.Count > 0)
			{
				short mat = (short)-emoList[0].rec.mat;
				if (mat > 0)
					mat++;
				if (rec.mat != mat)
				{
					rec.mat = mat;
					return 1;
				}
			}
			return 0;
		}

		public int AddUci(string moves, out string uci, int limitLen = 0, int limitAdd = 0)
		{
			return AddUci(moves.Trim().Split(' '), out uci, limitLen, limitAdd);
		}

		public int AddUci(List<string> moves, out string uci, int limitLen = 0, int limitAdd = 0)
		{
			return AddUci(moves.ToArray(), out uci, limitLen, limitAdd);
		}

		public int AddUciMate(string moves, int gameLength)
		{
			return AddUciMate(moves.Trim().Split(' '), gameLength);
		}

		public int AddUciMate(List<string> moves, int gameLength)
		{
			return AddUciMate(moves.ToArray(), gameLength);
		}

		public int AddUci(string[] moves, out string uci, int limitLen = 0, int limitAdd = 0)
		{
			uci = String.Empty;
			int ca = 0;
			if ((limitLen == 0) || (limitLen > moves.Length))
				limitLen = moves.Length;
			chess.SetFen();
			for (int n = 0; n < limitLen; n++)
			{
				string m = moves[n];
				uci = n == 0 ? m : $"{uci} {m}";
				if (chess.MakeMove(m, out _))
				{
					CRec rec = new CRec
					{
						tnt = chess.GetTnt()
					};
					if (recList.AddRec(rec))
						ca++;
					if ((limitAdd > 0) && (ca >= limitAdd))
						break;
				}
				else
					break;
			}
			return ca;
		}

		public int AddUciMate(string[] moves, int gameLength)
		{
			int ca = 0;
			chess.SetFen();
			for (int n = 0; n < moves.Length; n++)
			{
				string m = moves[n];
				if (!chess.MakeMove(m, out _))
					return ca;
				string tnt = chess.GetTnt();
				short mat = GetMat(n, gameLength);
				CRec rec = new CRec
				{
					tnt = tnt,
					mat = mat
				};
				if (recList.AddRec(rec))
					ca++;
				if ((Program.bookLimitAdd > 0) && (ca >= Program.bookLimitAdd))
					break;
			}
			UpdateBack(moves);
			return ca;
		}

		void RefreshAge()
		{
			for (int n = 0; n < 0x100; n++)
				arrAge[n] = 0;
			foreach (CRec rec in recList)
				arrAge[rec.age]++;
		}

		void WriteUInt64(BinaryWriter writer, ulong v)
		{
			byte[] bytes = BitConverter.GetBytes(v);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			writer.Write(bytes);
		}

		ulong ReadUInt64(BinaryReader reader)
		{
			ulong v = reader.ReadUInt64();
			if (BitConverter.IsLittleEndian)
			{
				byte[] bytes = BitConverter.GetBytes(v).Reverse().ToArray();
				return BitConverter.ToUInt64(bytes, 0);
			}
			return v;
		}

		void WriteInt16(BinaryWriter writer, short v)
		{
			byte[] bytes = BitConverter.GetBytes(v);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			writer.Write(bytes);
		}

		short ReadInt16(BinaryReader reader)
		{
			short v = reader.ReadInt16();
			if (BitConverter.IsLittleEndian)
			{
				byte[] bytes = BitConverter.GetBytes(v).Reverse().ToArray();
				return BitConverter.ToInt16(bytes, 0);
			}
			return v;
		}

		void TntToMbw(string tnt, out ulong m, out ulong b, out ulong w)
		{
			m = 0xFFFFFFFFFFFFFFFF;
			b = 0;
			w = 0;
			int z = 0;
			for (int n = 0; n < 64; n++)
			{
				ulong p = 0;
				switch (tnt[n])
				{
					case '-':
						m ^= 1ul << n;
						break;
					case 'a':
						p = 1;
						break;
					case 'P':
						p = 2;
						break;
					case 'p':
						p = 3;
						break;
					case 'N':
						p = 4;
						break;
					case 'n':
						p = 5;
						break;
					case 'B':
						p = 6;
						break;
					case 'b':
						p = 7;
						break;
					case 'R':
						p = 8;
						break;
					case 'r':
						p = 9;
						break;
					case 'Q':
						p = 10;
						break;
					case 'q':
						p = 11;
						break;
					case 'K':
						p = 12;
						break;
					case 'k':
						p = 13;
						break;
					case 'T':
						p = 14;
						break;
					case 't':
						p = 15;
						break;
				}
				if (p > 0)
				{
					int s = (z & 0xf) << 2;
					if (z++ < 16)
						b |= p << s;
					else
						w |= p << s;
				}
			}
		}

		string MbwToTnt(ulong m, ulong b, ulong w)
		{
			string tnt = String.Empty;
			int z = 0;
			for (int n = 0; n < 64; n++)
			{
				if ((m & (1ul << n)) == 0)
					tnt += "-";
				else
				{
					int s = (z & 0xf) << 2;
					ulong p = z++ < 16 ? (b >> s) & 0xf : (w >> s) & 0xf;
					switch (p)
					{
						case 1:
							tnt += "a";
							break;
						case 2:
							tnt += "P";
							break;
						case 3:
							tnt += "p";
							break;
						case 4:
							tnt += "N";
							break;
						case 5:
							tnt += "n";
							break;
						case 6:
							tnt += "B";
							break;
						case 7:
							tnt += "b";
							break;
						case 8:
							tnt += "R";
							break;
						case 9:
							tnt += "r";
							break;
						case 10:
							tnt += "Q";
							break;
						case 11:
							tnt += "q";
							break;
						case 12:
							tnt += "K";
							break;
						case 13:
							tnt += "k";
							break;
						case 14:
							tnt += "T";
							break;
						case 15:
							tnt += "t";
							break;
					}
				}
			}
			return tnt;
		}

		int GetMaxAge()
		{
			int max = AgeMax();
			int last = 0;
			for (int n = 0; n < 0xff; n++)
			{
				int cur = arrAge[n];
				if (last + cur < max)
					return n;
				last = cur;
			}
			return 0xfF;
		}

		public int Delete(int c)
		{
			return recList.RecDelete(c);
		}

		public bool IsWinner(int index, int count)
		{
			return (index & 1) != (count & 1);
		}

		public short GetMat(int index, int count)
		{
			int mate = (count - index + 1) >> 1;
			if (!IsWinner(index, count))
				mate = -mate;
			return (short)mate;
		}

		public CEmoList GetNotUsedList(CEmoList el)
		{
			if (el.Count == 0)
				return el;
			CEmoList emoList = new CEmoList();
			List<int> moves = chess.GenerateValidMoves(out _);
			foreach (int m in moves)
			{
				if (el.GetEmo(m) == null)
				{
					CEmo emo = new CEmo(m);
					emoList.Add(emo);
				}
			}
			if (emoList.Count > 0)
				return emoList;
			return el;
		}

		public CEmoList GetEmoList(short mat=short.MaxValue, int repetytion = -1)
		{
			CEmoList emoList = new CEmoList();
			List<int> moves = chess.GenerateValidMoves(out _, repetytion);
			foreach (int m in moves)
			{
				chess.MakeMove(m);
				string tnt = chess.GetTnt();
				CRec rec = recList.GetRec(tnt);
				if (rec != null)
					if (Math.Abs(rec.mat)<=mat)
					{
						CEmo emo = new CEmo(m, rec);
						emoList.Add(emo);
					}
				chess.UnmakeMove(m);
			}
			emoList.SortMat();
			return emoList;
		}

		public string GetMove(string fen, string moves, int rnd, ref bool bookWrite)
		{
			chess.SetFen(fen);
			chess.MakeMoves(moves);
			CEmoList emoList = GetEmoList();
			if (rnd > 200)
			{
				rnd = 100;
				emoList = GetNotUsedList(emoList);
			}
			if (emoList.Count == 0)
				return String.Empty;
			CEmo bst = emoList.GetRnd(rnd);
			chess.MakeMove(bst.emo);
			if (chess.IsRepetition())
			{
				bookWrite = false;
				return String.Empty;
			}
			string umo = chess.EmoToUmo(bst.emo);
			if (bst.rec != null)
			{
				if (bst.rec.mat != 0)
					Console.WriteLine($"info score mate {bst.rec.mat}");
				Console.WriteLine($"info string book {umo} {bst.rec.mat:+#;-#;0} possible {emoList.Count} age {bst.rec.age}");
			}
			return umo;
		}

		public void ShowLevel(int lev, int len)
		{
			int ageMax = AgeMax();
			int ageMin = AgeMin();
			int ageCou = arrAge[lev];
			int del = 0;
			if (ageCou < ageMin)
				del = ageCou - ageMin;
			if (ageCou > ageMax)
				del = ageCou - ageMax;
			Console.WriteLine("{0,4} {1," + len + "} {2," + len + "}", lev, arrAge[lev], del);
		}

		public void InfoStructure()
		{
			int len = recList.Count.ToString().Length;
			int ageAvg = AgeAvg();
			int ageMax = AgeMax();
			int ageMin = AgeMin();
			int ageDel = AgeDel();
			Console.WriteLine($"moves {recList.Count:N0} min {ageMin:N0} avg {ageAvg:N0} max {ageMax:N0} delta {ageDel:N0}");
			Console.WriteLine("{0,4} {1," + len + "} {2," + len + "}", "age", "count", "delta");
			Console.WriteLine();
			RefreshAge();
			ShowLevel(0, len);
			for (int n = 1; n < 0xff; n++)
			{
				if ((arrAge[n] > ageMax) || (arrAge[n] < ageMin))
					ShowLevel(n, len);
				if (arrAge[n] == 0)
					break;
			}
			ShowLevel(255, len);
		}

		public void InfoMoves(string moves = "")
		{
			chess.SetFen();
			if (!chess.MakeMoves(moves))
				Console.WriteLine("wrong moves");
			else
			{
				CEmoList el = GetEmoList();
				if (el.Count == 0)
					Console.WriteLine("no moves found");
				else
				{
					Console.WriteLine("id move  mate age");
					Console.WriteLine();
					int i = 1;
					foreach (CEmo e in el)
					{
						string umo = chess.EmoToUmo(e.emo);
						Console.WriteLine(String.Format("{0,2} {1,-4} {2,5} {3,3}", i++, umo, e.rec.mat, e.rec.age));
					}
				}
			}
		}

		public void Update()
		{
			Program.added = 0;
			Program.updated = 0;
			Program.deleted = 0;
			int up = recList.Count;
			int max;
			do
			{
				int line = 0;
				max = up;
				up = 0;
				foreach (CRec rec in recList)
				{
					up += UpdateRec(rec);
					Console.Write($"\rupdate {(++line * 100.0 / recList.Count):N4}%");
				}
				Program.updated += up;
				Console.WriteLine();
				Console.WriteLine($"Updated {up:N0}");
				SaveToFile();
			} while ((max > up) && (up > 0));
			Console.WriteLine($"records {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0}");
		}

		int Zero()
		{
			int z = 0;
			foreach (CRec rec in recList)
				if (rec.mat == 0)
					z++;
			return z;
		}

		#region save

		public bool SaveToFile(string p)
		{
			string ext = Path.GetExtension(p).ToLower();
			if (ext == defExt)
				return SaveToTnt(p);
			if (ext == ".uci")
				return SaveToUci(p);
			if (ext == ".pgn")
				return SaveToPgn(p);
			if (ext == ".txt")
				return SaveToTxt(p);
			return false;
		}

		public void SaveToFile()
		{
			if (!string.IsNullOrEmpty(path))
				SaveToFile(path);
		}

		public bool SaveToUci(string p)
		{
			List<string> sl = GetGames();
			FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None);
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (String uci in sl)
					sw.WriteLine(uci);
			}
			return true;
		}

		public bool SaveToTxt(string p)
		{
			int line = 0;
			FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None);
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (CRec rec in recList)
				{
					string l = $"{rec.tnt}{rec.mat:+#;-#;+0}";
					sw.WriteLine(l);
					Console.Write($"\rRecord {++line}");
				}
			}
			Console.WriteLine();
			return true;
		}

		public bool SaveToTnt(string p)
		{
			string pt = p + ".tmp";
			RefreshAge();
			int maxAge = GetMaxAge();
			Program.deleted = 0;
			if (maxRecords > 0)
				Program.deleted = recList.Count - maxRecords;
			else if (maxAge == 0xff)
				Program.deleted = AgeAvg() >> 5;
			if (Program.deleted > 0)
				Delete(Program.deleted);
			try
			{
				using (FileStream fs = File.Open(pt, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					using (BinaryWriter writer = new BinaryWriter(fs))
					{
						string lastTnt = String.Empty;
						recList.SortTnt();
						writer.Write(GetHeader());
						foreach (CRec rec in recList)
						{
							if (rec.tnt == lastTnt)
							{
								Program.deleted++;
								continue;
							}
							if (rec.age < maxAge)
								rec.age++;
							TntToMbw(rec.tnt, out ulong m, out ulong b, out ulong w);
							WriteUInt64(writer, m);
							WriteUInt64(writer, b);
							WriteUInt64(writer, w);
							WriteInt16(writer, rec.mat);
							writer.Write(rec.age);
							lastTnt = rec.tnt;
						}
					}
				}
			}
			catch
			{
				return false;
			}
			try
			{
				if (File.Exists(p) && File.Exists(pt))
					File.Delete(p);
			}
			catch
			{
				return false;
			}
			try
			{
				if (!File.Exists(p) && File.Exists(pt))
					File.Move(pt, p);
			}
			catch
			{
				return false;
			}
			if (Program.isLog && (maxAge > 0))
				log.Add($"book {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0} max {maxAge} zero {Zero()}");
			return true;
		}

		public bool SaveToPgn(string p)
		{
			List<string> sl = GetGames();
			int line = 0;
			FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None);
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (String uci in sl)
				{
					string[] arrMoves = uci.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					chess.SetFen();
					string pgn = String.Empty;
					foreach (string umo in arrMoves)
					{
						string san = chess.UmoToSan(umo);
						if (san == String.Empty)
							break;
						int number = (chess.g_moveNumber >> 1) + 1;
						if (chess.whiteTurn)
							pgn += $" {number}. {san}";
						else
							pgn += $" {san}";
						int emo = chess.UmoToEmo(umo);
						chess.MakeMove(emo);
					}
					sw.WriteLine();
					sw.WriteLine("[White \"White\"]");
					sw.WriteLine("[Black \"Black\"]");
					sw.WriteLine();
					sw.WriteLine(pgn.Trim());
					Console.Write($"\rgames {++line}");
				}
			}
			Console.WriteLine();
			return true;
		}

		/*List<string> GetGames()
		{
			List<string> sl = new List<string>();
			if (branchList.Start())
				do
				{
					string uci = branchList.GetUci();
					sl.Add(uci);
					Console.Write($"\rsearch {branchList.GetProcent():N4}%");
				} while (branchList.BlNext());
			double pro = (branchList.used * 100.0) / recList.Count;
			Console.WriteLine();
			Console.WriteLine($"games {sl.Count:N0} used {branchList.used:N0} ({pro:N2}%)");
			sl.Sort();
			return sl;
		}*/

		List<string> GetGames()
		{
			List<string> sl = new List<string>();
			GetGames(string.Empty, 0, short.MaxValue, 0, 1, ref sl);
			Console.WriteLine();
			Console.WriteLine("finish");
			Console.Beep();
			sl.Sort();
			return sl;
		}

		void GetGames(string moves, int ply, short score, double proT, double proU, ref List<string> list)
		{
			bool add = true;
			if (ply < 12)
			{
				chess.SetFen();
				chess.MakeMoves(moves);
				CEmoList el = GetEmoList();
				if (el.Count > 0)
				{
					proU /= el.Count;
					for (int n = 0; n < el.Count; n++)
					{
						CEmo emo = el[n];
						short curScore = Math.Abs(emo.rec.mat);
						double p = proT + n * proU;
						if (curScore <= score)
						{
							add = false;
							GetGames($"{moves} {chess.EmoToUmo(emo.emo)}".Trim(), ply + 1, curScore, p, proU, ref list);
						}
					}
				}
			}
			if (add)
			{
				list.Add(moves);
				double pro = (proT + proU) * 100.0;
				Console.Write($"\r{pro:N4} %");
			}
		}

		#endregion save

		#region load

		public bool LoadFromFile(string p)
		{
			if (String.IsNullOrEmpty(p))
				return false;
			stopWatch.Restart();
			recList.Clear();
			bool result = AddFile(p);
			stopWatch.Stop();
			TimeSpan ts = stopWatch.Elapsed;
			Console.WriteLine($"info string Loaded in {ts.Seconds}.{ts.Milliseconds} seconds");
			return result;
		}

		public bool LoadFromFile()
		{
			return LoadFromFile(path);
		}

		public bool AddFile(string p)
		{
			bool result = true;
			if (!File.Exists(p) && (!File.Exists(p + ".tmp")))
				return true;
			string ext = Path.GetExtension(p).ToLower();
			if (ext == defExt)
				result = AddFileTnt(p);
			else if (ext == ".uci")
				AddFileUci(p);
			else if (ext == ".pgn")
				AddFilePgn(p);
			Console.WriteLine($"info string moves {recList.Count:N0}");
			return result;
		}

		bool AddFileTnt(string p)
		{
			path = p;
			string pt = p + ".tmp";
			try
			{
				if (!File.Exists(p) && File.Exists(pt))
					File.Move(pt, p);
			}
			catch
			{
				return false;
			}
			try
			{
				using (FileStream fs = File.Open(p, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using (BinaryReader reader = new BinaryReader(fs))
					{
						string headerBst = GetHeader();
						string headerCur = reader.ReadString();
						if (!Program.isIv && (headerCur != headerBst))
							Console.WriteLine($"This program only supports version  [{headerBst}]");
						else
						{
							while (reader.BaseStream.Position != reader.BaseStream.Length)
							{
								ulong m = ReadUInt64(reader);
								ulong b = ReadUInt64(reader);
								ulong w = ReadUInt64(reader);
								CRec rec = new CRec
								{
									tnt = MbwToTnt(m, b, w),
									mat = ReadInt16(reader),
									age = reader.ReadByte()
								};
								recList.Add(rec);
							}
						}
					}
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		void AddFileUci(string p)
		{
			string[] lines = File.ReadAllLines(p);
			foreach (string uci in lines)
				AddUci(uci, out _);
		}

		void AddFilePgn(string p)
		{
			List<string> listPgn = File.ReadAllLines(p).ToList();
			string movesUci = String.Empty;
			chess.SetFen();
			foreach (string m in listPgn)
			{
				string cm = m.Trim();
				if (String.IsNullOrEmpty(cm))
					continue;
				if (cm[0] == '[')
					continue;
				cm = Regex.Replace(cm, @"\.(?! |$)", ". ");
				if (cm.StartsWith("1. "))
				{
					AddUci(movesUci, out _);
					ShowMoves();
					movesUci = String.Empty;
					chess.SetFen();
				}
				string[] arrMoves = cm.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string san in arrMoves)
				{
					if (Char.IsDigit(san[0]))
						continue;
					string umo = chess.SanToUmo(san);
					if (umo == String.Empty)
					{
						errors++;
						break;
					}
					movesUci += $" {umo}";
					int emo = chess.UmoToEmo(umo);
					chess.MakeMove(emo);
				}
			}
			AddUci(movesUci, out _);
			ShowMoves();
		}

		#endregion load

	}
}
