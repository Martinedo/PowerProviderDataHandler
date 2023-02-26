using PowerProviderDataHandler.Utils;
using SQLite;
using System.Data;

string EIC_CONSUMER = Helpers.GetVariable(nameof(EIC_CONSUMER));
string EIC_PRODUCER = Helpers.GetVariable(nameof(EIC_PRODUCER));

List<Unit> unitList = new ();

SQLiteConnection Database = new(Helpers.GetVariable("DB_SQLITE_PATH"), SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
Database.CreateTable<Unit>();

string[] files = Directory.GetFiles(Helpers.GetVariable("PATH_TO_EXPORTS"));

foreach (string file in files)
{
    string? EIC = null;
    int i = 14;
    using (var reader = new StreamReader(file))
    {
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            
            if (i-- > 0 || string.IsNullOrEmpty(line) || !line.Contains(";"))
            {
                if (line.Contains("EIC"))
                {
                    EIC = line.Split(';')[1];
                    continue;
                }
                continue;
            }
            var values = line.Split(';');

            unitList.Add(new ()
            {
                TimeStamp = (int)DateTime.ParseExact(values[0], "dd.MM.yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                Value = double.Parse(values[1].Replace("\"","")),
                Status = values.Length > 2  ? values[2] : "",
                Type = EIC == EIC_PRODUCER ? Type.PRODUCED_TO_GRID : Type.CONSUMED_FROM_GRID
            });
        }
    }
}

foreach (var unit in unitList)
{
    //Console.WriteLine(csvInfo.TimeStamp.ToString() + " --- " + csvInfo.Type);
}

foreach (var unit in unitList)
{
    Unit unitFound = Database.Table<Unit>().FirstOrDefault(u => u.TimeStamp == unit.TimeStamp && u.Type == u.Type);
    if (unitFound != null)
    {
        unitFound.Exists = true;
    }
}

Database.InsertAll(unitList.Where(c => !c.Exists));

Database.Commit();
Database.Close();

public class Unit
{
    public int TimeStamp { get; set; }
    public double Value { get; set; }
    public string? Status { get; set; }
    //1 - CONSUMER, 2 - PRODUCER
    public Type Type { get; set; }
    public bool Exists { get; set; }
}

public enum Type
{
    CONSUMED_FROM_GRID = 1,
    PRODUCED_TO_GRID = 2,
    CONSUMED_FROM_SOLAR = 3,
    PRODUCED_FROM_SOLAR = 4
}