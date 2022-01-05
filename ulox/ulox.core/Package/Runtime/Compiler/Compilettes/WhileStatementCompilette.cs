namespace ULox
{
    public class WhileStatementCompilette : ConfigurableLoopingStatementCompilette
    {
        public WhileStatementCompilette()
            : base(TokenType.WHILE ,true, false)
        {
        }
    }
}
