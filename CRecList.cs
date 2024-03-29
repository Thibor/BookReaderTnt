﻿using System;
using System.Collections.Generic;

namespace NSProgram
{
	class CRec
	{
		public short mat = 0;
		public byte age = 0;
		public string tnt = String.Empty;

		public double GetValue() {
			return mat == 0 ? 0 : 1.0 / mat;
		}
	}

	class CRecList : List<CRec>
	{
		readonly static Random rnd = new Random();

		public bool AddRec(CRec rec)
		{
			int index = FindTnt(rec.tnt);
			if (index == Count)
				Add(rec);
			else
			{
				CRec r = this[index];
				if (r.tnt == rec.tnt)
				{
					r.age = rec.age;
					r.mat = rec.mat;
					return false;
				}
				else
					Insert(index, rec);
			}
			return true;
		}

		public int RecDelete(int count)
		{
			if (count <= 0)
				return 0;
			int c = Count;
			if (count >= Count)
				Clear();
			else
			{
				SortAge();
				RemoveRange(Count - count, count);
				SortTnt();
			}
			return c - Count;
		}

		public int FindTnt(string tnt)
		{
			int first = -1;
			int last = Count;
			while (true)
			{
				if (last - first == 1)
					return last;
				int middle = (first + last) >> 1;
				CRec rec = this[middle];
				int c = String.Compare(tnt,rec.tnt, StringComparison.Ordinal);
				if (c < 0)
					last = middle;
				else if (c > 0)
					first = middle;
				else
					return middle;
			}
		}

		public CRec GetRec()
		{
			int index = rnd.Next(Count);
			if (index < Count)
				return this[index];
			return null;
		}

		public CRec GetRec(string tnt)
		{
			int index = FindTnt(tnt);
			if (index < Count)
				if (this[index].tnt == tnt)
					return this[index];
			return null;
		}

		public void DelTnt(string tnt)
		{
			if (IsTnt(tnt, out int index))
				RemoveAt(index);
		}

		public bool IsTnt(string tnt,out int index)
		{
			index = FindTnt(tnt);
			if (index < Count)
				return this[index].tnt == tnt;
			return false;
		}

		public void SortRnd()
		{
			int n = Count;
			while (n > 1)
			{
				int k = rnd.Next(n--);
				CRec value = this[k];
				this[k] = this[n];
				this[n] = value;
			}
		}

		public void SortTnt()
		{
			Sort(delegate (CRec r1, CRec r2)
			{
				return String.Compare(r1.tnt,r2.tnt,StringComparison.Ordinal);
			});
		}

		public void SortAge()
		{
			SortRnd();
			Sort(delegate (CRec r1, CRec r2)
			{
				return r1.age - r2.age;
			});
		}


	}
}
