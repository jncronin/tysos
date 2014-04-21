using System;
using System.Collections.Generic;
using System.Text;

namespace test_006
{
    class Program
    {
        static void Main(string[] args)
        {
            cA A = new cA();
            cB B = new cB();
            cC C = new cC();

            int rA = A.fA(2, 1);
            int rB = B.fA(3, 1);
            int rC = C.fA(4, 1);
        }
    }

    class cA
    {
        public virtual int fA(int x, int y)
        {
            return x + y;
        }
    }

    class cB : cA
    {
    }

    class cC : cA
    {
        public override int fA(int x, int y)
        {
            return x - y;
        }
    }
}
