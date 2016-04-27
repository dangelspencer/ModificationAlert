using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ModificationAlert
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> modified_files = new List<string>();
            List<string> old_files = new List<string>();
            List<string> new_files = new List<string>();

            //load configuration
            string[] config = null;
            if (args.Length == 1)
            {
                config = File.ReadAllLines(args[0]);
            }
            else
            {
                config = File.ReadAllLines(@"modification_config.ini");
            }
            string[] recipients = null;
            string[] config_parts = null;
            foreach (string entry in config)
            {
                if (!entry.StartsWith("#"))
                {
                    config_parts = entry.Split('=');
                    if (config_parts[1].Contains(','))
                    {
                        recipients = config_parts[1].Split(',');
                    }
                    else
                    {
                        recipients = new string[1];
                        recipients[0] = config_parts[1];
                    }
                    Console.WriteLine(" ");
                    Console.WriteLine("Path: " + config_parts[0]);
                    Console.WriteLine("Recipients: " + config_parts[1]);
                    Console.WriteLine("Log: " + config_parts[2]);

                    checkPath(config_parts[0], recipients, config_parts[2], old_files, new_files, modified_files);

                    File.Delete(config_parts[2]);
                    File.WriteAllLines(config_parts[2], new_files.ToArray());

                    string message = string.Format("<meta http-equiv='Content-Type' content='text/html;charset=utf-8'><p>The following file(s) have been modified in <a href='{0}'>{1}</a>", config_parts[0], config_parts[0].Substring(config_parts[0].LastIndexOf(@"\") + 1));
                    if (modified_files.ToArray().Length > 0)
                    {
                        foreach (string file in modified_files)
                        {
                            message += string.Format("<br>&emsp;-<a href='file:///{0}'>{1}</a>", file, file.Substring(file.LastIndexOf(@"\") + 1));
                        }
                        message += "</p>";
                        foreach (string recipient in recipients)
                        {
                            sendEmail(message, recipient);
                        }
                    }

                    new_files.Clear();
                    old_files.Clear();
                    modified_files.Clear();
                }
            }
        }

        static void checkPath(string path, string[] recipients, string log_path, List<string> old_files, List<string>  new_files, List<string> modified_files)
        {
            string hash = "";
            string[] old_info = null;
            bool new_file = true;
            bool first_run = true;

            if (File.Exists(log_path))
            {
                old_files = File.ReadAllLines(log_path).ToList();
                first_run = false;
            }


            foreach (string file in Directory.GetFiles(path))
            {
                hash = getFileHash(file);
                if (!first_run)
                {
                    if (hash != "")
                    {
                        new_file = true;
                        foreach (string old_file in old_files)
                        {
                            old_info = old_file.Split('=');
                            
                            if (file == old_info[0])
                            {
                                new_file = false;
                                Console.WriteLine("Checking: " + file + " -> " + old_info[0] + ", MATCH");
                                if (hash != old_info[1])
                                {
                                    modified_files.Add(file);
                                    new_files.Add(file + "=" + hash);
                                    Console.WriteLine("Modified: " + file + " (" + old_info[1] + " -> " + hash + ")");
                                }
                                else
                                {
                                    new_files.Add(old_file);
                                }
                                break;
                            }
                        }
                    }
                    if (new_file)
                    {
                        new_files.Add(file + "=" + hash);
                        modified_files.Add(file);
                        Console.WriteLine("Found: " + file + "-" + hash);
                    }
                }
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                Console.WriteLine("Checking Directory: " + dir);
                checkPath(dir, recipients, log_path, old_files, new_files, modified_files);
            }
        }

        //gets file hash and returns as a string
        static private string getFileHash(string fileName)
        {
            try
            {
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    var buffer = md5.ComputeHash(File.ReadAllBytes(fileName));
                    var sb = new StringBuilder();
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        sb.Append(buffer[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch (System.IO.IOException)
            {
                return "";
            }
        }

        static void sendEmail(string message, string recipient)
        {
            MailMessage mail = new MailMessage("example@example.com", recipient);
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "example.com";
            mail.Subject = "Modified File(s)";
            mail.Body = message;
            mail.IsBodyHtml = true;
            client.Send(mail);
        }
    }
}
