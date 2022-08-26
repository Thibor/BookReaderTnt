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

		public CEmo(int e, CRec r)
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
			int i1 = 0;
			int i2 = Count;
			if (rnd <= 100)
				i2 = (Count * rnd) / 100;
			else
				i1 = ((Count - 1) * (rnd - 100)) / 100;
			return this[CChess.random.Next(i1, i2)];
		}

		public void SetUsed(bool used = true)
		{
			foreach (CEmo e in this)
				e.rec.used = used;
		}

		public void Shuffle()
		{
			int n = Count;
			while (n > 1)
			{
				int k = rnd.Next(n--);
				(this[n], this[k]) = (this[k], this[n]);
			}
		}

		public void SortMat()
		{
			Shuffle();
			Sort(delegate (CEmo e1, CEmo e2)
			{
				double r1 = e1.rec.GetValue();
				double r2 = e2.rec.GetValue();
				if (r1 > r2)
					return -1;
				if (r2 > r1)
					return 1;
				return e2.rec.age - e1.rec.age;
			});
		}
	}
}
