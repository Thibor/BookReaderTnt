﻿using System.Collections.Generic;
using NSChess;

namespace NSProgram
{
	class CChessExt : CChess
	{
		public bool Is1ToEnd()
		{
			int count = 0;
			List<int> am = GenerateAllMoves(whiteTurn, false);
			if (!g_inCheck)
				foreach (int m in am)
				{
					MakeMove(m);
					GenerateAllMoves(whiteTurn, true);
					if (!g_inCheck)
					{
						count++;
						if (GetGameState() != CGameState.normal)
						{
							UnmakeMove(m);
							return true;
						}
					}
					UnmakeMove(m);
				}
			if (count == 0)
				return true;
			return false;
		}

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
				if (whiteTurn)
					y++;
				else
					y--;
				int i = y * 8 + x;
				if (whiteTurn)
				{
					if ((x > 0) && (chars[i - 1] == 'P'))
						chars[i] = 'a';
					if ((x < 7) && (chars[i + 1] == 'P'))
						chars[i] = 'a';

				}
				else
				{
					if ((x > 0) && (chars[i - 1] == 'p'))
						chars[i] = 'A';
					if ((x < 7) && (chars[i + 1] == 'p'))
						chars[i] = 'A';
				}
			}

			return new string(chars);
		}

		public string GetTnt()
		{
			string boaS = GetBoaS();
			if (!whiteTurn)
				boaS = FlipVBoaS(boaS);
			return boaS;
		}

		public void SetTnt(string tnt)
		{
			whiteTurn = true;
			g_castleRights = 0;
			g_lastCastle = 0;
			g_passing = 0;
			g_moveNumber = 0;
			for (int n = 0; n < tnt.Length; n++)
			{
				int index = arrField[n];
				char c = tnt[n];
				int piece = char.IsUpper(c) ? colorWhite : colorBlack;
				switch (char.ToLower(c))
				{
					case 'a':
						piece |= piecePawn;
						g_passing = index - 16;
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
						switch (n)
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
						break;
				}
				g_board[index] = piece;
			}
		}


	}

}
