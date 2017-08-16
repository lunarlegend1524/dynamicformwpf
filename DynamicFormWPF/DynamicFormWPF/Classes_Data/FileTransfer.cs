namespace DynamicFormWPF.Classes_Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Ports;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;

    public class FileTransfer
    {
        public static string s = "";
        public static byte[] combined = null;
        public static string defaultFolder = @"C:\data\temp";
        public static string folder = defaultFolder;
        public static string saveFolder = string.Empty;
        public static string warning = string.Empty;
        public static List<byte[]> list = new List<byte[]>();
        public static string Connectstt = "";

        public static string Dial(SerialPort P, string phoneNumber)
        {
            string status = "";
            P.Write("ATDT" + phoneNumber + Convert.ToChar(13));
            int time = 0;
            while (Connectstt == "" && time < 250)
            {
                Thread.Sleep(100);
                if (time == 299) { status = "Quay số thất bại"; }
                time++;
            }
            if (Connectstt != "")
            {
                status = Connectstt;
            }
            return status;
        }

        public static string Connect(SerialPort P)
        {
            string status = "";
            try
            {
                P.Open();
                P.DtrEnable = true;
                P.RtsEnable = true;
                P.Handshake = Handshake.None;
                P.ReceivedBytesThreshold = 1;
                P.Write("AT X1 M1 L2 N1 Q0 V1 \\N1 S0=1 S12=1" + Convert.ToChar(13));
                Thread.Sleep(100);
                status = "OK";
            }
            catch (Exception ex) { status = ex.Message; }
            return status;
        }

        public static void Disconnect(SerialPort P)
        {
            P.DtrEnable = false;
            P.RtsEnable = false;
            Thread.Sleep(100);
            P.Write("+++");
            Thread.Sleep(100);
            P.Write("AT H0" + Convert.ToChar(13));
            Thread.Sleep(1000);
            P.DtrEnable = true;
            P.RtsEnable = true;
        }

        public static string FileTransferer(string fileName, SerialPort P)
        {
            string status = "";
            FileInfo fi = new FileInfo(fileName);
            String name = "FileInfo@" + fi.Name.Length + "@" + DataEncryption.Encrypt(fi.Name, true) + "@" + "EndInfo";
            byte[] fileinfo = StrToByteArray(name);
            try
            {
                P.Write(fileinfo, 0, fileinfo.Length);
                Thread.Sleep(100);
                Byte[] byteSource = System.IO.File.ReadAllBytes(fileName);
                FileInfo fiSource = new FileInfo(fileName);
                if (fiSource.Length > 256)
                {
                    //Chia nho thanh nhieu phan
                    int output = (int)Math.Ceiling((double)fiSource.Length / 256);
                    int sizeRemain = (int)fiSource.Length;
                    int fileOffset = -256;
                    for (int i = 0; i < output; i++)
                    {
                        sizeRemain = (int)fiSource.Length - (i * 256);
                        fileOffset += 256;
                        if (sizeRemain > 256)
                        {
                            byte[] packet = File.ReadAllBytes(fileName).Skip(fileOffset).Take(256).ToArray();
                            string checksum = GetByteChecksum(packet);
                            string header = "PacketInfo!@#$%" + checksum + "!@#$%" + packet.Length + "!@#$%";
                            string end = "!@#$%" + "EndPacket";
                            byte[] send = AppendArrays(AppendArrays(StrToByteArray(header), packet), StrToByteArray(end));
                            P.Write(send, 0, send.Length);
                            Thread.Sleep(100);
                        }
                        else
                        {
                            byte[] packet = File.ReadAllBytes(fileName).Skip(fileOffset).Take(sizeRemain).ToArray();
                            string checksum = GetByteChecksum(packet);
                            string header = "PacketInfo!@#$%" + checksum + "!@#$%" + packet.Length + "!@#$%";
                            string end = "!@#$%" + "EndPacket";
                            byte[] send = AppendArrays(AppendArrays(StrToByteArray(header), packet), StrToByteArray(end));
                            P.Write(send, 0, send.Length);
                            Thread.Sleep(100);
                            P.Write("@TheEnd@");
                            status = "OK";
                            Thread.Sleep(100);
                        }
                    }
                }
                else
                {
                    string checksum = GetByteChecksum(byteSource);
                    string header = "PacketInfo!@#$%" + checksum + "!@#$%" + byteSource.Length + "!@#$%";
                    string end = "!@#$%" + "EndPacket";
                    byte[] send = AppendArrays(AppendArrays(StrToByteArray(header), byteSource), StrToByteArray(end));
                    P.Write(send, 0, send.Length);
                    Thread.Sleep(100);
                    P.Write("@TheEnd@");
                    status = "OK";
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return status;
        }

        public static void FileReceiver(object obj, SerialDataReceivedEventArgs e)
        {
            warning = "";
            SerialPort P = (SerialPort)obj;
            byte[] data = new byte[P.BytesToRead];
            P.Read(data, 0, data.Length);
            if (combined == null) { combined = data; }
            else
            {
                combined = AppendArrays(combined, data);
            }
            s += ByteArrayToStr(data);
            if (s.Contains("FileInfo") && s.Contains("EndInfo") && (s.IndexOf("EndInfo") - s.IndexOf("FileInfo")) > 5)
            {
                string fileInfo = s.Substring(s.IndexOf("FileInfo"), s.IndexOf("EndInfo") + 7 - s.IndexOf("FileInfo"));
                string fileName = GetFileName(fileInfo);
                combined = AppendArrays(combined.Skip(0).Take(s.IndexOf("FileInfo")).ToArray(), combined.Skip(s.IndexOf("EndInfo") + 7).ToArray());
                s = s.Replace(fileInfo, "");
                folder += "\\" + fileName;
                saveFolder = folder;
            }
            else if (s.Contains("AT") && s.Contains(Convert.ToChar(13)) || s.Contains("RING") && s.Contains(Convert.ToChar(13)) || s.Contains("CONNECT") && s.Contains(Convert.ToChar(13)) || s.Contains("OK") && s.Contains(Convert.ToChar(13)) || s.Contains("NO ANSWER") && s.Contains(Convert.ToChar(13)) || s.Contains("BUSY") && s.Contains(Convert.ToChar(13)) || s.Contains("NO CARRIER") && s.Contains(Convert.ToChar(13)) || s.Contains("NO DIALTONE") && s.Contains(Convert.ToChar(13)) || s.Contains("ERROR") && s.Contains(Convert.ToChar(13)))
            {
                if (s.Contains("CONNECT")) { Connectstt = "CONNECTED"; }
                else if (s.Contains("BUSY")) { Connectstt = "BUSY"; }
                else if (s.Contains("NO ANSWER")) { Connectstt = "NO ANSWER"; }
                else if (s.Contains("ERROR")) { Connectstt = "ERROR"; }
                s = "";
                combined = null;
            }
            else if (s.Contains("PacketInfo!@#$%") && s.Contains("EndPacket") && (s.IndexOf("EndPacket") - s.IndexOf("PacketInfo!@#$%")) > 5)
            {
                string packetInfo = s.Substring(s.IndexOf("PacketInfo!@#$%"), s.IndexOf("EndPacket") + 9 - s.IndexOf("PacketInfo!@#$%"));
                string packetLength = GetPacketLength(packetInfo);
                string packetChecksum = GetPacketChecksum(packetInfo);
                if (Convert.ToInt32(packetLength) != 0)
                {
                    byte[] packet = combined.Skip(s.IndexOf("PacketInfo!@#$%") + packetChecksum.Length + packetLength.Length + 25).Take(Convert.ToInt32(packetLength)).ToArray();
                    if (packetChecksum.Equals(GetByteChecksum(packet)))
                    {
                        list.Add(packet);
                    }

                    //else { P.Write("Gui File Bi Loi Roi"); }
                }
                combined = AppendArrays(combined.Skip(0).Take(s.IndexOf("PacketInfo!@#$%")).ToArray(), combined.Skip(s.IndexOf("EndPacket") + 9).ToArray());
                s = s.Replace(packetInfo, "");
            }

            //else if (s.Contains("Gui File Bi Loi Roi")) { warning = "Disconnect"; }
            else if (s.Contains("@TheEnd@"))
            {
                try
                {
                    if (File.Exists(folder)) { File.Delete(folder); }
                    FileStream fs = new FileStream(folder, FileMode.Append);
                    for (int i = 0; i < list.Count; i++)
                    {
                        fs.Write(list[i], 0, list[i].Length);
                    }
                    fs.Close();
                }
                catch (Exception) { }
                s = "";
                combined = null;
                list.Clear();
                warning = "OK";
                folder = defaultFolder;
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

        public static string GetFileName(string fileInfo)
        {
            string fileName = "";
            try
            {
                string[] info = fileInfo.Split('@');
                if (DataEncryption.Decrypt(info[2], true).Length == Convert.ToInt32(info[1])) { fileName = DataEncryption.Decrypt(info[2], true); }
            }
            catch (Exception) { }
            return fileName;
        }

        public static byte[] AppendArrays(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, c, 0, a.Length);
            Buffer.BlockCopy(b, 0, c, a.Length, b.Length);
            return c;
        }

        public static string GetPacketChecksum(string packetInfo)
        {
            try
            {
                string checksum = "";
                string[] stringSeparators = new string[] { "!@#$%" };
                string[] info = packetInfo.Split(stringSeparators, StringSplitOptions.None);
                checksum = info[1];
                return checksum;
            }
            catch (Exception ex) { return ex.Message; }
        }

        public static string GetPacketLength(string packetInfo)
        {
            string packetLength = "";
            try
            {
                string[] stringSeparators = new string[] { "!@#$%" };
                string[] info = packetInfo.Split(stringSeparators, StringSplitOptions.None);
                if (StrToByteArray(info[3]).Length == Convert.ToInt32(info[2])) { packetLength = info[2]; }
            }
            catch (Exception ex) { return ex.Message; }
            return packetLength;
        }

        public static string getSavedPath()
        {
            return saveFolder;
        }

        public static string getWarning()
        {
            return warning;
        }
    }
}