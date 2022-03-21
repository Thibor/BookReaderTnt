using System;
using System.Collections.Generic;

namespace NSProgram
{
	class CRec
	{
		public bool used = false;
		public ulong hash = 0;
		public sbyte mat = 0;
		public byte age = 0xff;
		public int mate = 0;
		public string tnt = String.Empty;
	}

	class CRecList : List<CRec>
	{
		readonly static Random rnd = new Random();

		public bool AddRec(CRec rec)
		{
			int index = FindHash(rec.hash);
			if (index == Count)
				Add(rec);
			else
			{
				CRec r = this[index];
				if (r.hash == rec.hash)
				{
					r.mat = rec.mat;
					r.age = 0xff;
					return false;
				}
				else
					Insert(index, rec);
			}
			return true;
		}

		public int AddHash(CRec rec)
		{
			int index = FindHash(rec.hash);
			if (index == Count)
				Add(rec);
			else
			{
				CRec r = this[index];
				if (r.hash == rec.hash)
				{
					if (r.mat < sbyte.MaxValue)
						r.mat++;
					r.age = 0xff;
					return 0;
				}
				else
					Insert(index, rec);
			}
			return 1;
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
				Shuffle();
				SortAge();
				RemoveRange(Count - count, count);
				SortHash();
			}
			return c - Count;
		}

		public bool RecUpdate(CRec rec)
		{
			int index = FindHash(rec.hash);
			if (index < Count)
			{
				CRec r = this[index];
				if (r.hash == rec.hash)
				{
					if (r.mat != rec.mat)
					{
						r.mat = rec.mat;
						return true;
					}
				}
			}
			return false;
		}

		public int FindHash(ulong hash)
		{
			int first = -1;
			int last = Count;
			while (true)
			{
				if (last - first == 1)
					return last;
				int middle = (first + last) >> 1;
				CRec rec = this[middle];
				if (hash < rec.hash)
					last = middle;
				else if (hash > rec.hash)
					first = middle;
				else
					return middle;
			}
		}

		public CRec GetRec(ulong hash)
		{
			int index = FindHash(hash);
			if (index < Count)
				if (this[index].hash == hash)
					return this[index];
			return null;
		}

		public bool IsHash(ulong hash)
		{
			int index = FindHash(hash);
			if (index < Count)
				return this[index].hash == hash;
			return false;
		}

		public void SetUsed(bool u = false)
		{
			foreach (CRec rec in this)
				rec.used = u;
		}

		public void Shuffle()
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

		public void SortHash()
		{
			Sort(delegate (CRec r1, CRec r2)
			{
				if (r1.hash > r2.hash)
					return 1;
				if (r1.hash < r2.hash)
					return -1;
				return 0;
			});
		}

		public void SortAge()
		{
			Sort(delegate (CRec r1, CRec r2)
			{
				return r2.age - r1.age;
			});
		}


	}
}
