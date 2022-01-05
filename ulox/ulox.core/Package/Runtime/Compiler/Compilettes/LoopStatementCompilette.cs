namespace ULox
{
    public class LoopStatementCompilette : ConfigurableLoopingStatementCompilette
    {
        public LoopStatementCompilette() 
            : base(TokenType.LOOP, false,false)
        {
        }
    }
}
