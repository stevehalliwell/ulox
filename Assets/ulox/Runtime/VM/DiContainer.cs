using System.Text;

namespace ULox
{
    public class DiContainer : Table
    {
        internal DiContainer ShallowCopy()
        {
            var ret = new DiContainer();
            foreach (var pair in this)
            {
                ret.Add(pair.Key, pair.Value);
            }
            return ret;
        }

        internal void ReplaceWith(DiContainer diContainerToRestore)
        {
            Clear(); 
            foreach (var pair in diContainerToRestore)
            {
                Add(pair.Key, pair.Value);
            }
        }

        internal string GenerateDump()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Registered in DI:");

            foreach (var item in this)
            {
                sb.AppendLine($"{item.Key}:{item.Value}");
            }

            return sb.ToString().Trim();
        }
    }
}
