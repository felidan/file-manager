using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FileManager
{
    static class Program
    {
        const ConsoleColor COLOR_HEADER = ConsoleColor.DarkRed;
        const ConsoleColor COLOR_PRIMARY = ConsoleColor.DarkBlue;
        const ConsoleColor COLOR_SECUNDARY = ConsoleColor.DarkGreen;
        const ConsoleColor COLOR_NON = ConsoleColor.Gray;
        static void Main(string[] args)
        {
            var temp = ShowFiles();
            if (temp != null && temp.Any())
            {
                PrintHeader();
                string command = ReadInput("Enter your pass for open fefault File OR # for go to menu", temp[0].Item2, (int)ConsoleColor.DarkCyan, false);
                
                if (command != "#")
                {
                    if (command.Length < 16) command = command.PadRight(16, 'X');
                    TryOpenFile(temp[0].Item2, command);
                }
                else
                    InitProcess();
            }
            else
                InitProcess();
        }

        
        private static void InitProcess()
        {
            while (true)
            {
                ShowMenu();
                string option = ReadInput("Enter a option");

                if (option == "1")
                {
                    #region 1 - Open File
                    PrintHeader();
                    var files = ShowFiles();
                    if (files != null && files.Any())
                    {
                        PrintFiles(files);
                        string fileName = ReadInput("Select file");
                        if (fileName == null) continue;
                        
                        string pathFile = files.First(x => x.Item1 == fileName).Item2;

                        PrintHeader();
                        string key = ReadInput("Enter with your key", clean: false);
                        if (key == null) continue;

                        if (key.Length < 16) key = key.PadRight(16, 'X');

                        TryOpenFile(pathFile, key);
                    }
                    else
                    {
                        Console.WriteLine("Not Found Files..");
                        Console.ReadKey();
                    }
                    #endregion
                }
                else if (option == "2")
                {
                    #region 2 - Register File
                    PrintHeader();
                    string path = ReadInput("Enter with file path");
                    if (path == null) continue;
                    
                    string key = ReadInput("Enter with your key", "ALERT! The content file will be encrypted!");
                    if (key == null) continue;
                    
                    if (key.Length < 16) key = key.PadRight(16, 'X');

                    RegisterFile(path);
                    EncryptFile(path, key);

                    Console.WriteLine("File registered with success!");
                    Thread.Sleep(2000);
                    #endregion
                }
                else if (option == "3")
                {
                    #region 3 - Unregister File
                    PrintHeader();
                    var files = ShowFiles();
                    if (files != null && files.Any())
                    {
                        PrintFiles(files);
                        string fileName = ReadInput("Select file");
                        if (fileName == null) continue;
                        
                        var item = files.First(x => x.Item1 == fileName);
                        files.Remove(item);

                        PrintHeader();
                        string key = ReadInput("Enter with your key", "Removing file " + item.Item2 + "..", preTextColor: (int)ConsoleColor.DarkCyan);
                        if (key == null) continue;
                        
                        if (key.Length < 16) key = key.PadRight(16, 'X');
                        
                        DecryptFile(item.Item2, key);
                        UpdateRegisterFiles(files);

                        Console.WriteLine("File unregistered with success!");
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        Console.WriteLine("Not Found Files..");
                        Console.ReadKey();
                    }
                    #endregion
                }
                else if (option == "4") return;
            }
        }

        private static string ReadInput(string labal, string preText = null, int preTextColor = -1, bool clean = true)
        {
            string input = "";

            if(preText != null)
            {
                Console.ForegroundColor = preTextColor == -1 ? COLOR_SECUNDARY : (ConsoleColor)preTextColor;
                Console.WriteLine();
                Console.Write(preText);
                Console.WriteLine(Environment.NewLine);
                
            }

            Console.ForegroundColor = COLOR_SECUNDARY;
            Console.Write($"{labal}");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($" (or ENTER for CANCEL)");
            Console.ForegroundColor = COLOR_SECUNDARY;
            Console.Write($": ");
            Console.ForegroundColor = COLOR_NON;

            input = Console.ReadLine();

            if(clean) Console.Clear();

            if (input == "" || input == "." || input == "\n" || input == "..")
                return null;

            return input;
        }

        private static void TryOpenFile(string pathFile, string key)
        {
            try
            {
                DecryptFile(pathFile, key);
                ExecFile(pathFile);
                Console.Write("Ended? ");
                Console.ReadKey();
                EncryptFile(pathFile, key);
            }
            catch
            {
                Console.Write("Invalid Password!");
                Console.ReadKey();
            }
        }
        
        #region CMD

        private static void ExecFile(string pathFile)
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = false;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine(pathFile);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
        }

        #endregion

        #region IO

        private static List<(string, string)> ShowFiles()
        {
            var files = new List<(string, string)>();
            using (StreamReader stream = new StreamReader($"{Directory.GetCurrentDirectory()}/file-register.cypt"))
            {
                int index = 1;
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();
                    files.Add((index.ToString(), line));
                    index++;
                }
            }
            return files;
        }

        private static void EncryptFile(string pathFile, string key)
        {
            string file = "";
            using (StreamReader stream = new StreamReader(pathFile))
            {
                file = stream.ReadToEnd();
                file = Encript(file, key);
            }

            using (StreamWriter stream = new StreamWriter(pathFile))
            {
                stream.Write(file);
            }
        }

        private static void DecryptFile(string pathFile, string key)
        {
            string file = "";
            using (StreamReader stream = new StreamReader(pathFile))
            {
                file = stream.ReadToEnd();
                file = Decript(file, key);
            }

            using (StreamWriter stream = new StreamWriter(pathFile))
            {
                stream.Write(file);
            }
        }

        private static void RegisterFile(string path)
        {
            using (StreamWriter stream = new StreamWriter($"{Directory.GetCurrentDirectory()}/file-register.cypt"))
            {
                stream.WriteLine(path);
            }
        }

        private static void UpdateRegisterFiles(List<(string, string)> files)
        {
            FileInfo fi = new FileInfo($"{Directory.GetCurrentDirectory()}/file-register.cypt");
            using (StreamWriter stream = new StreamWriter(fi.Open(FileMode.Truncate)))
            {
                files.ForEach(item =>
                {
                    stream.WriteLine(item.Item2);
                });
            }
        }

        #endregion

        #region PRINT
        private static void PrintHeader()
        {
            Console.Clear();
            var temp = Console.ForegroundColor;
            Console.ForegroundColor = COLOR_HEADER;
            Console.WriteLine("-- -------------------------------------- --");
            Console.WriteLine("-- -- Security Files by @Felipe Dantas -- --");
            Console.WriteLine("-- -------------------------------------- --");
            Console.WriteLine("");
            Console.ForegroundColor = temp;
        }

        private static void ShowMenu()
        {
            PrintHeader();

            Console.ForegroundColor = COLOR_PRIMARY;
            Console.WriteLine("1 - Open File");
            Console.WriteLine("2 - Register File");
            Console.WriteLine("3 - Unregister File");
            Console.WriteLine("4 - End");
            Console.WriteLine("");
        }

        private static void PrintFiles(List<(string, string)> files)
        {
            Console.ForegroundColor = COLOR_PRIMARY;

            files.ForEach(item => { Console.WriteLine($"{item.Item1} - {item.Item2}"); });
        }

        #endregion

        #region ENCRIPT / DECRYPT

        private static string Encript(string file, string key)
        {
            using (Rijndael algoritmo = CriarInstanciaRijndael(key, "Lk86tYUgHfT68iUx"))
            {
                ICryptoTransform encryptor =
                    algoritmo.CreateEncryptor(
                        algoritmo.Key, algoritmo.IV);

                using (MemoryStream streamResultado =
                       new MemoryStream())
                {
                    using (CryptoStream csStream = new CryptoStream(
                        streamResultado, encryptor,
                        CryptoStreamMode.Write))
                    {
                        using (StreamWriter writer =
                            new StreamWriter(csStream))
                        {
                            writer.Write(file);
                        }
                    }

                    return ArrayBytesToHexString(
                        streamResultado.ToArray());
                }
            }
        }

        private static string Decript(string fileText, string pass)
        {
            using (var algoritmo = CriarInstanciaRijndael(
                pass, "Lk86tYUgHfT68iUx"))
            {
                ICryptoTransform decryptor =
                    algoritmo.CreateDecryptor(
                        algoritmo.Key, algoritmo.IV);

                string textoDecriptografado = null;
                using (MemoryStream streamTextoEncriptado =
                    new MemoryStream(
                        HexStringToArrayBytes(fileText)))
                {
                    using (CryptoStream csStream = new CryptoStream(
                        streamTextoEncriptado, decryptor,
                        CryptoStreamMode.Read))
                    {
                        using (StreamReader reader =
                            new StreamReader(csStream))
                        {
                            textoDecriptografado =
                                reader.ReadToEnd();
                        }
                    }
                }

                return textoDecriptografado;
            }
        }

        private static string ArrayBytesToHexString(byte[] conteudo)
        {
            string[] arrayHex = Array.ConvertAll(
                conteudo, b => b.ToString("X2"));
            return string.Concat(arrayHex);
        }

        private static Rijndael CriarInstanciaRijndael(
            string chave, string vetorInicializacao)
        {
            Rijndael algoritmo = Rijndael.Create();
            algoritmo.Key =
                Encoding.ASCII.GetBytes(chave);
            algoritmo.IV =
                Encoding.ASCII.GetBytes(vetorInicializacao);

            return algoritmo;
        }

        private static byte[] HexStringToArrayBytes(string conteudo)
        {
            conteudo = conteudo.Replace("\r\n", "");
            int qtdeBytesEncriptados =
                conteudo.Length / 2;
            byte[] arrayConteudoEncriptado =
                new byte[qtdeBytesEncriptados];
            for (int i = 0; i < qtdeBytesEncriptados; i++)
            {
                arrayConteudoEncriptado[i] = Convert.ToByte(conteudo.Substring(i * 2, 2), 16);
            }

            return arrayConteudoEncriptado;
        }

        #endregion
    }
}