using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace FilePacket
{
    public enum PacketType
    {
        이름과사이즈 = 0,
        파일,
        더블클릭,
        파일이름
    }

    /*public enum PacketSendERROR
    {
        정상 = 0,
        에러
    }*/

    [Serializable]
    public class Packet
    {
        public int Length;
        public int Type;

        public Packet()
        {
            this.Length = 0;
            this.Type = 0;
        }

        public static byte[] Serialize(Object o)
        {
            MemoryStream ms = new MemoryStream(1024 * 4);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, o);

            return ms.ToArray();
        }

        public static Object Deserialize(byte[] bt)
        {
            MemoryStream ms = new MemoryStream(1024 * 4);
            foreach (byte b in bt)
            {
                ms.WriteByte(b);
            }

            ms.Position = 0;
            BinaryFormatter bf = new BinaryFormatter();

            Object obj = bf.Deserialize(ms);

            ms.Close();

            return obj;
        }
    }

    [Serializable]
    public class DataSend : Packet  //파일 데이터
    {
        public byte[] data = new byte[1024 * 4];        
    }

    [Serializable]
    public class NameSize : Packet  //이름, 사이즈
    {
        public long size = 0;
        public string name;
    }

    [Serializable]
    public class DoubleClickItem : Packet
    {
        public string itemName;
    }

    [Serializable]
    public class DataName : Packet
    {
        public string name;
    }

    class Class1
    {
        
    }
}
