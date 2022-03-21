using System.Collections.Generic;
using NSChess;

namespace NSProgram
{
	class CChessExt : CChess
	{
		public bool Is2ToEnd(out string myMov, out string enMov)
		{
			myMov = "";
			enMov = "";
			List<int> mu1 = GenerateValidMoves(out _);//my last move
			foreach (int myMove in mu1)
			{
				bool myEscape = true;
				MakeMove(myMove);
				List<int> mu2 = GenerateValidMoves(out _);//enemy mat move
				foreach (int enMove in mu2)
				{
					bool enAttack = false;
					MakeMove(enMove);
					List<int> mu3 = GenerateValidMoves(out bool mate);//my illegal move
					if (mate)
					{
						myEscape = false;
						enAttack = true;
						myMov = EmoToUmo(myMove);
						enMov = EmoToUmo(enMove);
					}
					UnmakeMove(enMove);
					if (enAttack)
						continue;
				}
				UnmakeMove(myMove);
				if (myEscape)
					return false;
			}
			return true;
		}

		string FlipVBoaS(string boaS)
		{
			string result = string.Empty;
			for (int y = 7; y >= 0; y--)
				for (int x = 0; x < 8; x++)
				{
					char c = boaS[y * 8 + x];
					result += char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
				}
			return result;
		}

		public string GetBoaS()
		{
			string result = "";
			for (int row = 0; row < 8; row++)
				for (int col = 0; col < 8; col++)
				{
					int i = ((row + 4) << 4) + col + 4;
					int piece = g_board[i];
					if (piece == colorEmpty)
						result += "-";
					else
					{
						char[] pieceArr = { ' ', 'p', 'n', 'b', 'r', 'q', 'k', ' ' };
						char pieceChar = pieceArr[piece & 0x7];
						result += ((piece & colorWhite) != 0) ? char.ToUpper(pieceChar) : pieceChar;
					}
				}
			char[] chars = result.ToCharArray();
			if ((g_castleRights & 1) != 0)
				chars[63] = 'T';
			if ((g_castleRights & 2) != 0)
				chars[56] = 'T';
			if ((g_castleRights & 4) != 0)
				chars[7] = 't';
			if ((g_castleRights & 8) != 0)
				chars[0] = 't';
			if (g_passing != 0)
			{
				int x = (g_passing & 0xf) - 4;
				int y = (g_passing >> 4) - 4;
				int i = y * 8 + x;
				if (whiteTurn)
					y++;
				else
					y--;
				if (whiteTurn)
				{
					if ((x > 0) && (chars[i - 1] == 'P'))
						chars[i] = 'w';
					if ((x < 7) && (chars[i + 1] == 'P'))
						chars[i] = 'w';

				}
				else
				{
					if ((x > 0) && (chars[i - 1] == 'p'))
						chars[i] = 'W';
					if ((x < 7) && (chars[i + 1] == 'p'))
						chars[i] = 'W';
				}
			}

			return new string(chars);
		}

		string BoaSToTnt(string boaS)
		{
			char[] chars = boaS.ToCharArray();
			string result = "";
			int empty = 0;
			for (int x = 0; x < 64; x++)
			{
				if (chars[x] == '-')
					empty++;
				else
				{
					if (empty > 0)
						result += empty.ToString();
					result += chars[x];
					empty = 0;
				}
			}
			return result;
		}

		public string GetTnt()
		{
			string boaS = GetBoaS();
			if (!whiteTurn)
				boaS = FlipVBoaS(boaS);
			return BoaSToTnt(boaS);
		}

		public void SetTnt(string tnt)
		{
			whiteTurn = true;
			g_castleRights = 0;
			g_passing = 0;
			for (int n = 0; n < 64; n++)
				g_board[arrField[n]] = colorEmpty;
			int i = 0;
			for (int n = 0; n < tnt.Length; n++)
			{
				char c = tnt[n];
				int piece = char.IsUpper(c) ? colorWhite : colorBlack;
				int x = i % 8;
				int y = i / 8;
				int index = arrField[i];
				switch (char.ToLower(c))
				{
					case 'w':
						piece |= piecePawn;
						g_passing = index;
						break;
					case 'p':
						piece |= piecePawn;
						break;
					case 'n':
						piece |= pieceKnight;
						break;
					case 'b':
						piece |= pieceBishop;
						break;
					case 't':
						piece |= pieceRook;
						switch (i)
						{
							case 63:
								g_castleRights |= 1;
								break;
							case 56:
								g_castleRights |= 2;
								break;
							case 7:
								g_castleRights |= 4;
								break;
							case 0:
								g_castleRights |= 8;
								break;
						}
						break;
					case 'r':
						piece |= pieceRook;
						break;
					case 'q':
						piece |= pieceQueen;
						break;
					case 'k':
						piece |= pieceKing;
						break;
					default:
						piece = colorEmpty;
						int e = c - '0';
						char c2 = tnt[n + 1];
						if (char.IsDigit(c2))
						{
							e = e * 10 + (c2 - '0');
							n++;
						}
						i += (e - 1);
						break;
				}
				i++;
				g_board[index] = piece;
			}
		}


	}

}
