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
		public const string version = "2025-09-15";
		public const string defExt = ".tnt";
		public CChessExt chess = new CChessExt();
		public CRecList recList = new CRecList();
		public CRapLog log = new CRapLog();
		readonly Stopwatch stopWatch = new Stopwatch();

		public string Title()
		{
            return $"{name} {version}";
        }

        public int DeltaRecords()
        {
            if (maxRecords == 0)
                return 0;
            return recList.Count - maxRecords;
        }

        #region file tnt

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
			if (!File.Exists(p))
				return true;
			try
			{
				using (FileStream fs = File.Open(p, FileMode.Open, FileAccess.Read, FileShare.Read))
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
								index = recList.Count
                            };
							recList.Add(rec);
						}
					}
				}
			}
			catch
			{
				return false;
			}
			recList.SortTnt();
			Program.bookCount = recList.Count;
            return true;
		}

		public bool SaveToTnt(string p)
		{
			if (string.IsNullOrEmpty(p))
				return false;
			string pt = p + ".tmp";
            int del = DeltaRecords();
            if (del > 0)
                Program.deleted += Delete(del);
            recList.SortIndex();
            try
			{
				using (FileStream fs = File.Open(pt, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					using (BinaryWriter writer = new BinaryWriter(fs))
					{
						string lastTnt = String.Empty;
						writer.Write(GetHeader());
						foreach (CRec rec in recList)
						{
							if (rec.tnt == lastTnt)
							{
								Program.deleted++;
								continue;
							}
							TntToMbw(rec.tnt, out ulong m, out ulong b, out ulong w);
							WriteUInt64(writer, m);
							WriteUInt64(writer, b);
							WriteUInt64(writer, w);
							WriteInt16(writer, rec.mat);
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
			recList.SortTnt();
            if (recList.Count / 100 != Program.bookCount/100)
                log.Add($"book {recList.Count:N0} added {Program.added} updated {Program.updated} deleted {Program.deleted:N0} zero {Zero()}");
			Program.bookCount = recList.Count;
            return true;
		}

		#endregion file tnt

		#region file uci

		bool AddFileUci(string p)
		{
			if (!File.Exists(p))
				return true;
			string[] lines = File.ReadAllLines(p);
			foreach (string uci in lines)
				AddUci(uci);
			return true;
		}

		public bool SaveToUci(string p)
		{
			if (string.IsNullOrEmpty(p))
				return false;
			List<string> sl = GetGames();
			using (FileStream fs = File.Open(p, FileMode.Create, FileAccess.Write, FileShare.None))
			using (StreamWriter sw = new StreamWriter(fs))
			{
				foreach (String uci in sl)
					sw.WriteLine(uci);
			}
			return true;
		}

		#endregion file uci

		#region file pgn

		bool AddFilePgn(string p)
		{
			if (!File.Exists(p))
				return true;
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
						if (chess.WhiteTurn)
							pgn += $" {chess.MoveNumber}. {san}";
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

		#endregion file pgn

		#region file txt

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

		#endregion file txt

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

		public void Clear()
		{
			recList.Clear();
		}

		string GetHeader()
		{
			return $"{name} {version}";
		}

		public int UpdateBack(string moves)
		{
			return UpdateBack(moves.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
		}

		public int UpdateBack(string[] moves)
		{
			int result = 0;
			List<CRec> recList = new List<CRec>();
			chess.SetFen();
			foreach (string uci in moves)
				if (chess.MakeMove(uci, out _))
				{
					string tnt = chess.GetTnt();
                    CRec rec = this.recList.GetRec(tnt);
					if (rec != null)
						recList.Add(rec);
					else break;
				}
				else break;
			for (int n = recList.Count - 1; n >= 0; n--)
				result += UpdateRec(recList[n]);
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

		public int AddUci(string moves)
		{
			return AddUci(moves.Trim().Split(' '));
		}

		public int AddUci(string[] moves)
		{
			int ca = 0;
            chess.SetFen();
			foreach (string m in moves)
			{
				if (chess.MakeMove(m, out _))
				{
					CRec rec = new CRec
					{
						tnt = chess.GetTnt(),
						index = recList.Count
                    };
					if (recList.AddRec(rec))
						ca++;
				}
				else
					break;
			}
			return ca;
		}

        public int AddUciMate(List<string> moves)
        {
            return AddUciMate(moves.ToArray());
        }

        public int AddUciMate(string[] moves)
		{
			int ca = 0;
			chess.SetFen();
			for (int n = 0; n < moves.Length; n++)
			{
				string m = moves[n];
				if (!chess.MakeMove(m, out _))
					return ca;
				string tnt = chess.GetTnt();
				short mat = GetMat(n,moves.Length);
				CRec rec = new CRec
				{
					tnt = tnt,
					mat = mat,
					index = recList.Count
                };
				if (recList.AddRec(rec))
					ca++;
				if ((Program.movesAdd > 0) && (ca >= Program.movesAdd))
					break;
			}
			UpdateBack(moves);
			return ca;
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

		public CEmoList GetEmoList(short mat = short.MaxValue, int repetytion = -1)
		{
			CEmoList emoList = new CEmoList();
			List<int> moves = chess.GenerateValidMoves(out _, repetytion);
			foreach (int m in moves)
			{
				chess.MakeMove(m);
				string tnt = chess.GetTnt();
				CRec rec = recList.GetRec(tnt);
				if (rec != null)
					if (Math.Abs(rec.mat) <= mat)
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
				Console.WriteLine($"info string book {umo} {bst.rec.mat:+#;-#;0} possible {emoList.Count} age {bst.rec.index}");
			}
			return umo;
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
					string frm="{0,6} {1,6} {2,6} {3,8}";
                    Console.WriteLine();
                    Console.WriteLine(frm,"id", "move",  "mate", "index");
					int i = 1;
					foreach (CEmo e in el)
					{
						string umo = chess.EmoToUmo(e.emo);
						Console.WriteLine(frm, i++, umo, e.rec.mat,recList.Count - e.rec.index);
					}
				}
			}
		}

		public void ShowInfo()
		{
            if (recList.Count == 0)
            {
                Console.WriteLine("no records");
                return;
            }
			Console.WriteLine($"Zero {Zero()}");
            InfoMoves();
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

        public bool SaveToFile(string p = "")
		{
			if (string.IsNullOrEmpty(p))
				if (string.IsNullOrEmpty(path))
					return false;
				else
					SaveToFile(path);
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

		public bool LoadFromFile(string p = "")
		{
			if (String.IsNullOrEmpty(p))
				if (String.IsNullOrEmpty(path))
					return false;
				else
					return LoadFromFile(path);
			stopWatch.Restart();
			recList.Clear();
			bool result = AddFile(p);
			stopWatch.Stop();
			TimeSpan ts = stopWatch.Elapsed;
			Console.WriteLine($"info string Loaded in {ts.TotalSeconds:N2} seconds");
			return result;
		}

		public bool AddFile(string p)
		{
			string ext = Path.GetExtension(p).ToLower();
			if (ext == defExt)
				return AddFileTnt(p);
			else if (ext == ".uci")
				return AddFileUci(p);
			else if (ext == ".pgn")
				return AddFilePgn(p);
			Console.WriteLine($"info string moves {recList.Count:N0}");
			return false;
		}

		#endregion load

	}
}
