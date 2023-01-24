namespace ULox
{
    public enum ReturnMode : byte
    {
        One,
        Begin,
        End,
        MarkMultiReturnAssignStart,
        MarkMultiReturnAssignEnd,
        Implicit,
    }
}
