using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSProgram
{
	class CBranch
	{
		public int index = -1;
		public CEmoList emoList = new CEmoList();

		public void Fill()
		{
			index = -1;
			emoList = Program.book.GetEmoList();
		}

		public CEmo GetEmo()
		{
			if ((index >= 0) && (index < emoList.Count))
				return emoList[index];
			return null;
		}

		public CEmo Next()
		{
			index++;
			return GetEmo();
		}
	}

	internal class CBranchList : List<CBranch>
	{
		public bool Next()
		{
			if (Count == 0)
				return false;
			CBranch lastBranch = this.Last();
			CEmo lastEmo = lastBranch.GetEmo();
			if(lastEmo != null)
				Program.book.chess.UnmakeMove(lastEmo.emo);
			CEmo newEmo = lastBranch.Next();
			if (newEmo != null)
			{
				Program.book.chess.MakeMove(newEmo.emo);
				return true;
			}
			else if (Count > 1)
			{
				RemoveAt(Count - 1);
				return Next();
			}
			return false;
		}

		public void Fill()
		{
			CBranch branch = new CBranch();
			branch.Fill();
			CEmo emo = branch.Next();
			if (emo != null)
			{
				Add(branch);
				Program.book.chess.MakeMove(emo.emo);
				Fill();
			}
		}

		public string GetUci()
		{
			string uci = String.Empty;
			foreach (CBranch branch in this)
			{
				CEmo emo = branch.GetEmo();
				if (emo != null)
				{
					string umo = Program.book.chess.EmoToUmo(emo.emo);
					uci = $"{uci} {umo}";
				}
			}
			return uci.Trim();
		}

		public string GetIndex()
		{
			int len = 0;
			string index = String.Empty;
			foreach (CBranch branch in this)
			{
				index = $"{index} {branch.index}";
				if (++len == 16)
					break;
			}
			return index.Trim();
		}

		public void SetUsed()
		{
			foreach (CBranch b in this)
				b.emoList.SetUsed();
		}

	}

}
