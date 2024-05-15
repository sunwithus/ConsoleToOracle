using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Oracle.ManagedDataAccess.Client;

class Program
{

    static void Main()
    {
        string ffmpegExePath = @"C:\dotnet\ffmpeg\ffmpeg.exe"; // Путь к исполняемому файлу FFmpeg
        string path = @"C:\temp\2";

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

                        DirectoryInfo dirInfo = new DirectoryInfo(path);
                        if (!dirInfo.Exists)
                        {
                            dirInfo.Create();
                        }
                        var trustedFileName = Path.GetRandomFileName();
                        var trustedFilePath = Path.Combine(path, trustedFileName);

                        string outputFilePath = trustedFilePath + ".wav"; // путь к выходному файлу

                        // Create the command string for FFmpeg
                        //###########################################################
                        StringBuilder sbffmpeg = new StringBuilder();
                        sbffmpeg.Append($"{ffmpegExePath} -i ");
                        sbffmpeg.Append(filePath);
                        //sbffmpeg.Append(" -codec:a pcm_s16le -b:a 128k -ac 1 -ar 8000 "); // формат для Whisper
                        sbffmpeg.Append(" -codec:a pcm_alaw -b:a 128k -ac 1 -ar 8000 "); // формат для Whisper
                        sbffmpeg.Append(outputFilePath);

                        // Run FFmpeg with cmd.exe
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            CreateNoWindow = true
                        };

                        using (Process process = Process.Start(startInfo))
                        {
                            using (StreamWriter sw = process.StandardInput)
                            {
                                sw.WriteLine(sbffmpeg.ToString());
                                sw.WriteLine("exit");
                                sw.Flush();
                            }
                            process.WaitForExit();
                        }
                        //###########################################################
                        Console.WriteLine("Success to convert file!");
                        Debug.WriteLine("Success to convert file!");




                        byte[] fileData = File.ReadAllBytes(outputFilePath);
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