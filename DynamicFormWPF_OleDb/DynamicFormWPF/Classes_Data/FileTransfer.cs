namespace DynamicFormWPF
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using System.Linq;
    using System.Text;
    using System.IO.Ports;
    using System.IO;
    using System.Security.Cryptography;

    class FileTransfer
    {
        public static string folder = @"D:\data";
        public static List<byte[]> list = new List<byte[]>();
        public static ArrayList array = new ArrayList();

        public static string FileTransferer(string FileName, SerialPort P)
        {
            string status = string.Empty;
            byte[] flag = null;
            //P.DataReceived += new SerialDataReceivedEventHandler(FileReceiver);
            FileInfo fi = new FileInfo(FileName);
            String name = "FileInfo@" + fi.Name;
            byte[] fileinfo = StrToByteArray(name);
            try
            {
                P.Write(fileinfo, 0, fileinfo.Length);
                //SendBinaryFile(P, FileName);
                Byte[] byteSource = System.IO.File.ReadAllBytes(FileName);
                FileInfo fiSource = new FileInfo(FileName);
                if (fiSource.Length > 4096)
                {
                    //Chia nho thanh nhieu phan 4KB
                    int output = (int)Math.Ceiling((double)fiSource.Length / 4096);
                    int sizeRemain = (int)fiSource.Length;
                    int fileOffset = -4096;
                    for (int i = 0; i < output; i++)
                    {
                        sizeRemain = (int)fiSource.Length - (i * 4096);
                        fileOffset += 4096;
                        if (sizeRemain >= 4096)
                        {
                            byte[] packet = File.ReadAllBytes(FileName).Skip(fileOffset).Take(4096).ToArray();
                            string checksum = GetByteChecksum(packet);
                            string pacInfo = "PacketInfo@" + checksum;
                            byte[] pInfo = StrToByteArray(pacInfo);
                            P.Write(pInfo, 0, pInfo.Length);
                            System.Threading.Thread.Sleep(100);
                            P.Write(byteSource, fileOffset, 4096);
                            System.Threading.Thread.Sleep(100);
                        }
                        else
                        {
                            byte[] packet = File.ReadAllBytes(FileName).Skip(fileOffset).Take(sizeRemain).ToArray();
                            string checksum = GetByteChecksum(packet);
                            string pacInfo = "PacketInfo@" + checksum;
                            byte[] pInfo = StrToByteArray(pacInfo);
                            P.Write(pInfo, 0, pInfo.Length);
                            System.Threading.Thread.Sleep(100);
                            P.Write(byteSource, fileOffset, sizeRemain);
                            System.Threading.Thread.Sleep(100);

                        }
                    }
                    flag = StrToByteArray("THE END");
                    P.Write(flag, 0, flag.Length);
                    System.Threading.Thread.Sleep(100);
                }
                else
                {
                    string checksum = GetByteChecksum(byteSource);
                    string pacInfo = "PacketInfo@" + checksum;
                    byte[] pInfo = StrToByteArray(pacInfo);
                    P.Write(pInfo, 0, pInfo.Length);
                    System.Threading.Thread.Sleep(100);
                    P.Write(byteSource, 0, (int)fiSource.Length);
                    System.Threading.Thread.Sleep(100);
                    flag = StrToByteArray("THE END");
                    P.Write(flag, 0, flag.Length);
                    System.Threading.Thread.Sleep(100);
                }
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }
            return status;
        }

        public static void FileReceiver(object obj)
        {
            SerialPort Port = (SerialPort)obj;
            byte[] data = new byte[Port.BytesToRead];
            Port.Read(data, 0, data.Length);
            String s = ByteArrayToStr(data);
            if (s.Contains("FileInfo"))
            {
                string[] info = s.Split('@');
                folder +=  "\\" + info[1];
            }
            else if (s.Contains("PacketInfo"))
            {
                string[] pInfo = s.Split('@');
                array.Add(pInfo[1]);
            }
            else if (s.Contains("THE END"))
            {
                String fileRec = folder;

                try
                {
                    FileStream _FileStream = new FileStream(fileRec, System.IO.FileMode.Append);

                    for (int i = 0; i < list.Count; i++)
                    {
                        _FileStream.Write(list[i], 0, list[i].Length);
                    }
                    _FileStream.Close();
                }
                catch (Exception ex)
                {
                    Console.Write(ex.ToString());
                }
                list.Clear();
            }
            else if (s == null || s == "") { }
            else
            {
                string checksum = GetByteChecksum(data);
                if (array.Contains(checksum))
                {
                    list.Add(data);
                }

            }
        }

        public static byte[] StrToByteArray(string str)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(str);
        }

        public static string ByteArrayToStr(byte[] byteArray)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetString(byteArray);
        }

        public static string GetByteChecksum(byte[] bytes)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

            byte[] hashValue = md5.ComputeHash(bytes);

            return Convert.ToBase64String(hashValue);
        }

        public static string getSavedPath()
        {
            return folder;
        }
    }
}