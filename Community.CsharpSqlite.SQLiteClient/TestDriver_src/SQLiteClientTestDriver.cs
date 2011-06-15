﻿using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Community.CsharpSqlite;
using Community.CsharpSqlite.SQLiteClient;

namespace SQLiteClientTests
{
  public class SQLiteClientTestDriver
  {
    StringBuilder TempDirectory = new StringBuilder( "B:/TEMP/" );

    public void Test1()
    {
      Console.WriteLine( "Test1 Start." );

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
      IDbCommand cmd = con.CreateCommand();

      Console.WriteLine( "create table TEST_TABLE..." );
      cmd.CommandText = "CREATE TABLE TEST_TABLE ( COLA INTEGER, COLB TEXT, COLC DATETIME )";
      cmd.ExecuteNonQuery();

      Console.WriteLine( "insert row 1..." );
      cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (123,'ABC','2008-12-31 18:19:20' )";
      cmd.ExecuteNonQuery();

      Console.WriteLine( "insert row 2..." );
      cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (124,'DEF', '2009-11-16 13:35:36' )";
      cmd.ExecuteNonQuery();

      //Console.WriteLine("commit...");
      //cmd.CommandText = "COMMIT";
      //cmd.ExecuteNonQuery();

      Console.WriteLine( "SELECT data from TEST_TABLE..." );
      cmd.CommandText = "SELECT COLA, COLB, COLC FROM TEST_TABLE";
      IDataReader reader = cmd.ExecuteReader();
      int r = 0;
      Console.WriteLine( "Read the data..." );
      while ( reader.Read() )
      {
        Console.WriteLine( "  Row: {0}", r );
        int i = reader.GetInt32( reader.GetOrdinal( "COLA" ) );
        Console.WriteLine( "    COLA: {0}", i );

        string s = reader.GetString( reader.GetOrdinal( "COLB" ) );
        Console.WriteLine( "    COLB: {0}", s );

        DateTime dt = reader.GetDateTime( reader.GetOrdinal( "COLC" ) );
        Console.WriteLine( "    COLB: {0}", dt.ToString( "MM/dd/yyyy HH:mm:ss" ) );

        r++;
      }
      Console.WriteLine( "Rows retrieved: {0}", r );

//alxwest: DataTable & SqliteDataAdapter currently unavailable for Silverlight
#if !SQLITE_SILVERLIGHT 
      SqliteCommand command = new SqliteCommand( "PRAGMA table_info('TEST_TABLE')", con );
      DataTable dataTable = new DataTable();
      SqliteDataAdapter dataAdapter = new SqliteDataAdapter();
      dataAdapter.SelectCommand = command;
      dataAdapter.Fill( dataTable );
      DisplayDataTable( dataTable, "Columns" );
#endif

      Console.WriteLine( "Close and cleanup..." );
      con.Close();
      con = null;

      Console.WriteLine( "Test1 Done." );
    }
    public void Test2()
    {
      Console.WriteLine( "Test2 Start." );

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
      IDbCommand cmd = con.CreateCommand();

      Console.WriteLine( "create table TEST_TABLE..." );
      cmd.CommandText = "CREATE TABLE TBL ( ID NUMBER, NAME TEXT)";
      cmd.ExecuteNonQuery();

      Console.WriteLine( "insert row 1..." );
      cmd.CommandText = "INSERT INTO TBL ( ID, NAME ) VALUES (1, 'ONE' )";
      cmd.ExecuteNonQuery();

      Console.WriteLine( "insert row 2..." );
      cmd.CommandText = "INSERT INTO TBL ( ID, NAME ) VALUES (2, 'ä¸­æ–‡' )";
      cmd.ExecuteNonQuery();

      //Console.WriteLine("commit...");
      //cmd.CommandText = "COMMIT";
      //cmd.ExecuteNonQuery();

      Console.WriteLine( "SELECT data from TBL..." );
      cmd.CommandText = "SELECT id,NAME FROM tbl WHERE name = 'ä¸­æ–‡'";
      IDataReader reader = cmd.ExecuteReader();
      int r = 0;
      Console.WriteLine( "Read the data..." );
      while ( reader.Read() )
      {
        Console.WriteLine( "  Row: {0}", r );
        int i = reader.GetInt32( reader.GetOrdinal( "ID" ) );
        Console.WriteLine( "    ID: {0}", i );

        string s = reader.GetString( reader.GetOrdinal( "NAME" ) );
        Console.WriteLine( "    NAME: {0} = {1}", s, s == "ä¸­æ–‡" );
        r++;
      }
      Console.WriteLine( "Rows retrieved: {0}", r );

      //alxwest: DataTable & SqliteDataAdapter currently unavailable for Silverlight
#if !SQLITE_SILVERLIGHT 
      SqliteCommand command = new SqliteCommand( "PRAGMA table_info('TEST_TABLE')", con );
      DataTable dataTable = new DataTable();
      SqliteDataAdapter dataAdapter = new SqliteDataAdapter();
      dataAdapter.SelectCommand = command;
      dataAdapter.Fill( dataTable );
      DisplayDataTable( dataTable, "Columns" );
#endif

      Console.WriteLine( "Close and cleanup..." );
      con.Close();
      con = null;

      Console.WriteLine( "Test1 Done." );
    }
    public void Test3()
    {
      Console.WriteLine( "Test3 (Date Paramaters) Start." );

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
      IDbCommand cmd = con.CreateCommand();

      Console.WriteLine( "create table TEST_TABLE..." );
      cmd.CommandText = "CREATE TABLE TBL ( ID NUMBER, DATE_TEXT REAL)";
      cmd.ExecuteNonQuery();

      Console.WriteLine( "insert ..." );
      cmd.CommandText = "INSERT INTO TBL  ( ID, DATE_TEXT) VALUES ( 1,  @DATETEXT)";
      cmd.Parameters.Add(
        new SqliteParameter
        {
          ParameterName = "@DATETEXT",
          Value = DateTime.Now
        }
        );

      cmd.ExecuteNonQuery();


      Console.WriteLine( "SELECT data from TBL..." );
      cmd.CommandText = "SELECT * FROM tbl";
      IDataReader reader = cmd.ExecuteReader();
      int r = 0;
      Console.WriteLine( "Read the data..." );
      while ( reader.Read() )
      {
        Console.WriteLine( "  Row: {0}", r );
        int i = reader.GetInt32( reader.GetOrdinal( "ID" ) );
        Console.WriteLine( "    ID: {0}", i );

        string s = reader.GetString( reader.GetOrdinal( "DATE_TEXT" ) );
        Console.WriteLine( "    DATE_TEXT: {0}", s );
        r++;
      }
      Console.WriteLine( "Rows retrieved: {0}", r );


      Console.WriteLine( "Close and cleanup..." );
      con.Close();
      con = null;

      Console.WriteLine( "Test3 Done." );
    }

    //nSoftware code for Threading
    string connstring_T4;
    public void Test4()
    {
      string dbFilename = "threading_t4.db";
      if ( File.Exists( dbFilename ) )
        File.Delete( dbFilename );
      connstring_T4 = @"Version=3,busy_timeout=100,uri=file:" + dbFilename;

      Setup_T4();
      InsertSameTable_T4(); //concurrent inserts
      SelectorWrite_T4(); //concurrent selects and inserts
      Console.WriteLine( "Testing for Threading done. Press enter to continue" );
      Console.In.Read();
    }
    private void SelectorWrite_T4()
    {
      //concurrent reads/writes in the same table, if there were only Selects it would be preferable for the sqlite engine not to lock internally.
      for ( int i = 0; i < 10; i++ )
      {
        Console.WriteLine( "SELECT/INSERT ON Thread {0}", i );
        Thread worker = new Thread( () =>
        {
          // Cannot use value of i, since it exceeds the scope of this thread and will be 
          // reused by multiple threads
          int aValue = 100 + Thread.CurrentThread.ManagedThreadId;
          int op = aValue % 2;

          SqliteConnection con = new SqliteConnection();
          con.ConnectionString = connstring_T4;
          con.Open();
          IDbCommand cmd = con.CreateCommand();
          cmd = con.CreateCommand();
          if ( op == 0 )
          {
            cmd.CommandText = String.Format( "Select * FROM ATABLE" );
            cmd.ExecuteReader();
          }
          else
          {
            cmd.CommandText = String.Format( "INSERT INTO ATABLE ( A, B, C ) VALUES ({0},'threader', '1' )", aValue );
            Console.WriteLine( cmd.CommandText );
            cmd.ExecuteNonQuery();
          }
        } );
        worker.Start();
      }
    }
    //we need concurrency support on a table level inside of the database file.
    private void InsertSameTable_T4()
    {
      for ( int i = 0; i < 10; i++ )
      {
        Console.WriteLine( "INSERTING ON Thread {0}", i );
        Thread worker = new Thread( () =>
        {
          // Cannot use value of i, since it exceeds the scope of this thread and will be 
          // reused by multiple threads

          int aValue = Thread.CurrentThread.ManagedThreadId;

          SqliteConnection con = new SqliteConnection();
          con.ConnectionString = connstring_T4;
          con.Open();
          IDbCommand cmd = con.CreateCommand();
          cmd = con.CreateCommand();
          cmd.CommandText = String.Format( "INSERT INTO ATABLE ( A, B, C ) VALUES ({0},'threader', '1' )", aValue );
          Console.WriteLine( cmd.CommandText );
          cmd.ExecuteNonQuery();
        }
          );
        worker.Start();
      }
    }

    private void Setup_T4()
    {
      SqliteConnection con = new SqliteConnection();
      con.ConnectionString = connstring_T4;
      con.Open();
      IDbCommand cmd = con.CreateCommand();
      cmd = con.CreateCommand();
      cmd.CommandText = "CREATE TABLE IF NOT EXISTS ATABLE(A integer primary key , B varchar (50), C integer)";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "CREATE TABLE IF NOT EXISTS BTABLE(A integer primary key , B varchar (50), C integer)";
      cmd.ExecuteNonQuery();
      cmd.CommandText = String.Format( "INSERT INTO BTABLE ( A, B, C ) VALUES (6,'threader', '1' )" );
      cmd.ExecuteNonQuery();
    }

    //nSoftware code for Threading
    string connstring_T5;
    public void Test5()
    {
      string dbFilename = "threading_t5.db";
      if ( File.Exists( dbFilename ) )
        File.Delete( dbFilename );
      connstring_T5 = @"Version=3,busy_timeout=2500,uri=file:" + dbFilename;

      Setup_T5();
      MultiInsertsSameThread_T5(); //concurrent inserts
      Console.WriteLine( "Threads are running..." );
      Console.In.Read();
    }

    private void MultiInsertsSameThread_T5()
    {
      for ( int i = 0; i < 10; i++ )
      {
        //Console.WriteLine( "SELECT/INSERT ON Thread {0}", i );
        Thread worker = new Thread( () =>
        {
          string commandt = String.Empty;
          try
          {
            // Cannot use value of i, since it exceeds the scope of this thread and will be 
            // reused by multiple threads
            int aValue = 100 + Thread.CurrentThread.ManagedThreadId;
            int op = aValue % 2;

            SqliteConnection con = new SqliteConnection();
            con.ConnectionString = connstring_T5;
            con.Open();
            IDbCommand cmd = con.CreateCommand();
            cmd = con.CreateCommand();
            if ( op == 0 )
            {
              for ( int j = 0; j < 1000; j++ )
              {
                int rows;
                int retry = 0;
                cmd.CommandText = String.Format( "INSERT INTO BTABLE ( A, B, C ) VALUES ({0},'threader', '1' )", ( aValue * 10000 ) + j );
                commandt = cmd.CommandText;
                do
                {
                  rows = cmd.ExecuteNonQuery();
                  if ( rows == 0 )
                  {
                    retry += 1; // Insert Failed
                    Console.WriteLine( cmd.CommandText );
                    Console.WriteLine( "retry {0}", retry );
                    Console.WriteLine( ( (SqliteCommand)cmd ).GetLastError() );
                  }
                } while ( rows == 0 && retry < 5 );
              }
            }
            else
            {
              cmd.CommandText = String.Format( "Select * FROM ATABLE" );
              commandt = cmd.CommandText;
              cmd.ExecuteReader();
            }
          }
          catch ( Exception ex )
          {
            Console.WriteLine( String.Format( "Command {0} threw exception {1}", commandt, ex.Message ) );
          }
        } );

        worker.Start();
      }
    }

    private void Setup_T5()
    {
      SqliteConnection con = new SqliteConnection();
      con.ConnectionString = connstring_T5;
      con.Open();
      IDbCommand cmd = con.CreateCommand();
      cmd = con.CreateCommand();
      cmd.CommandText = "CREATE TABLE IF NOT EXISTS ATABLE(A integer primary key , B varchar (50), C integer)";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "CREATE TABLE IF NOT EXISTS BTABLE(A integer primary key , B varchar (50), C integer)";
      cmd.ExecuteNonQuery();
      cmd.CommandText = String.Format( "INSERT INTO BTABLE ( A, B, C ) VALUES (6,'threader', '1' )" );
      cmd.ExecuteNonQuery();
    }

    //nSoftware code for Threading & Transactions
    static string connstring_T6;
    public void Test6()
    {
      string dbFilename = "threading_t6.db";
      if ( File.Exists( dbFilename ) )
        File.Delete( dbFilename );
      connstring_T6 = @"Version=3,busy_timeout=2000,uri=file:" + dbFilename;

      Setup_T6();
      MultiInsertsTransactionsSameThread_T6(); //concurrent inserts
      Console.WriteLine( "Threads are running..." );
      Console.In.Read();
    }

    static Random rnd = new Random();
    private void MultiInsertsTransactionsSameThread_T6()
    {
      for ( int i = 0; i < 20; i++ )
      {
        Thread.Sleep( rnd.Next( 100, 1000 ) );
        Console.WriteLine( "Launching Thread {0}", i );
        Thread worker = new Thread( SQLiteClientTestDriver.T6_ThreadStart );
        worker.Start( i );
      }
    }
    private static void T6_ThreadStart( object data )
    {
      string commandt = String.Empty;
      int i = (int)data;
      try
      {
        int aValue = 100 + i;
        int op = aValue % 2;

        SqliteConnection con = new SqliteConnection();
        con.ConnectionString = connstring_T6;
        con.Open();
        IDbCommand cmd = con.CreateCommand();
        cmd = con.CreateCommand();
        if ( op == 0 )
        {
          SqliteTransaction trans = (SqliteTransaction)con.BeginTransaction();
          for ( int j = 0; j < 5000; j++ )
          {
            int rows;
            int retry = 0;
            cmd.CommandText = String.Format( "INSERT INTO BTABLE ( A, B, C ) VALUES ({0},'threader', '1' )", ( aValue * 10000 ) + j );
            commandt = cmd.CommandText;
            do
            {
              rows = cmd.ExecuteNonQuery();
              if ( rows == 0 )
              {
                retry += 1; // Insert Failed
                Console.WriteLine( "retry {0}:{1}:{2}", retry, ( (SqliteCommand)cmd ).GetLastError(), cmd.CommandText );
                Thread.Sleep( rnd.Next( 50, 1000 ) );
              }
            } while ( rows == 0 && retry < 10 );
          }
          trans.Commit();
        }
        else
        {
          cmd.CommandText = String.Format( "Select * FROM ATABLE" );
          commandt = cmd.CommandText;
          cmd.ExecuteReader();
        }
      }
      catch ( Exception ex )
      {
        Console.WriteLine( String.Format( "Command {0} threw exception {1}", commandt, ex.Message ) );
      }
    }
    private void Setup_T6()
    {
      SqliteConnection con = new SqliteConnection();
      con.ConnectionString = connstring_T6;
      con.Open();
      IDbCommand cmd = con.CreateCommand();
      cmd = con.CreateCommand();
      cmd.CommandText = "CREATE TABLE IF NOT EXISTS ATABLE(A integer primary key , B varchar (50), C integer)";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "CREATE TABLE IF NOT EXISTS BTABLE(A integer primary key , B varchar (50), C integer)";
      cmd.ExecuteNonQuery();
      cmd.CommandText = String.Format( "INSERT INTO BTABLE ( A, B, C ) VALUES (6,'threader', '1' )" );
      cmd.ExecuteNonQuery();
    }
    //nSoftware code for Threading
    string connstring_T7;
    public void Test7()
    {
      string dbFilename = "threading_t7.db";
      if ( File.Exists( dbFilename ) )
        File.Delete( dbFilename );
      connstring_T7 = @"Version=3,busy_timeout=1000,uri=file:" + dbFilename;

      MultiInsertsSameThread_T7(); //concurrent table creation
      Console.WriteLine( "Threads are running..." );
      Console.In.Read();
    }

    private void MultiInsertsSameThread_T7()
    {
      List<Thread> lthread = new List<Thread>();
      for ( int i = 0; i < 70; i++ )
      {
        Thread.Sleep( rnd.Next( 100, 1000 ) );
        Console.WriteLine( "Launching Thread {0}", i );
        Thread worker = new Thread( this.T7_ThreadStart );
        lthread.Add( worker );
        worker.Start( i );
      }
      bool alldone = false;

      while ( !alldone )
      {
        alldone = true;
        for ( int i = 0; i < lthread.Count; i++ )
        {
          if ( lthread[i].ThreadState == ThreadState.Running )
            alldone = false;
          Thread.Sleep( 100 );
        }
      }
      Console.WriteLine( "finished" );
    }

    private void T7_ThreadStart( object iSequence )
    {
      int aValue = (int)iSequence * 1000;
      int op = aValue % 2;
      int rows = 0;

      SqliteConnection con = new SqliteConnection();
      con.ConnectionString = connstring_T7;
      con.Open();
      IDbCommand cmd = con.CreateCommand();
      cmd = con.CreateCommand();
      string commandt = String.Format( "CREATE TABLE IF NOT EXISTS ATABLE{0}(A integer primary key , B varchar (50), C integer, D varchar (500))", aValue );
      cmd.CommandText = commandt;
      try
      {
        rows = cmd.ExecuteNonQuery();
        Console.WriteLine( "Created table: ATABLE" + aValue );
      }
      catch ( Exception ex )
      {
        Console.WriteLine( String.Format( "Command {0} threw exception {1}", commandt, ex.Message ) );
      }
    }

    private void Setup_T7()
    {
      SqliteConnection con = new SqliteConnection();
      con.ConnectionString = connstring_T7;
      con.Open();
      IDbCommand cmd = con.CreateCommand();
      cmd = con.CreateCommand();
      cmd.CommandText = "CREATE TABLE IF NOT EXISTS ATABLE(A integer primary key , B varchar (50), C integer)";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "CREATE TABLE IF NOT EXISTS BTABLE(A integer primary key , B varchar (50), C integer)";
      cmd.ExecuteNonQuery();
      cmd.CommandText = String.Format( "INSERT INTO BTABLE ( A, B, C ) VALUES (6,'threader', '1' )" );
      cmd.ExecuteNonQuery();
    }


    public void Issue_65()
    {
      //alxwest: causes error "Unable to open database" as TempDirectory.ToString() is set to "B:/TEMP/"
      //string datasource = "file://" + TempDirectory.ToString() + "myBigDb.s3db";
      string datasource = "file://" + "myBigDb.s3db";

      using ( IDbConnection conn = new SqliteConnection( "uri=" + datasource ) )
      {
        long targetFileSize = (long)Math.Pow( 2, 32 ) - 1;
        int rowLength = 1024; // 2^10

        long loopCount = (int)( targetFileSize / rowLength ) + 10000;

        char[] chars = new char[rowLength];
        for ( int i = 0; i < rowLength; i++ )
        {
          chars[i] = 'A';
        }

        string row = new string( chars );

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
          for ( int i = 0; i < loopCount; i++ )
          {
            cmd.ExecuteNonQuery();
          }
        }
        catch 
        {
          Console.WriteLine( ( (SqliteCommand)cmd ).GetLastError() );
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
      IDbCommand cmd = con.CreateCommand();

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

    //Multi thread execution special command or ddl command results in exception..
    public void Issue_86()
    {
      AppDomain.CurrentDomain.UnhandledException +=
          ( sender, eventArgs ) =>
          {
            Console.WriteLine( eventArgs.ExceptionObject );
          };
      int flags = Sqlite3.SQLITE_OPEN_NOMUTEX | Sqlite3.SQLITE_OPEN_READWRITE | Sqlite3.SQLITE_OPEN_CREATE;
      for ( int i = 0; i < 10; i++ )
      {
        Console.WriteLine( "Running Thread {0}", i );
        var t = new Thread(
          () =>
          {
            string dbFilename = string.Format( "db{0}.sqlite", Thread.CurrentThread.ManagedThreadId );
            if ( File.Exists( dbFilename ) )
              File.Delete( dbFilename );
            Console.WriteLine( "Using Database {0}", dbFilename );
            Sqlite3.sqlite3 db = null;
            Sqlite3.sqlite3_open_v2( dbFilename, ref db, flags, null );
            var command = string.Format( "create table [t{0}] (id, name, amount)", Thread.CurrentThread.ManagedThreadId );
            ExecuteCommand( db, command );
            Sqlite3.sqlite3_close( db );
          } );
        t.Start();
      }
    }

    private static void ExecuteCommand( Sqlite3.sqlite3 db, string command )
    {
      int rc;
      Sqlite3.Vdbe vm = null;
      if ( Sqlite3.sqlite3_prepare_v2( db, command, command.Length, ref vm, 0 ) != Sqlite3.SQLITE_OK )
      {
        throw new InvalidOperationException( string.Format( "Query failed ({0}), message: {1}.", db.errCode, Sqlite3.sqlite3_errmsg( db ) ) );
      }
      rc = Sqlite3.sqlite3_step( vm );
      if ( rc != Sqlite3.SQLITE_DONE && rc != Sqlite3.SQLITE_ROW )
      {
        throw new InvalidOperationException( string.Format( "Query failed ({0}), message: {1}.", db.errCode, Sqlite3.sqlite3_errmsg( db ) ) );
      }
      Sqlite3.sqlite3_finalize( vm );
    }

//alxwest: DataTable & SqliteDataAdapter currently unavailable for Silverlight
#if !SQLITE_SILVERLIGHT 
    public void DisplayDataTable( DataTable table, string name )
    {
      Console.WriteLine( "Display DataTable: {0}", name );
      int r = 0;
      foreach ( DataRow row in table.Rows )
      {
        Console.WriteLine( "Row {0}", r );
        int c = 0;
        foreach ( DataColumn col in table.Columns )
        {

          Console.WriteLine( "   Col {0}: {1} {2}", c, col.ColumnName, col.DataType );
          Console.WriteLine( "       Value: {0}", row[col] );
          c++;
        }
        r++;
      }
      Console.WriteLine( "Rows in data table: {0}", r );

    }
#endif


    public void Issue_119()
    {
      Console.WriteLine( "Test Start." );

      Console.WriteLine( "Create connection..." );
      SqliteConnection con = new SqliteConnection();

      string dbFilename = @"=SqliteTest3=.db";
      string cs = string.Format( "Version=3,uri=file:{0}", dbFilename );

      Console.WriteLine( "Set connection String: {0}", cs );

      if ( File.Exists( dbFilename ) )
        File.Delete( dbFilename );

      con.ConnectionString = cs;

      Console.WriteLine( "Open database..." );
      con.Open();

      Console.WriteLine( "create command..." );
      IDbCommand cmd = con.CreateCommand();

      Console.WriteLine( "create table TEST_TABLE..." );
      cmd.CommandText = "CREATE TABLE TEST_TABLE ( COLA INTEGER, COLB TEXT, COLC DATETIME )";
      cmd.ExecuteNonQuery();

      Console.WriteLine( "insert row 1..." );
      cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (123,'ABC','2008-12-31 18:19:20' )";
      cmd.ExecuteNonQuery();

      Console.WriteLine( "insert row 2..." );
      cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (124,'DEF', '2009-11-16 13:35:36' )";
      cmd.ExecuteNonQuery();

      Console.WriteLine( "SELECT data from TEST_TABLE..." );
      cmd.CommandText = "SELECT RowID, COLA, COLB, COLC FROM TEST_TABLE";
      IDataReader reader = cmd.ExecuteReader();
      int r = 0;
      Console.WriteLine( "Read the data..." );
      while ( reader.Read() )
      {
        Console.WriteLine( "  Row: {0}", r );
        int rowid = reader.GetInt32( reader.GetOrdinal( "RowID" ) );
        Console.WriteLine( "    RowID: {0}", rowid );

        int i = reader.GetInt32( reader.GetOrdinal( "COLA" ) );
        Console.WriteLine( "    COLA: {0}", i );

        string s = reader.GetString( reader.GetOrdinal( "COLB" ) );
        Console.WriteLine( "    COLB: {0}", s );

        DateTime dt = reader.GetDateTime( reader.GetOrdinal( "COLC" ) );
        Console.WriteLine( "    COLB: {0}", dt.ToString( "MM/dd/yyyy HH:mm:ss" ) );

        r++;
      }

      Console.WriteLine( "Close and cleanup..." );
      con.Close();
      con = null;

      Console.WriteLine( "Test Done." );
    }
    
    public static int Main( string[] args )
    {
      SQLiteClientTestDriver tests = new SQLiteClientTestDriver();

      int Test = 119;
      switch ( Test )
      {
        case 1:
          tests.Test1();
          break;
        case 2:
          tests.Test2();
          break;
        case 3:
          tests.Test3();
          break;
        case 4:
          tests.Test4();
          break;
        case 5:
          tests.Test5();
          break;
        case 6:
          tests.Test6();
          break;
        case 7:
          tests.Test7();
          break;
        case 65:
          tests.Issue_65();
          break;
        case 76:
          tests.Issue_76();
          break;
        case 86:
          tests.Issue_86();
          break;
        case 119:
          tests.Issue_119();
          break;
      }
      Console.WriteLine( "Press Enter to Continue" );
      //Console.ReadKey();
      tests = null;

      return 0;
    }
  }
}
