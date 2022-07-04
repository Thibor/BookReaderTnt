using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NSChess;
using RapLog;

namespace NSProgram
{

	class CBook
	{
		string path = String.Empty;
		public int errors = 0;
		public int maxRecords = 0;
		public const string name = "BookReaderTnt";
		public const string version = "2022-07-03";
		public string fileShortName = String.Empty;
		string fileDirectory = String.Empty;
		public const string defExt = ".tnt";
		public CChessExt chess = new CChessExt();
		readonly int[] arrAge = new int[0x100];
		public CRecList recList = new CRecList();
		public CBranchList branchList = new CBranchList();
		public CRapLog log = new CRapLog();

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
			return (AgeAvg() >> 3) + 1;
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

		string GetBookFile()
		{
			return $"{fileShortName}{defExt}";
		}

		string GetBookPath()
		{
			return $@"{fileDirectory}{GetBookFile()}";
		}

		public bool LoadFromFile(string p)
		{
			path = p;
			fileDirectory = Path.GetDirectoryName(p);
			if (fileDirectory != String.Empty)
				fileDirectory = $@"{fileDirectory}\";
			fileShortName = Path.GetFileNameWithoutExtension(p);
			recList.Clear();
			return AddFile(p);
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
			if (String.IsNullOrEmpty(fileShortName))
				fileShortName = Path.GetFileNameWithoutExtension(p);
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

		ulong[] TntToHash(string tnt)
		{
			int z = 0;
			ulong[] hash = new ulong[4];
			for (int h = 0; h < 4; h++)
				for (int n = 0; n < 16; n++)
				{
					ulong p = 0;
					switch (tnt[z++])
					{
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
					hash[h] <<= 4;
					hash[h] |= p;
				}
			return hash;
		}

		string HashToTnt(ulong[] hash)
		{
			string tnt = String.Empty;
			foreach (ulong h in hash)
				for (int n = 15; n >= 0; n--)
				{
					ulong p = (h >> (n << 2)) & 0xf;
					switch (p)
					{
						case 0:
							tnt += "-";
							break;
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
			return tnt;
		}

		bool AddFileTnt(string p)
		{
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
						string header = GetHeader();
						if (reader.ReadString() != header)
							Console.WriteLine($"This program only supports version  [{header}]");
						else
						{
							while (reader.BaseStream.Position != reader.BaseStream.Length)
							{
								ulong[] hash = new ulong[4];
								for (int n = 0; n < hash.Length; n++)
									hash[n] = ReadUInt64(reader);
								CRec rec = new CRec
								{
									tnt = HashToTnt(hash),
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
				AddUci(uci);
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
					AddUci(movesUci);
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
			AddUci(movesUci);
			ShowMoves();
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

		public int UpdateBack(string moves, int count = 0)
		{
			return UpdateBack(moves.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), count);
		}

		public int UpdateBack(List<string> moves, int count = 0)
		{
			return UpdateBack(moves.ToArray(), count);
		}

		public int UpdateBack(string[] moves, int count = 0)
		{
			if ((count == 0) || (count > moves.Length))
				count = moves.Length;
			int result = 0;
			List<int> le = new List<int>();
			chess.SetFen();
			for (int n = 0; n < count; n++)
			{
				string m = moves[n];
				chess.MakeMove(m, out int emo);
				le.Add(emo);
			}
			for (int n = le.Count - 1; n >= 0; n--)
			{
				int emo = le[n];
				chess.UnmakeMove(emo);
				CEmoList emoList = GetEmoList();
				if (emoList.Count > 0)
				{
					string tnt = chess.GetTnt();
					sbyte mat = (sbyte)-emoList[0].rec.mat;
					if (mat > 0)
						mat++;
					CRec rec = recList.GetRec(tnt);
					if (rec != null)
						if (rec.mat != mat)
						{
							rec.mat = mat;
							result++;
						}
				}
			}
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

		public int AddUci(string[] moves, int limit = 0)
		{
			int ca = 0;
			int count = moves.Length;
			if ((limit == 0) || (limit > count))
				limit = count;
			chess.SetFen();
			if (!chess.MakeMoves(moves))
				return 0;
			chess.SetFen();
			for (int n = 0; n < limit; n++)
			{
				string m = moves[n];
				chess.MakeMove(m, out _);
				CRec rec = new CRec
				{
					tnt = chess.GetTnt()
				};
				if (recList.AddRec(rec))
					ca++;
			}
			return ca;
		}

		public int AddUci(string moves, int limit = 0)
		{
			return AddUci(moves.Trim().Split(' '), limit);
		}

		public int AddUci(List<string> moves, int limit = 0)
		{
			return AddUci(moves.ToArray(), limit);
		}

		public int AddUciMate(string moves, int gameLength)
		{
			return AddUciMate(moves.Trim().Split(' '), gameLength);
		}

		public int AddUciMate(List<string> moves, int gameLength)
		{
			return AddUciMate(moves.ToArray(), gameLength);
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
				if ((Program.bookAdd > 0) && (ca >= Program.bookAdd))
					break;
			}
			UpdateBack(moves, ca);
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

		public bool SaveToFile(string p)
		{
			string ext = Path.GetExtension(p).ToLower();
			if (ext == defExt)
				return SaveToTnt(p);
			if (ext == ".uci")
				return SaveToUci(p);
			if (ext == ".pgn")
				return SaveToPgn(p);
			if (ext == ".tns")
				return SaveToTns(p);
			return false;
		}

		public void SaveToFile()
		{
			SaveToFile(GetBookPath());
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

		public bool SaveToTns(string p)
		{
			int line = 0;
			FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None);
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (CRec rec in recList)
				{
					string l = $"{rec.tnt}{rec.mat.ToString("+#;-#;+0")}";
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
							ulong[] hash = TntToHash(rec.tnt);
							if (rec.age < maxAge)
								rec.age++;
							foreach (ulong h in hash)
								WriteUInt64(writer, h);
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
			if (Program.deleted > 0)
				Console.WriteLine($"log book {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0}");
			if (Program.isLog && (maxAge > 0))
				log.Add($"book {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0} max {maxAge}");
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

		public CEmoList GetEmoList(bool onlyNotUsed = false, int repetytion = -1)
		{
			CEmoList emoList = new CEmoList();
			List<int> moves = chess.GenerateValidMoves(out _, repetytion);
			foreach (int m in moves)
			{
				chess.MakeMove(m);
				string tnt = chess.GetTnt();
				CRec rec = recList.GetRec(tnt);
				if (rec != null)
					if (!onlyNotUsed || !rec.used)
					{
						if (onlyNotUsed)
							rec.used = true;
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
			if (bst.rec.mat != 0)
				Console.WriteLine($"info score mate {bst.rec.mat}");
			Console.WriteLine($"info string book {umo} {bst.rec.mat:+#;-#;0} possible {emoList.Count} age {bst.rec.age}");
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

		public void InfoMoves(string moves)
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

		List<string> GetGames()
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

	}
}
