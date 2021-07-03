using System.Text;

namespace ULox
{
    public class DumpGlobals
    {
        private StringBuilder sb = new StringBuilder();

        public string Generate(Table globals)
        {
            int count = 0;
            foreach (var item in globals)
            {
                sb.Append($"{item.Key} : {item.Value}");

                if (count != globals.Count)
                    sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
