using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;

class Program
{
    static void Main()
    {
        string conStringDBA = "User Id=SYSDBA;Password=masterkey;Data Source=192.168.2.125/sprutora;";
        string sourceFolderPath = "C:\\Users\\Gansor\\Desktop\\1\\";
        //string destinationFolderPath = "C:\\Users\\Gansor\\Desktop\\1_1\\";
        string[] files = Directory.GetFiles(sourceFolderPath);

        using (OracleConnection con = new OracleConnection(conStringDBA))
        {
            con.Open();
            Console.WriteLine("Successfully connected to Oracle Database");

            foreach (string filePath in files)
            {
                // Get the last key in the SPR_SPEECH_TABLE
                int key;

                using (OracleCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText = "SELECT S_INCKEY FROM test.SPR_SPEECH_TABLE WHERE S_INCKEY=(SELECT max(S_INCKEY) FROM test.SPR_SPEECH_TABLE)";
                    key = Convert.ToInt32(cmd.ExecuteScalar());
                    key++;
                }

                // Insert records into SPR_SPEECH_TABLE and SPR_SP_DATA_1_TABLE
                using (OracleTransaction transaction = con.BeginTransaction())
                {
                    if (File.Exists(filePath))
                    {
                        byte[] fileData = File.ReadAllBytes(filePath);
                        int duration = (int)(fileData.Length / 8000);
                        string durationString = string.Format("{0:D2}:{1:D2}:{2:D2}", duration / 3600, (duration % 3600) / 60, duration % 60);

                        using (OracleCommand insertCommand = con.CreateCommand())
                        {
                            insertCommand.Transaction = transaction;
                            insertCommand.CommandText = "INSERT INTO test.SPR_SPEECH_TABLE (S_INCKEY, S_TYPE, S_PRELOOKED, S_DATETIME, S_DEVICEID/*, S_DURATION*/) " +
                                                        "VALUES (:S_INCKEY, :S_TYPE, :S_PRELOOKED, :S_DATETIME, :S_DEVICEID/*, :S_DURATION*/)";

                            insertCommand.Parameters.Add(":S_INCKEY", OracleDbType.Int32).Value = key;
                            insertCommand.Parameters.Add(":S_TYPE", OracleDbType.Int32).Value = 0;
                            insertCommand.Parameters.Add(":S_PRELOOKED", OracleDbType.Int32).Value = 0;
                            insertCommand.Parameters.Add(":S_DATETIME", OracleDbType.Date).Value = DateTime.Now;
                            insertCommand.Parameters.Add(":S_DEVICEID", OracleDbType.Varchar2).Value = "APK_SUPERACCESS";
                            //insertCommand.Parameters.Add(":S_DURATION", OracleDbType.Varchar2).Value = durationString;

                            insertCommand.ExecuteNonQuery();
                        }

                        using (OracleCommand insertCommand = con.CreateCommand())
                        {
                            insertCommand.Transaction = transaction;
                            insertCommand.CommandText = "INSERT INTO test.SPR_SP_DATA_1_TABLE (S_INCKEY, S_ORDER, S_FSPEECH, S_RECORDTYPE) " +
                                                        "VALUES (:S_INCKEY, 1, :S_FSPEECH, 'PCMA')";

                            insertCommand.Parameters.Add(":S_INCKEY", OracleDbType.Int32).Value = key;
                            insertCommand.Parameters.Add(":S_FSPEECH", OracleDbType.Blob).Value = fileData;

                            insertCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }

            Console.WriteLine("Data insertion completed successfully");
            con.Close();
        }
    }
}