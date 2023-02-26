
namespace PowerProviderDataHandler.Utils
{
    public static class Helpers
    {
        public static string GetVariable(string name)
        {
            var dic = File.ReadAllLines(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "\\variables.txt")
              .Select(l => l.Split(":="))
              .ToDictionary(s => s[0].Trim(), s => s[1].Trim());
            return dic[name];
        }
    }
}
