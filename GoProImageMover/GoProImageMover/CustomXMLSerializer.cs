using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace GoProImageMover
{
    public static class CustomXMLSerializer
    {
        public static void SaveXmlData<T>(T obj, string filePath) where T : class
        {
            System.Xml.Serialization.XmlSerializer serializer1 =
                        new System.Xml.Serialization.XmlSerializer(typeof(T));

            StreamWriter sw = new StreamWriter(filePath,
                                               false,
                                               Encoding.UTF8);
            serializer1.Serialize(sw, obj);
            sw.Close();
        }

        public static T LoadXmlData<T>(string filePath) where T : class
        {
            System.Xml.Serialization.XmlSerializer serializer2 =
                new System.Xml.Serialization.XmlSerializer(typeof(T));
            System.IO.FileStream fs2 =
                new System.IO.FileStream(filePath, System.IO.FileMode.Open);
            T loadAry;
            loadAry = (T)serializer2.Deserialize(fs2);
            fs2.Close();

            return loadAry;
        }

        public static void WriteLoadFile(string path, string text)
        {
            Encoding enc = Encoding.GetEncoding("UTF-8");
            StreamWriter writer =
                    new StreamWriter(path, false, enc);
            writer.Write(text);
            writer.Close();
        }
    }
}
