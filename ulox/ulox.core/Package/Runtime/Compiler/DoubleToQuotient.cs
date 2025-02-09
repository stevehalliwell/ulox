using System;

namespace ULox
{
    public static class DoubleToQuotient
    {
        // adapted from https://rosettacode.org/wiki/Convert_decimal_number_to_rational#C++
        public static (bool isPossible, int nume, uint denom) ToQuotient(double num, int decimalPlances)
        {
            if (num == 0.0)
            {
                return (true, 0, 1);
            }
            var isNegative = num < 0.0;
            if (isNegative)
            {
                num = -num;
            }

            double epsilon = 1.0 / Math.Pow(10, decimalPlances);

            if (Math.Abs(num - Math.Round(num)) < epsilon)
            {
                return (true, (int)Math.Round(num), 1);
            }

            ulong a = 0;
            ulong b = 1;
            ulong c = (ulong)Math.Ceiling(num);
            ulong d = 1;
            ulong auxiliary_1 = uint.MaxValue / 2;

            while (c < auxiliary_1 && d < auxiliary_1)
            {
                var auxiliary_2 = (a + c) / (double)(b + d);

                if (Math.Abs(num - auxiliary_2) < epsilon)
                {
                    break;
                }

                if (num > auxiliary_2)
                {
                    a = a + c;
                    b = b + d;
                }
                else
                {
                    c = a + c;
                    d = b + d;
                }
            }

            var divisor = GCD((a + c), (b + d));
            var numeUL = (a + c) / divisor;
            var denomUL = (b + d) / divisor;
            if (numeUL > uint.MaxValue || denomUL > uint.MaxValue)
            {
                return (false, 0, 0);
            }
            return (true, (isNegative ? -1 : 1 ) * (int)numeUL, (uint)denomUL);
        }

        //  https://stackoverflow.com/questions/18541832/c-sharp-find-the-greatest-common-divisor
        private static ulong GCD(ulong a, ulong b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a | b;
        }
    }
}
