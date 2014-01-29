using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GoProImageMover.Data
{
    //XMLファイルに保存するオブジェクトのためのクラス
    [XmlRoot("root")]
    public class DataAppInfo
    {
        [XmlElement("Driver1")]
        public string Driver1 = "";

        [XmlElement("Driver2")]
        public string Driver2 = "";

        [XmlElement("Driver3")]
        public string Driver3 = "";
        
        [XmlElement("Driver4")]
        public string Driver4 = "";
        
        [XmlElement("Driver5")]
        public string Driver5 = "";
        
        [XmlElement("Driver6")]
        public string Driver6 = "";

        [XmlElement("GoProPath")]
        public string GoProPath = "";

        [XmlElement("PCPath")]
        public string PCPath = "";

        [XmlElement("take")]
        public int take = 0;

        [XmlElement("format")]
        public string format = "GoPro_{0}_";

    }
}
