// See https://aka.ms/new-console-template for more information

using PowerProviderDataHandler.Utils;
using System.Data;
using System.Data.SqlClient;

string EIC_CONSUMER = Helpers.GetVariable(nameof(EIC_CONSUMER));
string EIC_PRODUCER = Helpers.GetVariable(nameof(EIC_PRODUCER));


List<CsvInfo> csvInfoList = new List<CsvInfo>();


string[] files = Directory.GetFiles(Helpers.GetVariable("PATH"));

foreach (string file in files)
{
    string EIC = null;
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

            
            
            csvInfoList.Add(new CsvInfo()
            {
                TimeStamp = DateTime.ParseExact(values[0], "dd.MM.yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                Value = double.Parse(values[1].Replace("\"","")),
                Status = values[2],
                Type = EIC == EIC_PRODUCER ? 2 : 1
            });
        }
    }
}


foreach (var csvInfo in csvInfoList)
{
    //Console.WriteLine(csvInfo.TimeStamp.ToString() + " --- " + csvInfo.Type);
}

//DB connection


//insert to DB

using (SqlConnection openCon = new SqlConnection(Helpers.GetVariable("DB_CONNECTION_STRING")))
{
    openCon.Open();

    SqlDataReader sqlDataReader;
    SqlCommand sqlCommand;

    foreach (var csvInfo in csvInfoList)
    {
        sqlCommand = new SqlCommand($"select TIMESTAMP, VALUE, TYPE from ELEKTRINA where TIMESTAMP = '{csvInfo.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss")}' and TYPE = {csvInfo.Type}", openCon);
        sqlDataReader = sqlCommand.ExecuteReader();
        csvInfo.Exists = sqlDataReader.HasRows;
        sqlDataReader.Close();
        sqlCommand.Dispose();
    }

    using (SqlTransaction oTransaction = openCon.BeginTransaction())
    {
        using (SqlCommand oCommand = openCon.CreateCommand())
        {
            oCommand.Transaction = oTransaction;
            oCommand.CommandType = CommandType.Text;
            oCommand.CommandText = "INSERT INTO ELEKTRINA (TIMESTAMP, VALUE, TYPE) VALUES (@t1, @v1, @t2);";
            oCommand.Parameters.Add(new SqlParameter("@t1", SqlDbType.DateTime));
            oCommand.Parameters.Add(new SqlParameter("@v1", SqlDbType.Float));
            oCommand.Parameters.Add(new SqlParameter("@t2", SqlDbType.Int));
            try
            {
                foreach (var csvInfo in csvInfoList.Where(c => !c.Exists))
                {
                    //Console.WriteLine(csvInfo.TimeStamp.ToString() + " --- " + csvInfo.Type);
                    oCommand.Parameters[0].Value = csvInfo.TimeStamp;
                    oCommand.Parameters[1].Value = csvInfo.Value;
                    oCommand.Parameters[2].Value = csvInfo.Type;
                    if (oCommand.ExecuteNonQuery() != 1)
                    {
                        //'handled as needed, 
                        //' but this snippet will throw an exception to force a rollback
                        throw new InvalidProgramException();
                    }
                }
                oTransaction.Commit();

            }
            catch (Exception)
            {
                oTransaction.Rollback();
                throw;
            }
        }
    }

    openCon.Close();
}

public class CsvInfo
{
    public DateTime TimeStamp { get; set; }
    public double Value { get; set; }
    public string Status { get; set; }
    public int Type { get; set; }
    public bool Exists { get; set; }
}