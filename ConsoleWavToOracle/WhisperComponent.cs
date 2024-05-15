/*
@using System.Text
@using Whisper.net
@using Whisper.net.Ggml

@using System;
@using System.IO;
@using System.Diagnostics;


<h2>Whisper.net modelName: @selectedModel</h2>
Требования к аудиозаписи: wave-файл с частотой дискретизации 16 кГц
<br />

@* Добавление выпадающего списка для выбора языка 
<select @onchange="ChangeLanguage">
    <option value="ru">Русский</option>
    <option value="en">Английский</option>
</select>
@* Добавление выпадающего списка для выбора модели Whisper
<select @onchange="ChangeModel">
    @foreach (var modelFile in Directory.GetFiles(@"D:/WhisperModel/"))
    {
        <option value="@modelFile">@Path.GetFileName(modelFile)</option>
    }
</select> *@

@if (whisperProcessor == null)
{
    <p>Loading...</p>
}
else
{
    <br />
    <br />
    <div class="text-left">
        <p>Перенесите сюда или выберите файл для обработки</p>
        <InputFile OnChange="@(async (f) => await OnFileChange(f))"> </InputFile>
        @*<br /><input type="file" @onchange="OnFileChange" />*@
    </div>
    <br />
    @*<button @onclick="CleanupResources">Остановить выполнение</button>*@
}

@if (!string.IsNullOrEmpty(txtResult))
{
    <p>Результат выполнения:</p>
    <pre style="white-space: pre-wrap; background:#d8eaef;">@txtResult</pre>
}

@code
{
    private string selectedLanguage = "ru"; // Default language
    private string selectedModel = "D:/WhisperModel/ggml-large-v3.bin"; // Default whisper model

    private WhisperProcessor? whisperProcessor = null;
    private static WhisperFactory? whisperFactory = null;
    private string txtResult = string.Empty;
    //private string ggmlmodelName = "ggml-small.bin";
    private string path = @"C:\temp\1";
    private string ffmpegExePath = @"C:\dotnet\ffmpeg\ffmpeg.exe"; // Путь к исполняемому файлу FFmpeg

    // Метод для очистки ресурсов Whisper после завершения
    private void CleanupResources()
    {
        whisperProcessor?.Dispose();
        whisperFactory?.Dispose();
    }

    private void CleanupTempFiles()
    {
        Directory.Delete(path, true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            //CleanupResources();
            //whisperProcessor = null;
            //whisperFactory = null;
            //StateHasChanged();
            return;
        }

        string modelName = selectedModel;

        var modelFilePath = selectedModel; //$"D:/WhisperModel/{modelName}";
        whisperFactory = WhisperFactory.FromPath(modelFilePath);

        if (whisperFactory == null)
        {
            using var memoryStream = new MemoryStream();
            var model = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Tiny);
            await model.CopyToAsync(memoryStream);
            whisperFactory = WhisperFactory.FromBuffer(memoryStream.ToArray());
        }

        if (whisperProcessor == null)
        {
            whisperProcessor = whisperFactory.CreateBuilder()
                                    .WithLanguage(selectedLanguage) //en, ru...
                                    .Build();
            StateHasChanged();
        }
    }

    private async Task OnFileChange(InputFileChangeEventArgs e)
    {
        var file = e.File;
        // Create the directory if it doesn't exist. Generate a file name
        DirectoryInfo dirInfo = new DirectoryInfo(path);
        if (!dirInfo.Exists)
        {
            dirInfo.Create();
        }
        var trustedFileName = Path.GetRandomFileName();
        var trustedFilePath = Path.Combine(path, trustedFileName);

        // Save the file to the specified folder
        if (file != null)
        {
            try
            {
                long maxallowedsize = 1024 * 1024 * 128; // 128 MB
                using (var fileStream = file.OpenReadStream(maxallowedsize))
                {
                    using (var fileOutput = new FileStream(trustedFilePath, FileMode.Create))
                    {
                        byte[] buffer = new byte[8192]; // Buffer size for copying in chunks

                        int bytesRead;
                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileOutput.WriteAsync(buffer, 0, bytesRead);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately, e.g., log the error or display an error message
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        Debug.WriteLine("Success to save file!");

        string filePath = trustedFilePath; // путь к входному файлу
        string outputFilePath = trustedFilePath + ".wav"; // путь к выходному файлу
        txtResult = "файл скопирован, пожалуйста подождите...";
        StateHasChanged();
        System.Threading.Thread.Sleep(2000);

        // Create the command string for FFmpeg
        //###########################################################
        StringBuilder sbffmpeg = new StringBuilder();
        sbffmpeg.Append($"{ffmpegExePath} -i ");
        sbffmpeg.Append(filePath);
        sbffmpeg.Append(" -codec:a pcm_s16le -b:a 128k -ac 1 -ar 16000 "); // формат для Whisper
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
                txtResult = sbffmpeg.ToString();
                sw.WriteLine(sbffmpeg.ToString());
                sw.WriteLine("exit");
                sw.Flush();
            }
            process.WaitForExit();
        }
        //###########################################################
        Debug.WriteLine("Success to convert file!");
        txtResult = "конвертирование файла заперешено, передаю данные в Whisper, пожалуйста подождите...";
        StateHasChanged();
        System.Threading.Thread.Sleep(3000);

        var sb = new StringBuilder();
        using (var fileStream = File.OpenRead(outputFilePath))
        {
            txtResult = "";
            await foreach (var result in whisperProcessor!.ProcessAsync(fileStream))
            {
                sb.AppendLine($"{result.Start}->{result.End}: {result.Text}");
                txtResult = sb.ToString();
                StateHasChanged();
            }
        }
        Debug.WriteLine("Success to make text with Whisper!");
        CleanupTempFiles();
        CleanupResources();
        sb.AppendLine("---------------------------");
        sb.AppendLine("Преобразование завершено! Благодарим за использование.");
        txtResult = sb.ToString();
        StateHasChanged();
    }
}
*/