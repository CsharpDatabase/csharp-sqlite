using System;
using System.Data;
using System.IO;
using Community.CsharpSqlite.SQLiteClient;
using System.Text;

namespace SQLiteClientTests
{
  public class SQLiteClientTestDriver
  {
    StringBuilder TempDirectory = new StringBuilder("B:/TEMP/");

    public void Test1()
    {
      Console.WriteLine("Test1 Start.");

      Console.WriteLine("Create connection...");
      SqliteConnection con = new SqliteConnection();

      string dbFilename = @"SqliteTest3.db";
      string cs = string.Format("Version=3,uri=file:{0}", dbFilename);

      Console.WriteLine("Set connection String: {0}", cs);

      if (File.Exists(dbFilename))
        File.Delete(dbFilename);

      con.ConnectionString = cs;

      Console.WriteLine("Open database...");
      con.Open();

      Console.WriteLine("create command...");
      SqliteCommand cmd = con.CreateCommand();

      Console.WriteLine("create table TEST_TABLE...");
      cmd.CommandText = "CREATE TABLE TEST_TABLE ( COLA INTEGER, COLB TEXT, COLC DATETIME )";
      cmd.ExecuteNonQuery();

      Console.WriteLine("insert row 1...");
      cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (123,'ABC','2008-12-31 18:19:20' )";
      cmd.ExecuteNonQuery();

      Console.WriteLine("insert row 2...");
      cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (124,'DEF', '2009-11-16 13:35:36' )";
      cmd.ExecuteNonQuery();

      //Console.WriteLine("commit...");
      //cmd.CommandText = "COMMIT";
      //cmd.ExecuteNonQuery();

      Console.WriteLine("SELECT data from TEST_TABLE...");
      cmd.CommandText = "SELECT COLA, COLB, COLC FROM TEST_TABLE";
      SqliteDataReader reader = cmd.ExecuteReader();
      int r = 0;
      Console.WriteLine("Read the data...");
      while (reader.Read())
      {
        Console.WriteLine("  Row: {0}", r);
        int i = reader.GetInt32(reader.GetOrdinal("COLA"));
        Console.WriteLine("    COLA: {0}", i);

        string s = reader.GetString(reader.GetOrdinal("COLB"));
        Console.WriteLine("    COLB: {0}", s);

        DateTime dt = reader.GetDateTime(reader.GetOrdinal("COLC"));
        Console.WriteLine("    COLB: {0}", dt.ToString("MM/dd/yyyy HH:mm:ss"));

        r++;
      }
      Console.WriteLine("Rows retrieved: {0}", r);


      SqliteCommand command = new SqliteCommand("PRAGMA table_info('TEST_TABLE')", con);
      DataTable dataTable = new DataTable();
      SqliteDataAdapter dataAdapter = new SqliteDataAdapter();
      dataAdapter.SelectCommand = command;
      dataAdapter.Fill(dataTable);
      DisplayDataTable(dataTable, "Columns");


      Console.WriteLine("Close and cleanup...");
      con.Close();
      con = null;

      Console.WriteLine("Test1 Done.");
    }
    public void Test2()
    {
      Console.WriteLine("Test2 Start.");

      Console.WriteLine("Create connection...");
      SqliteConnection con = new SqliteConnection();

      string dbFilename = @"SqliteTest3.db";
      string cs = string.Format("Version=3,uri=file:{0}", dbFilename);

      Console.WriteLine("Set connection String: {0}", cs);

      if (File.Exists(dbFilename))
        File.Delete(dbFilename);

      con.ConnectionString = cs;

      Console.WriteLine("Open database...");
      con.Open();

      Console.WriteLine("create command...");
      SqliteCommand cmd = con.CreateCommand();

      Console.WriteLine("create table TEST_TABLE...");
      cmd.CommandText = "CREATE TABLE TBL ( ID NUMBER, NAME TEXT)";
      cmd.ExecuteNonQuery();

      Console.WriteLine("insert row 1...");
      cmd.CommandText = "INSERT INTO TBL ( ID, NAME ) VALUES (1, 'ONE' )";
      cmd.ExecuteNonQuery();

      Console.WriteLine("insert row 2...");
      cmd.CommandText = "INSERT INTO TBL ( ID, NAME ) VALUES (2, '中文' )";
      cmd.ExecuteNonQuery();

      //Console.WriteLine("commit...");
      //cmd.CommandText = "COMMIT";
      //cmd.ExecuteNonQuery();

      Console.WriteLine("SELECT data from TBL...");
      cmd.CommandText = "SELECT id,NAME FROM tbl WHERE name = '中文'";
      SqliteDataReader reader = cmd.ExecuteReader();
      int r = 0;
      Console.WriteLine("Read the data...");
      while (reader.Read())
      {
        Console.WriteLine("  Row: {0}", r);
        int i = reader.GetInt32(reader.GetOrdinal("ID"));
        Console.WriteLine("    ID: {0}", i);

        string s = reader.GetString(reader.GetOrdinal("NAME"));
        Console.WriteLine("    NAME: {0} = {1}", s, s == "中文");
        r++;
      }
      Console.WriteLine("Rows retrieved: {0}", r);


      SqliteCommand command = new SqliteCommand("PRAGMA table_info('TEST_TABLE')", con);
      DataTable dataTable = new DataTable();
      SqliteDataAdapter dataAdapter = new SqliteDataAdapter();
      dataAdapter.SelectCommand = command;
      dataAdapter.Fill(dataTable);
      DisplayDataTable(dataTable, "Columns");


      Console.WriteLine("Close and cleanup...");
      con.Close();
      con = null;

      Console.WriteLine("Test1 Done.");
    }
    public void Test3()
    {
      Console.WriteLine("Test3 (Date Paramaters) Start.");

      Console.WriteLine("Create connection...");
      SqliteConnection con = new SqliteConnection();

      string dbFilename = @"SqliteTest3.db";
      string cs = string.Format("Version=3,uri=file:{0}", dbFilename);

      Console.WriteLine("Set connection String: {0}", cs);

      if (File.Exists(dbFilename))
        File.Delete(dbFilename);

      con.ConnectionString = cs;

      Console.WriteLine("Open database...");
      con.Open();

      Console.WriteLine("create command...");
      SqliteCommand cmd = con.CreateCommand();

      Console.WriteLine("create table TEST_TABLE...");
      cmd.CommandText = "CREATE TABLE TBL ( ID NUMBER, DATE_TEXT REAL)";
      cmd.ExecuteNonQuery();

      Console.WriteLine("insert ...");
      cmd.CommandText = "INSERT INTO TBL  ( ID, DATE_TEXT) VALUES ( 1,  @DATETEXT)";
      cmd.Parameters.Add(
        new SQLiteParameter      {
        ParameterName =          "@DATETEXT",        Value = DateTime.Now      }
        );

      cmd.ExecuteNonQuery();


      Console.WriteLine("SELECT data from TBL...");
      cmd.CommandText = "SELECT * FROM tbl";
      SqliteDataReader reader = cmd.ExecuteReader();
      int r = 0;
      Console.WriteLine("Read the data...");
      while (reader.Read())
      {
        Console.WriteLine("  Row: {0}", r);
        int i = reader.GetInt32(reader.GetOrdinal("ID"));
        Console.WriteLine("    ID: {0}", i);

        string s = reader.GetString(reader.GetOrdinal("DATE_TEXT"));
        Console.WriteLine("    DATE_TEXT: {0}", s);
        r++;
      }
      Console.WriteLine("Rows retrieved: {0}", r);


      Console.WriteLine("Close and cleanup...");
      con.Close();
      con = null;

      Console.WriteLine("Test3 Done.");
    }
    public void Issue_65()
    {
      string datasource = "file://" + TempDirectory.ToString() + "myBigDb.s3db";

      using (IDbConnection conn = new SqliteConnection("uri=" + datasource))
      {
        long targetFileSize = (long)Math.Pow(2, 32) - 1;
        int rowLength = 1024; // 2^10

        long loopCount = (int)(targetFileSize / rowLength) + 10000;

        char[] chars = new char[rowLength];
        for (int i = 0; i < rowLength; i++)
        {
          chars[i] = 'A';
        }

        string row = new string(chars);

        conn.Open();
        IDbCommand cmd = conn.CreateCommand();

        try
        {
          cmd.CommandText = "PRAGMA cache_size = 16000; PRAGMA synchronous = OFF; PRAGMA journal_mode = MEMORY;";
          cmd.ExecuteNonQuery();

          cmd.CommandText = "drop table if exists [MyTable]";
          cmd.ExecuteNonQuery();

          cmd.CommandText = "create table [MyTable] ([MyField] varchar(" + rowLength + ") null)";
          cmd.ExecuteNonQuery();

          cmd.CommandText = "insert into [MyTable] ([MyField]) VALUES ('" + row + "')";
          for (int i = 0; i < loopCount; i++)
          {
            cmd.ExecuteNonQuery();
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine(((SqliteCommand)cmd).GetLastError());
        }
        finally
        {
          cmd.Cancel();
          conn.Close();
          conn.Dispose();
        }
      }

    }

    //Issue 76 Encryption is not implemented in C#SQLite client connection and command objects 
    public void Issue_76()
    {
      Console.WriteLine( "Test for Issue_76 Start." );

      Console.WriteLine( "Create connection..." );
      SqliteConnection con = new SqliteConnection();

      string dbFilename = @"SqliteTest3.db";
      string cs = string.Format( "Version=3,uri=file:{0}", dbFilename );

      Console.WriteLine( "Set connection String: {0}", cs );

      if ( File.Exists( dbFilename ) )
        File.Delete( dbFilename );

      con.ConnectionString = cs;

      Console.WriteLine( "Open database..." );
      con.Open();

      Console.WriteLine( "create command..." );
      SqliteCommand cmd = con.CreateCommand();

      cmd.CommandText = "pragma hexkey='0x73656372657470617373776F72640f11'";
      Console.WriteLine( cmd.CommandText );
      cmd.ExecuteNonQuery();

      cmd.CommandText = "create table a (b); insert into a values ('row 1');select * from a;";
      Console.WriteLine( cmd.CommandText );
      Console.WriteLine( "Result {0}", cmd.ExecuteScalar() );

      Console.WriteLine( "Close & Reopen Connection" );
      con.Close();
      con.Open();

      cmd.CommandText = "select * from a;";
      Console.WriteLine( cmd.CommandText );
      Console.WriteLine( "Result {0}", cmd.ExecuteScalar() );

      Console.WriteLine( "Close & Reopen Connection" );
      con.Close();
      con.Open();
      cmd.CommandText = "pragma hexkey='0x73656372657470617373776F72640f11'";
      Console.WriteLine( cmd.CommandText );
      cmd.ExecuteNonQuery();

      cmd.CommandText = "select * from a;";
      Console.WriteLine( cmd.CommandText );
      Console.WriteLine( "Result {0}", cmd.ExecuteScalar() );

      Console.WriteLine( "Close & Reopen Connection with password" );
      con.Close();

      con.ConnectionString = cs + ",Password=0x73656372657470617373776F72640f11";
      con.Open();
      cmd.CommandText = "select * from a;";
      Console.WriteLine( cmd.CommandText );
      Console.WriteLine( "Result {0}", cmd.ExecuteScalar() );
      
      con = null;

      Console.WriteLine( "Issue_76 Done." );
    }
    public void DisplayDataTable( DataTable table, string name )
    {
      Console.WriteLine("Display DataTable: {0}", name);
      int r = 0;
      foreach (DataRow row in table.Rows)
      {
        Console.WriteLine("Row {0}", r);
        int c = 0;
        foreach (DataColumn col in table.Columns)
        {

          Console.WriteLine("   Col {0}: {1} {2}", c, col.ColumnName, col.DataType);
          Console.WriteLine("       Value: {0}", row[col]);
          c++;
        }
        r++;
      }
      Console.WriteLine("Rows in data table: {0}", r);

    }

    public static int Main(string[] args)
    {
      SQLiteClientTestDriver tests = new SQLiteClientTestDriver();

      int Test =76;
      switch (Test)
      {
        case 1:
          tests.Test1(); break;
        case 2:
          tests.Test2(); break;
        case 3:
          tests.Test3(); break;
        case 65:
          tests.Issue_65(); break;
        case 76:
          tests.Issue_76();
          break;
      }
      Console.WriteLine("Press Enter to Continue");
      Console.ReadKey();
      tests = null;

      return 0;
    }
  }
}
