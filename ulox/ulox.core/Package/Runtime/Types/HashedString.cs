using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class HashedString : IComparable<HashedString>
    {
        public readonly int Hash;
        public readonly string String;

        public HashedString(string str)
        {
            String = str;
            Hash = str.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(HashedString other) => this.Hash.CompareTo(other.Hash);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(HashedString left, HashedString right)
        {
            if (((object)left) == null && ((object)right) == null)
                return true;
            if (((object)left) == null ^ ((object)right) == null)
                return false;

            return left.Hash == right.Hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(HashedString left, HashedString right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => String;
    }

    public sealed class HashedStringComparer : IEqualityComparer<HashedString>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(HashedString x, HashedString y) => x.Hash == y.Hash;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(HashedString obj) => obj.Hash;
    }
}
