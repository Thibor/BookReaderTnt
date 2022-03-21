using System;
using System.Collections.Generic;
using NSChess;

namespace NSProgram
{
	class CEmo
	{
		public int emo = 0;
		public CRec rec = null;

		public CEmo(int e)
		{
			emo = e;
		}

		public CEmo(int e,CRec r)
		{
			emo = e;
			rec = r;
		}

	}

	class CEmoList : List<CEmo>
	{
		readonly static Random rnd = new Random();

		public CEmo GetEmo(int emo)
		{
			foreach (CEmo e in this)
				if (e.emo == emo)
					return e;
			return null;
		}

		public CEmo GetRnd(int rnd = 0)
		{
			if (Count == 0)
				return null;
			if (rnd < 0)
				rnd = 0;
			bool r = rnd > 100;
			if (r)
				rnd = 200 - rnd;
			CEmo bst = this[0];
			double bd = -200.0;
			double h = rnd / 100.0;
			foreach(CEmo e in this)
			{
				double m = r ? 127.0 - e.rec.mat : e.rec.mat + 128.0;
				double cd = m * (1.0 - CChess.random.NextDouble() * h);
				if (bd < cd)
				{
					bd = cd;
					bst = e;
				}
			}
			return bst;
		}

		public void SetUsed()
		{
			foreach(CEmo e in this)
				e.rec.used = true;
		}

		public void Shuffle()
		{
			int n = Count;
			while (n > 1)
			{
				int k = rnd.Next(n--);
				CEmo value = this[k];
				this[k] = this[n];
				this[n] = value;
			}
		}

		public void SortMat()
		{
			Shuffle();
			Sort(delegate (CEmo e1, CEmo e2)
			{
				int r = e2.rec.mat - e1.rec.mat;
				if (r != 0)
					return r;
				return e1.rec.age - e2.rec.age;
			});
		}
	}
}
